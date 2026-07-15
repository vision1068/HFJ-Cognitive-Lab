// dataBinding — assembles the immutable PSX data context every builder reads.
//
// PSX-ONLY ENFORCEMENT (Scope decision S4): this engine has NO global / FMP
// branch. It sources everything from `fetchCompanyDetail` (PSX Data Portal),
// assumes PKR, and treats any figure PSX does not publish as `na`. There is
// deliberately no code path that reaches a non-PSX provider.
//
// PROVENANCE RULE (data contract): price-derived figures are dated by the true
// EOD date (last priceHistory[].date) and sourced "PSX Data Portal". Snapshot
// fundamentals scraped now (P/E, market cap, EPS) are dated by `lastUpdated`
// and sourced "PSX Data Portal (snapshot)". Fetch time is NEVER presented as a
// price-series date.

import type { Company } from "@/types";
import { psxTickers, psxTickerBySymbol } from "@/data/psxTickers";
import { fetchCompanyDetail } from "../companies";
import type { Duration } from "./types";
import { median } from "./priceMath";

export const PRICE_SOURCE = "PSX Data Portal";
export const SNAPSHOT_SOURCE = "PSX Data Portal (snapshot)";

// A same-sector peer reduced to the comparable snapshot fields.
export interface PeerSnapshot {
  ticker: string;
  name: string;
  sector: string;
  price: number | null;
  peRatio: number | null;
  marketCap: number | null;
  asOf: string; // peer's snapshot timestamp (lastUpdated)
}

export interface PredicContext {
  ticker: string;
  duration: Duration;
  company: Company;
  peers: PeerSnapshot[];
  peerMedianPE: number | null;
  peerMedianMarketCap: number | null;
  // Provenance handles shared by every builder.
  priceAsOf: string | null; // last EOD date in priceHistory, or null when empty
  snapshotAsOf: string; // company.lastUpdated
  priceSource: string;
  snapshotSource: string;
}

function lastPriceDate(company: Company): string | null {
  const h = company.priceHistory;
  if (!h || h.length === 0) return null;
  return h[h.length - 1].date;
}

/**
 * Build the bound context for one ticker + horizon.
 *
 * Peers are the curated same-sector PSX companies (excluding self), fetched with
 * Promise.allSettled — a peer that fails to load simply drops from the
 * comparison and NEVER fails the report.
 */
export async function buildContext(ticker: string, duration: Duration): Promise<PredicContext> {
  const company = await fetchCompanyDetail(ticker);

  // Anchor the sector on the curated label when we have one, so peer matching is
  // stable even if the live snapshot returns a raw PSX sector string.
  const selfMeta = psxTickerBySymbol[ticker];
  const targetSector = selfMeta?.sector ?? company.sector;

  const peerMetas = psxTickers.filter(
    (t) => t.ticker !== ticker && t.sector === targetSector,
  );

  const settled = await Promise.allSettled(
    peerMetas.map((m) => fetchCompanyDetail(m.ticker)),
  );

  const peers: PeerSnapshot[] = [];
  for (const r of settled) {
    if (r.status === "fulfilled" && r.value) {
      const c = r.value;
      peers.push({
        ticker: c.ticker,
        name: c.name,
        sector: c.sector,
        price: c.price,
        peRatio: c.peRatio,
        marketCap: c.marketCap,
        asOf: c.lastUpdated,
      });
    }
  }

  const peerMedianPE = median(
    peers.map((p) => p.peRatio).filter((n): n is number => n != null && n > 0),
  );
  const peerMedianMarketCap = median(
    peers.map((p) => p.marketCap).filter((n): n is number => n != null && n > 0),
  );

  return {
    ticker: company.ticker,
    duration,
    company,
    peers,
    peerMedianPE,
    peerMedianMarketCap,
    priceAsOf: lastPriceDate(company),
    snapshotAsOf: company.lastUpdated,
    priceSource: PRICE_SOURCE,
    snapshotSource: SNAPSHOT_SOURCE,
  };
}
