// Serverless proxy for the PSX Data Portal (production).
// Deploy target: Vercel / Netlify Functions (Node runtime). Mirrors the Vite dev
// proxy so the browser only ever calls same-origin /api/psx/* and never hits CORS.
const UA =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

const PSX_HOST = "dps.psx.com.pk";

export default async function handler(req, res) {
  try {
    const rawUrl = req.url || "";
    // Reject hostile encodings before parsing (parity with the Yahoo proxy).
    if (/%2f|%5c|%2e%2e|\\|\r|\n/i.test(rawUrl)) {
      res.statusCode = 400;
      res.setHeader("Content-Type", "application/json");
      res.end(JSON.stringify({ error: "Illegal characters in request path" }));
      return;
    }

    const url = new URL(rawUrl, "http://localhost");
    // Strip the /api/psx prefix; forward the rest (path + query) upstream.
    const upstreamPath = url.pathname.replace(/^\/api\/psx/, "");
    if (!upstreamPath.startsWith("/") || upstreamPath.includes("..") || upstreamPath.includes("@")) {
      res.statusCode = 400;
      res.setHeader("Content-Type", "application/json");
      res.end(JSON.stringify({ error: "Illegal upstream path" }));
      return;
    }

    const target = new URL(`https://${PSX_HOST}${upstreamPath}`);
    target.search = url.search;
    // SECURITY: assert the fully-resolved host is EXACTLY the pinned PSX host.
    if (target.hostname !== PSX_HOST) {
      res.statusCode = 400;
      res.setHeader("Content-Type", "application/json");
      res.end(JSON.stringify({ error: "Refusing non-allowlisted upstream host" }));
      return;
    }

    const upstream = await fetch(target.toString(), {
      headers: { "User-Agent": UA, Accept: "*/*" },
    });

    const body = await upstream.text();
    res.statusCode = upstream.status;
    res.setHeader("Content-Type", upstream.headers.get("content-type") || "text/plain");
    res.setHeader("Cache-Control", "public, s-maxage=45, stale-while-revalidate=120");
    res.end(body);
  } catch (err) {
    res.statusCode = 502;
    res.setHeader("Content-Type", "application/json");
    res.end(JSON.stringify({ error: "PSX upstream request failed", detail: String(err) }));
  }
}
