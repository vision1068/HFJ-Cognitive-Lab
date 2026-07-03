// Serverless proxy for the PSX Data Portal (production).
// Deploy target: Vercel / Netlify Functions (Node runtime). Mirrors the Vite dev
// proxy so the browser only ever calls same-origin /api/psx/* and never hits CORS.
const UA =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

export default async function handler(req, res) {
  try {
    const url = new URL(req.url, "http://localhost");
    // Strip the /api/psx prefix; forward the rest (path + query) upstream.
    const upstreamPath = url.pathname.replace(/^\/api\/psx/, "");
    const target = `https://dps.psx.com.pk${upstreamPath}${url.search}`;

    const upstream = await fetch(target, {
      headers: { "User-Agent": UA, Accept: "*/*" },
    });

    const body = await upstream.text();
    res.setHeader("Content-Type", upstream.headers.get("content-type") || "text/plain");
    res.setHeader("Cache-Control", "public, s-maxage=45, stale-while-revalidate=120");
    res.status(upstream.status).send(body);
  } catch (err) {
    res.status(502).json({ error: "PSX upstream request failed", detail: String(err) });
  }
}
