// Shared Yahoo Finance proxy — the SINGLE source of truth used by BOTH the
// Vercel serverless function (api/yahoo/[...path].js) and the Vite dev-server
// middleware (vite.config.ts), so dev and prod run identical crumb logic (NFR-5).
//
// Responsibilities:
//  - Perform the Yahoo cookie+crumb handshake server-side and cache it (~30min).
//  - Append &crumb + attach Cookie for the 3 crumb-gated endpoints; pass v8 chart
//    through untouched.
//  - SECURITY (Auditor conditions): host-pin, path allowlist, per-IP rate limit,
//    and NEVER log or leak the crumb/cookie to the browser.
//
// This module uses only native Node http req/res members (req.url, req.method,
// req.headers, req.socket; res.statusCode/setHeader/end) so the same handler
// works under Vercel's (req,res) and Vite's connect (req,res,next) signatures.

const UA =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

const PRIMARY_HOST = "query1.finance.yahoo.com";
export const ALLOWED_HOSTS = new Set(["query1.finance.yahoo.com", "query2.finance.yahoo.com"]);

// Path allowlist — only these four upstream prefixes may be proxied.
const ALLOWED_PREFIXES = [
  "/v8/finance/chart/",
  "/v7/finance/quote",
  "/v10/finance/quoteSummary/",
  "/v1/finance/search",
];
// Of those, these require the crumb + cookie (v8 chart is keyless).
const CRUMB_REQUIRED_PREFIXES = ["/v7/finance/quote", "/v10/finance/quoteSummary/", "/v1/finance/search"];

const SESSION_TTL_MS = 30 * 60 * 1000; // 30 minutes

// ---------------------------------------------------------------------------
// Logging — redacts crumb / cookie / query string so a secret can never reach
// the log sink. Only method + the query-less upstream path + status are logged.
// ---------------------------------------------------------------------------
const SECRET_KEYS = /(crumb|cookie|set-cookie|a1|a3)/i;
export function redactForLog(value) {
  if (value == null) return value;
  const s = String(value);
  // Strip any query string entirely (it may carry ?...&crumb=...).
  const noQuery = s.split("?")[0];
  // Belt-and-braces: blank out any k=v pair whose key looks secret.
  return noQuery.replace(/([?&][^=]*=)[^&\s]*/g, (m) => (SECRET_KEYS.test(m) ? m.replace(/=.*/, "=[REDACTED]") : m));
}
function logLine(method, upstreamPath, status) {
  // upstreamPath is already query-less by construction; redactForLog is defensive.
  // eslint-disable-next-line no-console
  console.log(`[yahoo-proxy] ${method} ${redactForLog(upstreamPath)} -> ${status}`);
}

// ---------------------------------------------------------------------------
// Per-IP token bucket. NOTE: serverless instances are ephemeral and per-region,
// so this state is best-effort and weakens under horizontal scale — acceptable
// for personal use, documented per the Auditor condition.
// ---------------------------------------------------------------------------
const BUCKET_CAPACITY = 60; // burst
const BUCKET_REFILL_PER_SEC = 1; // sustained ~1 req/s/IP
const buckets = new Map();
export function rateLimitAllows(ip, now = Date.now()) {
  let b = buckets.get(ip);
  if (!b) {
    b = { tokens: BUCKET_CAPACITY, ts: now };
    buckets.set(ip, b);
  }
  const elapsedSec = (now - b.ts) / 1000;
  b.tokens = Math.min(BUCKET_CAPACITY, b.tokens + elapsedSec * BUCKET_REFILL_PER_SEC);
  b.ts = now;
  if (b.tokens < 1) return false;
  b.tokens -= 1;
  return true;
}
function clientIp(req) {
  const xff = req.headers?.["x-forwarded-for"];
  if (typeof xff === "string" && xff.length > 0) return xff.split(",")[0].trim();
  return req.socket?.remoteAddress || "unknown";
}

// ---------------------------------------------------------------------------
// Pure request validation + upstream URL construction. Exported for unit tests
// so host-pin / allowlist behaviour can be asserted without any network.
// Returns { ok:true, upstreamPath, needsCrumb, buildTarget(crumb) } or
// { ok:false, status, error }.
// ---------------------------------------------------------------------------
export function resolveUpstream(rawUrl) {
  if (typeof rawUrl !== "string" || rawUrl.length === 0) {
    return { ok: false, status: 400, error: "Missing request URL" };
  }

  // Reject obviously hostile encodings BEFORE any parsing/decoding.
  // (encoded slash/backslash, userinfo, CRLF, path traversal)
  if (/%2f|%5c|%2e%2e|\\|\r|\n/i.test(rawUrl)) {
    return { ok: false, status: 400, error: "Illegal characters in request path" };
  }

  let url;
  try {
    url = new URL(rawUrl, "http://localhost");
  } catch {
    return { ok: false, status: 400, error: "Malformed request URL" };
  }

  const upstreamPath = url.pathname.replace(/^\/api\/yahoo/, "");
  if (!upstreamPath.startsWith("/")) {
    return { ok: false, status: 400, error: "Invalid upstream path" };
  }
  if (upstreamPath.includes("..") || upstreamPath.includes("@")) {
    return { ok: false, status: 400, error: "Illegal upstream path" };
  }

  const allowed = ALLOWED_PREFIXES.some((p) => upstreamPath.startsWith(p));
  if (!allowed) {
    return { ok: false, status: 400, error: "Upstream path is not on the allowlist" };
  }

  const needsCrumb = CRUMB_REQUIRED_PREFIXES.some((p) => upstreamPath.startsWith(p));

  const buildTarget = (crumb) => {
    const params = new URLSearchParams(url.search);
    if (needsCrumb && crumb) params.set("crumb", crumb);
    const target = new URL(`https://${PRIMARY_HOST}${upstreamPath}`);
    target.search = params.toString();
    // Auditor condition: assert the FULLY-RESOLVED host after construction.
    if (!ALLOWED_HOSTS.has(target.hostname)) {
      throw new Error(`Refusing non-allowlisted upstream host: ${target.hostname}`);
    }
    return target;
  };

  return { ok: true, upstreamPath, needsCrumb, buildTarget };
}

// ---------------------------------------------------------------------------
// Cookie + crumb session (module-cached, ~30min TTL, single re-handshake on
// invalid crumb). fetchImpl is injectable for tests.
// ---------------------------------------------------------------------------
let session = { cookie: null, crumb: null, fetchedAt: 0 };

// Test-only reset so unit tests start from a clean session.
export function __resetSessionForTest() {
  session = { cookie: null, crumb: null, fetchedAt: 0 };
  buckets.clear();
}

async function getSession(fetchImpl, force = false) {
  if (!force && session.crumb && Date.now() - session.fetchedAt < SESSION_TTL_MS) {
    return session;
  }
  // 1) Hit fc.yahoo.com to collect the A1/A3 cookies (the 404 body is irrelevant).
  const r1 = await fetchImpl("https://fc.yahoo.com", { headers: { "User-Agent": UA } });
  const setCookies =
    typeof r1.headers.getSetCookie === "function"
      ? r1.headers.getSetCookie()
      : [r1.headers.get && r1.headers.get("set-cookie")].filter(Boolean);
  const cookie = setCookies.map((c) => String(c).split(";")[0]).join("; ");

  // 2) Exchange the cookie for a crumb.
  const r2 = await fetchImpl(`https://${PRIMARY_HOST}/v1/test/getcrumb`, {
    headers: { "User-Agent": UA, Cookie: cookie },
  });
  const crumb = (await r2.text()).trim();

  session = { cookie, crumb, fetchedAt: Date.now() };
  return session;
}

function isInvalidCrumb(status, body) {
  return status === 401 || (typeof body === "string" && /invalid crumb/i.test(body));
}

// ---------------------------------------------------------------------------
// Main handler. Portable across Vercel (req,res) and Vite connect (req,res,next).
// ---------------------------------------------------------------------------
export async function handleYahooProxy(req, res, opts = {}) {
  const fetchImpl = opts.fetchImpl || globalThis.fetch;
  const method = req.method || "GET";

  const resolved = resolveUpstream(req.url);
  if (!resolved.ok) {
    logLine(method, "(rejected)", resolved.status);
    res.statusCode = resolved.status;
    res.setHeader("Content-Type", "application/json");
    res.end(JSON.stringify({ error: resolved.error }));
    return;
  }

  const ip = clientIp(req);
  if (!rateLimitAllows(ip)) {
    logLine(method, resolved.upstreamPath, 429);
    res.statusCode = 429;
    res.setHeader("Content-Type", "application/json");
    res.end(JSON.stringify({ error: "Rate limit exceeded. Please slow down." }));
    return;
  }

  async function doUpstream(force) {
    let crumb = null;
    if (resolved.needsCrumb) {
      const s = await getSession(fetchImpl, force);
      crumb = s.crumb;
    }
    const target = resolved.buildTarget(crumb);
    const headers = { "User-Agent": UA, Accept: "application/json" };
    if (resolved.needsCrumb && session.cookie) headers.Cookie = session.cookie;
    const upstream = await fetchImpl(target.toString(), { headers });
    const body = await upstream.text();
    return { upstream, body };
  }

  try {
    let { upstream, body } = await doUpstream(false);

    // Single re-handshake on an invalid/expired crumb, then give up.
    if (resolved.needsCrumb && isInvalidCrumb(upstream.status, body)) {
      ({ upstream, body } = await doUpstream(true));
    }

    logLine(method, resolved.upstreamPath, upstream.status);

    const contentType = upstream.headers.get
      ? upstream.headers.get("content-type") || "application/json"
      : "application/json";
    res.statusCode = upstream.status;
    res.setHeader("Content-Type", contentType);
    res.setHeader("Cache-Control", "public, s-maxage=45, stale-while-revalidate=120");
    // NOTE: we deliberately do NOT forward any Set-Cookie / crumb to the browser.
    res.end(body);
  } catch (err) {
    logLine(method, resolved.upstreamPath, 502);
    res.statusCode = 502;
    res.setHeader("Content-Type", "application/json");
    // redactForLog also guards the error string from leaking a URL+crumb.
    res.end(JSON.stringify({ error: "Yahoo upstream request failed", detail: redactForLog(String(err)) }));
  }
}

export default handleYahooProxy;
