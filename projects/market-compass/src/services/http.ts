// All external market-data requests are routed through same-origin proxy paths to
// avoid browser CORS restrictions on the PSX Data Portal and Yahoo Finance.
//   /api/psx/*   -> https://dps.psx.com.pk/*      (dev: vite proxy, prod: serverless fn)
//   /api/yahoo/* -> https://query1.finance.yahoo.com/*
export const PSX_BASE = "/api/psx";
export const YAHOO_BASE = "/api/yahoo";

// GitHub Pages is a static host — it cannot run the /api/psx and /api/yahoo
// serverless proxy functions, so PSX/Yahoo requests would otherwise fail with an
// opaque CORS error there. The Pages deploy workflow sets VITE_DEPLOY_TARGET=static
// so we can surface an honest "not available in this deployment" message instead.
const IS_STATIC_HOST = import.meta.env.VITE_DEPLOY_TARGET === "static";

function assertProxyAvailable(url: string): void {
  if (IS_STATIC_HOST && (url.startsWith(PSX_BASE) || url.startsWith(YAHOO_BASE))) {
    throw new Error(
      "Live PSX/Yahoo data isn't available on this static GitHub Pages deployment (no server-side proxy). Run locally with `npm run dev`, or deploy to a host with serverless functions (Vercel/Netlify)."
    );
  }
}

export async function fetchJson<T>(url: string): Promise<T> {
  assertProxyAvailable(url);
  const res = await fetch(url, { headers: { Accept: "application/json" } });
  if (!res.ok) throw new Error(`Request failed (${res.status}) for ${url}`);
  return (await res.json()) as T;
}

export async function fetchText(url: string): Promise<string> {
  assertProxyAvailable(url);
  const res = await fetch(url);
  if (!res.ok) throw new Error(`Request failed (${res.status}) for ${url}`);
  return await res.text();
}
