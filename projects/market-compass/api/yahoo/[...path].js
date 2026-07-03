// Serverless proxy for Yahoo Finance (production) — used only for USD/PKR.
// Deploy target: Vercel / Netlify Functions (Node runtime).
const UA =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

export default async function handler(req, res) {
  try {
    const url = new URL(req.url, "http://localhost");
    const upstreamPath = url.pathname.replace(/^\/api\/yahoo/, "");
    const target = `https://query1.finance.yahoo.com${upstreamPath}${url.search}`;

    const upstream = await fetch(target, {
      headers: { "User-Agent": UA, Accept: "application/json" },
    });

    const body = await upstream.text();
    res.setHeader("Content-Type", upstream.headers.get("content-type") || "application/json");
    res.setHeader("Cache-Control", "public, s-maxage=45, stale-while-revalidate=120");
    res.status(upstream.status).send(body);
  } catch (err) {
    res.status(502).json({ error: "Yahoo upstream request failed", detail: String(err) });
  }
}
