import type { MarketIndex, PricePoint } from "@/types";
import { PSX_BASE, fetchJson, fetchText } from "./http";

// ---------------------------------------------------------------------------
// PSX timeseries (end-of-day). Real JSON from dps.psx.com.pk.
// Response: { status, message, data: [[unixSeconds, value, volume, ...], ...] }
// Rows are newest-first. `value` is the closing price/index level.
// ---------------------------------------------------------------------------
interface PsxTimeseries {
  status: number;
  message: string;
  data: [number, number, number, number?][];
}

export interface EodSeries {
  history: PricePoint[]; // oldest -> newest
  latest: number;
  previous: number;
}

export async function fetchPsxEod(symbol: string): Promise<EodSeries> {
  const json = await fetchJson<PsxTimeseries>(`${PSX_BASE}/timeseries/eod/${encodeURIComponent(symbol)}`);
  if (json.status !== 1 || !Array.isArray(json.data) || json.data.length === 0) {
    throw new Error(`No PSX EOD data for ${symbol}`);
  }
  // newest-first -> oldest-first
  const rows = [...json.data].sort((a, b) => a[0] - b[0]);
  const history: PricePoint[] = rows.map((r) => {
    const close = r[1];
    return {
      date: new Date(r[0] * 1000).toISOString().slice(0, 10),
      // The EOD feed only exposes a single value + volume; intraday O/H/L are not
      // published here, so we do not fabricate them — charts use close only.
      open: close,
      high: close,
      low: close,
      close,
      volume: r[2] ?? 0,
    };
  });
  const latest = history[history.length - 1].close;
  const previous = history.length > 1 ? history[history.length - 2].close : latest;
  return { history, latest, previous };
}

// ---------------------------------------------------------------------------
// PSX company page (HTML). Real fundamentals parsed from stats_label/stats_value
// pairs plus the quote header. Any field we cannot locate is returned null and the
// UI shows "Data not available" rather than a fabricated value.
// ---------------------------------------------------------------------------
export interface PsxFundamentals {
  name: string | null;
  psxSector: string | null;
  price: number | null;
  ldcp: number | null; // last day closing price (previous close)
  open: number | null;
  high: number | null;
  low: number | null;
  volume: number | null;
  peRatio: number | null;
  marketCap: number | null; // PKR
  sharesOutstanding: number | null;
  eps: number | null;
}

function stripTags(s: string): string {
  return s.replace(/<[^>]+>/g, "").replace(/&amp;/g, "&").trim();
}

function toNumber(raw: string | undefined | null): number | null {
  if (raw == null) return null;
  const cleaned = raw.replace(/[^\d.-]/g, "");
  if (cleaned === "" || cleaned === "-" || cleaned === ".") return null;
  const n = Number(cleaned);
  return Number.isFinite(n) ? n : null;
}

export async function fetchPsxCompany(symbol: string): Promise<PsxFundamentals> {
  const html = await fetchText(`${PSX_BASE}/company/${encodeURIComponent(symbol)}`);

  // Ordered stats_label -> stats_value pairs; keep FIRST occurrence of each label
  // (the main quote block precedes sector-peer blocks that reuse the same labels).
  const stats = new Map<string, string>();
  const re = /stats_label">([\s\S]*?)<\/div>\s*<div class="stats_value">([^<]*)<\/div>/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(html)) !== null) {
    const label = stripTags(m[1]).toLowerCase();
    if (!stats.has(label)) stats.set(label, m[2].trim());
  }
  const stat = (needle: string): string | undefined => {
    for (const [label, value] of stats) if (label.includes(needle)) return value;
    return undefined;
  };

  const nameMatch = html.match(/quote__name">([\s\S]*?)<\/div>/);
  const sectorMatch = html.match(/quote__sector">\s*<span>([\s\S]*?)<\/span>/);
  const priceMatch = html.match(/quote__close">\s*Rs\.?\s*([0-9,]+\.?[0-9]*)/i);
  // First <td>EPS</td> value in the financials table.
  const epsMatch = html.match(/<td>\s*EPS\s*<\/td>\s*<td[^>]*>\s*<span[^>]*>([0-9,.\-()]+)<\/span>/i);

  const marketCapThousands = toNumber(stat("market cap"));

  return {
    name: nameMatch ? stripTags(nameMatch[1]) : null,
    psxSector: sectorMatch ? stripTags(sectorMatch[1]) : null,
    price: priceMatch ? toNumber(priceMatch[1]) : null,
    ldcp: toNumber(stat("ldcp")),
    open: toNumber(stat("open")),
    high: toNumber(stat("high")),
    low: toNumber(stat("low")),
    volume: toNumber(stat("volume")),
    peRatio: toNumber(stat("p/e")),
    marketCap: marketCapThousands != null ? marketCapThousands * 1000 : null,
    sharesOutstanding: toNumber(stat("shares")),
    eps: epsMatch ? toNumber(epsMatch[1].replace(/[()]/g, "")) : null,
  };
}

// ---------------------------------------------------------------------------
// PSX indices (KSE-100, KMI-30) from the timeseries endpoint — real JSON.
// ---------------------------------------------------------------------------
const INDEX_DEFS: { name: string; symbol: string }[] = [
  { name: "KSE-100", symbol: "KSE100" },
  { name: "KMI-30", symbol: "KMI30" },
];

export async function fetchPsxIndices(): Promise<MarketIndex[]> {
  const results = await Promise.all(
    INDEX_DEFS.map(async (def) => {
      const eod = await fetchPsxEod(def.symbol);
      const changeAbs = eod.latest - eod.previous;
      const changePercent = eod.previous ? (changeAbs / eod.previous) * 100 : 0;
      return {
        name: def.name,
        symbol: def.symbol,
        value: eod.latest,
        changeAbs,
        changePercent,
        source: "PSX Data Portal",
      } satisfies MarketIndex;
    })
  );
  return results;
}
