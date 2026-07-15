import type { Context } from "@netlify/functions";

const UA =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

const PSX_HOST = "dps.psx.com.pk";

export default async function handler(req: Request, context: Context): Promise<Response> {
  try {
    const url = new URL(req.url);
    const rawPath = url.pathname;

    // Reject hostile encodings before parsing (parity with Vercel version).
    if (/%2f|%5c|%2e%2e|\\|\r|\n/i.test(rawPath)) {
      return new Response(JSON.stringify({ error: "Illegal characters in request path" }), {
        status: 400,
        headers: { "Content-Type": "application/json" },
      });
    }

    // Strip whichever prefix is present. Direct invocation sees
    // /.netlify/functions/psx/...; invocation via the netlify.toml redirect
    // (how the real app reaches this function, at /api/psx/*) sees the
    // ORIGINAL pre-rewrite path, i.e. /api/psx/... — Netlify does not
    // rewrite req.url for status=200 redirects to functions.
    const upstreamPath = rawPath
      .replace(/^\/\.netlify\/functions\/psx/, "")
      .replace(/^\/api\/psx/, "");
    if (!upstreamPath.startsWith("/") || upstreamPath.includes("..") || upstreamPath.includes("@")) {
      return new Response(JSON.stringify({ error: "Illegal upstream path" }), {
        status: 400,
        headers: { "Content-Type": "application/json" },
      });
    }

    // Construct the full upstream URL with query string.
    const upstreamUrl = new URL(`https://${PSX_HOST}${upstreamPath}${url.search}`);

    // Forward the request upstream.
    const upstreamReq = await fetch(upstreamUrl.toString(), {
      method: req.method,
      headers: {
        "User-Agent": UA,
        ...Object.fromEntries(
          [...req.headers.entries()].filter(
            ([k]) => !["host", "connection", "keep-alive"].includes(k.toLowerCase())
          )
        ),
      },
    });

    // Pass through the upstream response.
    const body = await upstreamReq.arrayBuffer();
    return new Response(body, {
      status: upstreamReq.status,
      statusText: upstreamReq.statusText,
      headers: new Headers(upstreamReq.headers),
    });
  } catch (error) {
    console.error("[psx-proxy]", error);
    return new Response(JSON.stringify({ error: "Proxy error" }), {
      status: 502,
      headers: { "Content-Type": "application/json" },
    });
  }
}
