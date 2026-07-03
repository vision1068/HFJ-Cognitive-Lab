// All external market-data requests are routed through same-origin proxy paths to
// avoid browser CORS restrictions on the PSX Data Portal and Yahoo Finance.
//   /api/psx/*   -> https://dps.psx.com.pk/*      (dev: vite proxy, prod: serverless fn)
//   /api/yahoo/* -> https://query1.finance.yahoo.com/*
export const PSX_BASE = "/api/psx";
export const YAHOO_BASE = "/api/yahoo";

export async function fetchJson<T>(url: string): Promise<T> {
  const res = await fetch(url, { headers: { Accept: "application/json" } });
  if (!res.ok) throw new Error(`Request failed (${res.status}) for ${url}`);
  return (await res.json()) as T;
}

export async function fetchText(url: string): Promise<string> {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`Request failed (${res.status}) for ${url}`);
  return await res.text();
}
