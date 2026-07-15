import type { Context } from "@netlify/functions";

const UA =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

const PRIMARY_HOST = "query1.finance.yahoo.com";
const ALLOWED_HOSTS = new Set(["query1.finance.yahoo.com", "query2.finance.yahoo.com"]);

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

// Logging — redacts crumb / cookie / query string so a secret can never reach the log sink.
const SECRET_KEYS = /(crumb|cookie|set-cookie|a1|a3)/i;
function redactForLog(value: string | null | undefined): string {
  if (value == null) return "";
  const s = String(value);
  const noQuery = s.split("?")[0];
  return noQuery.replace(/([?&][^=]*=)[^&\s]*/g, (m) =>
    SECRET_KEYS.test(m) ? m.replace(/=.*/, "=[REDACTED]") : m
  );
}

function logLine(method: string, upstreamPath: string, status: number): void {
  console.log(`[yahoo-proxy] ${method} ${redactForLog(upstreamPath)} -> ${status}`);
}

// Per-IP token bucket (best-effort, ephemeral per serverless instance).
const BUCKET_CAPACITY = 60;
const BUCKET_REFILL_PER_SEC = 1;
const buckets = new Map<string, { tokens: number; ts: number }>();

function rateLimitAllows(ip: string, now: number = Date.now()): boolean {
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

function clientIp(req: Request, context: Context): string {
  const xff = req.headers.get("x-forwarded-for");
  if (xff) return xff.split(",")[0].trim();
  return context.clientContext?.identity?.clientIp || "unknown";
}

// Request validation + URL construction.
function resolveUpstream(rawUrl: string): { ok: true; upstreamPath: string; needsCrumb: boolean } | { ok: false; status: number; error: string } {
  if (!rawUrl || rawUrl.length === 0) {
    return { ok: false, status: 400, error: "Missing request URL" };
  }

  // Reject hostile encodings before parsing.
  if (/%2f|%5c|%2e%2e|\\|\r|\n/i.test(rawUrl)) {
    return { ok: false, status: 400, error: "Illegal characters in request path" };
  }

  let url: URL;
  try {
    url = new URL(rawUrl, "http://localhost");
  } catch {
    return { ok: false, status: 400, error: "Malformed request URL" };
  }

  // Strip whichever prefix is present. Direct invocation sees
  // /.netlify/functions/yahoo/...; invocation via the netlify.toml redirect
  // (how the real app reaches this function, at /api/yahoo/*) sees the
  // ORIGINAL pre-rewrite path, i.e. /api/yahoo/... — Netlify does not
  // rewrite req.url for status=200 redirects to functions.
  const upstreamPath = url.pathname
    .replace(/^\/\.netlify\/functions\/yahoo/, "")
    .replace(/^\/api\/yahoo/, "");
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
  return { ok: true, upstreamPath, needsCrumb };
}

// Session cache (module-level, ~30min TTL).
let session: { cookie: string | null; crumb: string | null; fetchedAt: number } = {
  cookie: null,
  crumb: null,
  fetchedAt: 0,
};

async function getSession(force: boolean = false): Promise<{ cookie: string; crumb: string }> {
  if (!force && session.crumb && Date.now() - session.fetchedAt < SESSION_TTL_MS) {
    return { cookie: session.cookie || "", crumb: session.crumb };
  }

  // 1) Hit fc.yahoo.com to collect A1/A3 cookies.
  const r1 = await fetch("https://fc.yahoo.com", { headers: { "User-Agent": UA } });
  const setCookieHeader = r1.headers.get("set-cookie");
  const cookie = setCookieHeader ? setCookieHeader.split(";")[0] : "";

  // 2) Exchange the cookie for a crumb.
  const r2 = await fetch(`https://${PRIMARY_HOST}/v1/test/getcrumb`, {
    headers: { "User-Agent": UA, Cookie: cookie },
  });
  const crumb = (await r2.text()).trim();

  session = { cookie, crumb, fetchedAt: Date.now() };
  return { cookie, crumb };
}

function isInvalidCrumb(status: number, body: string): boolean {
  return status === 401 || /invalid crumb/i.test(body);
}

// Main handler.
export default async function handler(req: Request, context: Context): Promise<Response> {
  const method = req.method || "GET";
  const resolved = resolveUpstream(req.url);

  if (!resolved.ok) {
    logLine(method, "(rejected)", resolved.status);
    return new Response(JSON.stringify({ error: resolved.error }), {
      status: resolved.status,
      headers: { "Content-Type": "application/json" },
    });
  }

  const ip = clientIp(req, context);
  if (!rateLimitAllows(ip)) {
    logLine(method, resolved.upstreamPath, 429);
    return new Response(JSON.stringify({ error: "Rate limit exceeded. Please slow down." }), {
      status: 429,
      headers: { "Content-Type": "application/json" },
    });
  }

  try {
    const url = new URL(req.url);
    const params = new URLSearchParams(url.search);

    let crumb = "";
    if (resolved.needsCrumb) {
      const sess = await getSession(false);
      crumb = sess.crumb;
      params.set("crumb", crumb);
    }

    // Build and validate target URL.
    const target = new URL(`https://${PRIMARY_HOST}${resolved.upstreamPath}`);
    target.search = params.toString();
    if (!ALLOWED_HOSTS.has(target.hostname)) {
      throw new Error(`Refusing non-allowlisted upstream host: ${target.hostname}`);
    }

    const headers: Record<string, string> = { "User-Agent": UA, Accept: "application/json" };
    if (resolved.needsCrumb && session.cookie) headers.Cookie = session.cookie;

    let upstream = await fetch(target.toString(), { headers });
    let body = await upstream.text();

    // Single re-handshake on invalid crumb, then give up.
    if (resolved.needsCrumb && isInvalidCrumb(upstream.status, body)) {
      const sessRefresh = await getSession(true);
      const paramsRefresh = new URLSearchParams(url.search);
      paramsRefresh.set("crumb", sessRefresh.crumb);
      const targetRefresh = new URL(`https://${PRIMARY_HOST}${resolved.upstreamPath}`);
      targetRefresh.search = paramsRefresh.toString();
      const headersRefresh = { ...headers, Cookie: sessRefresh.cookie };
      upstream = await fetch(targetRefresh.toString(), { headers: headersRefresh });
      body = await upstream.text();
    }

    logLine(method, resolved.upstreamPath, upstream.status);

    const contentType = upstream.headers.get("content-type") || "application/json";
    return new Response(body, {
      status: upstream.status,
      headers: {
        "Content-Type": contentType,
        "Cache-Control": "public, s-maxage=45, stale-while-revalidate=120",
      },
    });
  } catch (err) {
    logLine(method, resolved.upstreamPath, 502);
    return new Response(
      JSON.stringify({
        error: "Yahoo upstream request failed",
        detail: redactForLog(String(err)),
      }),
      {
        status: 502,
        headers: { "Content-Type": "application/json" },
      }
    );
  }
}
