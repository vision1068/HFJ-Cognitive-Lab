import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  resolveUpstream,
  redactForLog,
  rateLimitAllows,
  ALLOWED_HOSTS,
  handleYahooProxy,
  __resetSessionForTest,
} from "../../api/yahoo/_yahooProxy.js";

beforeEach(() => __resetSessionForTest());

describe("NFR-5 host-pin + path allowlist", () => {
  it("pins the allowed upstream hosts", () => {
    expect(ALLOWED_HOSTS.has("query1.finance.yahoo.com")).toBe(true);
    expect(ALLOWED_HOSTS.has("query2.finance.yahoo.com")).toBe(true);
    expect(ALLOWED_HOSTS.has("evil.com")).toBe(false);
  });

  it("accepts the four allowlisted prefixes and marks crumb-gated ones", () => {
    expect(resolveUpstream("/api/yahoo/v8/finance/chart/AAPL?range=1mo")).toMatchObject({ ok: true, needsCrumb: false });
    expect(resolveUpstream("/api/yahoo/v7/finance/quote?symbols=AAPL")).toMatchObject({ ok: true, needsCrumb: true });
    expect(resolveUpstream("/api/yahoo/v10/finance/quoteSummary/AAPL")).toMatchObject({ ok: true, needsCrumb: true });
    expect(resolveUpstream("/api/yahoo/v1/finance/search?q=x")).toMatchObject({ ok: true, needsCrumb: true });
  });

  it("rejects a path not on the allowlist (400)", () => {
    const r = resolveUpstream("/api/yahoo/v99/secret/endpoint");
    expect(r.ok).toBe(false);
    expect(r.status).toBe(400);
  });

  it("rejects traversal / encoded-slash / userinfo host-escape attempts (400)", () => {
    expect(resolveUpstream("/api/yahoo/v8/finance/chart/../../evil").ok).toBe(false);
    expect(resolveUpstream("/api/yahoo/v8/finance/chart/x%2f..%2fevil").ok).toBe(false);
    expect(resolveUpstream("/api/yahoo/v7/finance/quote/@evil.com").ok).toBe(false);
    expect(resolveUpstream("/api/yahoo/v7/finance/quote\r\nHost: evil.com").ok).toBe(false);
  });

  it("only ever builds a URL on a pinned host", () => {
    const r = resolveUpstream("/api/yahoo/v7/finance/quote?symbols=AAPL");
    expect(r.ok).toBe(true);
    const target = (r as any).buildTarget("SOMECRUMB");
    expect(ALLOWED_HOSTS.has(target.hostname)).toBe(true);
    expect(target.searchParams.get("crumb")).toBe("SOMECRUMB");
  });
});

describe("secret redaction + rate limiting", () => {
  it("never echoes a crumb / cookie into logged strings", () => {
    expect(redactForLog("/v7/finance/quote?symbols=AAPL&crumb=abc123")).not.toContain("abc123");
    expect(redactForLog("boom fetching https://query1.finance.yahoo.com/v7/finance/quote?crumb=zzz")).not.toContain("zzz");
  });

  it("token bucket eventually rejects a hammering IP", () => {
    let allowedCount = 0;
    for (let i = 0; i < 200; i++) if (rateLimitAllows("1.2.3.4", 1_000_000)) allowedCount++;
    // Capacity is finite (60), so far fewer than 200 requests pass at a fixed instant.
    expect(allowedCount).toBeLessThan(200);
    expect(allowedCount).toBeGreaterThan(0);
  });
});

describe("Auditor condition: crumb never logged and never returned to the browser", () => {
  const CRUMB = "TOPSECRET_CRUMB_XYZ";
  const COOKIE = "A1=secretcookievalue";

  function fakeFetch(url: string) {
    if (url.startsWith("https://fc.yahoo.com")) {
      return Promise.resolve({
        status: 404,
        headers: { getSetCookie: () => [`${COOKIE}; Path=/; Secure`], get: () => null },
        text: async () => "",
      });
    }
    if (url.includes("/v1/test/getcrumb")) {
      return Promise.resolve({ status: 200, headers: { get: () => "text/plain" }, text: async () => CRUMB });
    }
    // The real upstream v7 call — echo back a body that does NOT contain the crumb.
    return Promise.resolve({
      status: 200,
      headers: { get: (k: string) => (k.toLowerCase() === "content-type" ? "application/json" : null) },
      text: async () => JSON.stringify({ quoteResponse: { result: [{ symbol: "AAPL" }], error: null } }),
    });
  }

  it("keeps the crumb out of logs and out of the HTTP response body", async () => {
    const logSpy = vi.spyOn(console, "log").mockImplementation(() => {});

    const req = { url: "/api/yahoo/v7/finance/quote?symbols=AAPL", method: "GET", headers: {}, socket: { remoteAddress: "9.9.9.9" } };
    let body = "";
    const res = {
      statusCode: 0,
      setHeader: () => {},
      end: (b: string) => {
        body = b;
      },
    };

    await handleYahooProxy(req as any, res as any, { fetchImpl: fakeFetch as any });

    // The crumb must never appear in the response returned to the browser.
    expect(body).not.toContain(CRUMB);
    expect(body).not.toContain(COOKIE);
    // The crumb must never appear in any logged line.
    const logged = logSpy.mock.calls.flat().join(" ");
    expect(logged).not.toContain(CRUMB);
    expect(logged).not.toContain(COOKIE);
    expect(res.statusCode).toBe(200);

    logSpy.mockRestore();
  });
});
