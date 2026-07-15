import type { SearchResult, UniverseSymbol } from "@/types";
import { YAHOO_BASE, fetchJson } from "./http";
import { getFallbackUniverse } from "./universe";

// FR-5: "smart" search — a deterministic local fuzzy match over the PSX universe
// (this is NOT AI), merged with Yahoo v1 search for GLOBAL symbols (Yahoo returns
// zero PSX hits, so PSX name search MUST be local). Ranking:
//   exact ticker > ticker prefix > name(word) prefix > ticker substring > name substring.

// Score tiers — local matches always outrank best-effort global hits.
const SCORE = {
  exactTicker: 1000,
  tickerPrefix: 800,
  nameWordPrefix: 650,
  namePrefix: 640,
  tickerSubstring: 500,
  nameSubstring: 400,
  globalBase: 350,
} as const;

/** Lowercase + strip diacritics so "peñoles" matches "penoles". */
export function normalize(s: string): string {
  return s
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase()
    .trim();
}

function scoreRow(nq: string, row: UniverseSymbol): number {
  const sym = row.symbol.toLowerCase();
  const name = normalize(row.name);
  if (sym === nq) return SCORE.exactTicker;
  if (sym.startsWith(nq)) return SCORE.tickerPrefix;
  if (name.startsWith(nq)) return SCORE.namePrefix;
  if (name.split(/[^a-z0-9]+/).some((w) => w.length > 0 && w.startsWith(nq))) return SCORE.nameWordPrefix;
  if (sym.includes(nq)) return SCORE.tickerSubstring;
  if (name.includes(nq)) return SCORE.nameSubstring;
  return 0;
}

/** Pure, testable local search over a supplied universe. */
export function searchLocal(q: string, universe: UniverseSymbol[]): SearchResult[] {
  const nq = normalize(q);
  if (nq.length === 0) return [];
  const hits: SearchResult[] = [];
  for (const row of universe) {
    const score = scoreRow(nq, row);
    if (score > 0) {
      hits.push({ ticker: row.symbol, name: row.name, exchange: "PSX", source: "psx", score });
    }
  }
  return hits;
}

interface YahooSearchQuote {
  symbol?: string;
  shortname?: string;
  longname?: string;
  exchDisp?: string;
  quoteType?: string;
}
interface YahooSearchResponse {
  quotes?: YahooSearchQuote[];
}

/** Best-effort global search via Yahoo v1; returns [] on any failure. */
export async function searchGlobal(q: string): Promise<SearchResult[]> {
  const nq = q.trim();
  if (nq.length === 0) return [];
  try {
    const json = await fetchJson<YahooSearchResponse>(
      `${YAHOO_BASE}/v1/finance/search?q=${encodeURIComponent(nq)}&quotesCount=10&newsCount=0`
    );
    const quotes = (json.quotes ?? []).filter((x) => x.quoteType === "EQUITY" && x.symbol);
    return quotes.map((x, i) => ({
      ticker: x.symbol as string,
      name: x.longname ?? x.shortname ?? (x.symbol as string),
      exchange: x.exchDisp ?? "",
      source: "yahoo" as const,
      // Preserve Yahoo's own ranking order while keeping globals below local hits.
      score: SCORE.globalBase - i,
    }));
  } catch {
    return [];
  }
}

function rankAndDedupe(results: SearchResult[]): SearchResult[] {
  const best = new Map<string, SearchResult>();
  for (const r of results) {
    const existing = best.get(r.ticker);
    if (!existing || r.score > existing.score) best.set(r.ticker, r);
  }
  return Array.from(best.values()).sort((a, b) => b.score - a.score || a.ticker.localeCompare(b.ticker));
}

/**
 * FR-5: merged local + global search. `universe` defaults to the committed
 * fallback so callers that have not loaded the live universe still work.
 */
export async function searchCompanies(q: string, universe: UniverseSymbol[] = getFallbackUniverse()): Promise<SearchResult[]> {
  const [local, global] = await Promise.all([Promise.resolve(searchLocal(q, universe)), searchGlobal(q)]);
  return rankAndDedupe([...local, ...global]);
}
