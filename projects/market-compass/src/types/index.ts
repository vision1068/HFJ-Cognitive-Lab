// Market Compass — PSX-only real-data model.
// All market data is sourced live from the Pakistan Stock Exchange Data Portal
// (dps.psx.com.pk) and Yahoo Finance (USD/PKR only). Fields that no free data
// source can provide are typed as nullable and rendered as "Data not available".

// Widened to string: PSX prices are PKR, but global symbols (reached via search
// / the detail page) carry their own currency (USD, EUR, …). formatCurrency
// renders a known symbol per code and falls back gracefully for the rest.
export type Currency = string;

// Widened to string so global companies can report their real exchange
// (NasdaqGS, NYSE, …) rather than being forced to "PSX".
export type Exchange = string;

export type Sector =
  | "Banking"
  | "Fertilizer"
  | "Cement"
  | "Oil & Gas"
  | "Power"
  | "Technology";

export interface PricePoint {
  date: string; // ISO yyyy-mm-dd
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

// A tradable symbol in the PSX universe (from dps.psx.com.pk/symbols or the
// committed fallback). Descriptive only — carries no price/valuation.
export interface UniverseSymbol {
  symbol: string; // PSX symbol, e.g. "OGDC"
  name: string;
  sector: string;
}

// One symbol's live quote + fundamentals as mapped from a Yahoo v7 batch-quote
// row. Every numeric field is nullable — a missing field is rendered as
// "Data not available", never fabricated.
export interface BatchQuote {
  symbol: string; // symbol exactly as Yahoo returned it, e.g. "OGDC.KA" or "AAPL"
  name: string | null;
  price: number | null;
  change: number | null;
  changePercent: number | null;
  trailingPE: number | null;
  eps: number | null;
  marketCap: number | null;
  volume: number | null;
  week52High: number | null;
  week52Low: number | null;
  quoteType: string | null; // Yahoo classification, e.g. "EQUITY" / "MUTUALFUND"
  currency: string | null;
  exchange: string | null; // Yahoo exchange code, e.g. "KAR" (Karachi) / "NMS"
  asOf: string | null; // ISO timestamp from regularMarketTime
  // False when quoteType/currency validation fails for this row, meaning the
  // fundamental fields (trailingPE/eps/marketCap) must NOT be shown as an
  // equity's. Price/change may still be valid; only fundamentals are gated.
  fundamentalsAvailable: boolean;
}

// Result of a batch-quote call: mapped quotes plus reconciliation of which
// requested symbols Yahoo silently dropped (so callers never shrink the set).
export interface BatchQuoteResult {
  quotes: BatchQuote[];
  missing: string[]; // requested symbols that were NOT returned by Yahoo
}

export type SearchSource = "psx" | "yahoo";

// A single ranked search hit (from the local PSX universe or Yahoo v1 search).
export interface SearchResult {
  ticker: string; // PSX symbol or global Yahoo symbol
  name: string;
  exchange: string; // "PSX" for local hits, Yahoo exchDisp for global hits
  source: SearchSource;
  score: number; // ranking score, higher = better match
}

// One fiscal period of financial-statement data. Any line item a source does
// not publish is null. PSX symbols have no free statements source at all.
export interface FinancialStatement {
  fiscalPeriodEnd: string; // ISO date, e.g. "2025-09-30"
  revenue: number | null;
  netIncome: number | null;
  totalAssets: number | null;
  operatingCashFlow: number | null;
  freeCashFlow: number | null;
}

// Multi-year statement set for a symbol. `available:false` (with a reason) is
// the honest state for PSX symbols and any global symbol Yahoo 404s.
export interface StatementSet {
  symbol: string;
  available: boolean;
  reason?: string; // populated only when available === false
  currency: string | null;
  source: string | null; // "Yahoo Finance" when available, else null
  statements: FinancialStatement[]; // newest-first, as Yahoo returns them
}

export interface Company {
  ticker: string;
  psxSymbol: string; // symbol used on PSX endpoints
  name: string;
  logoInitials: string;
  logoColor: string;
  exchange: Exchange;
  // "Pakistan" for PSX companies; may be empty/other for global companies.
  country: string;
  // True for PSX-listed companies, false for global (e.g. Yahoo search hits).
  // Drives, among other things, which symbol form the statements service is asked
  // for (PSX -> honest N/A, global -> real multi-year statements).
  isPsx: boolean;
  // Widened from the curated `Sector` union to string: the full PSX universe has
  // many more sector labels than the curated top-10 six. Curated companies still
  // carry a `Sector` value (assignable to string); universe companies carry the
  // raw PSX sector label.
  sector: string;
  industry: string;
  currency: Currency;
  description: string;
  website: string;
  ceo: string;

  // --- Live market data (real, from PSX) ---
  price: number;
  previousClose: number;
  changeAbs: number;
  changePercent: number;
  dayOpen: number | null;
  dayHigh: number | null;
  dayLow: number | null;
  volume: number | null;
  week52High: number | null;
  week52Low: number | null;

  // --- Fundamentals (real, from PSX company page; null when unavailable) ---
  marketCap: number | null; // in PKR
  peRatio: number | null;
  eps: number | null;
  sharesOutstanding: number | null;
  psxSector: string | null; // official PSX sector label

  sparkline: number[];
  priceHistory: PricePoint[];

  priceSource: string;
  fundamentalsSource: string | null;
  lastUpdated: string; // ISO timestamp

  // --- Provenance / freshness (added for the live-data enhancement) ---
  asOf: string | null; // when the live quote was captured (Yahoo regularMarketTime), ISO
  quoteType: string | null; // Yahoo classification of the security, when known
  fundamentalsAvailable: boolean; // true only when real fundamentals were obtained
  statementsSource: string | null; // "Yahoo Finance" if statements exist, else null (PSX = null)
}

export interface MarketIndex {
  name: string;
  symbol: string;
  value: number;
  changePercent: number;
  changeAbs: number;
  source: string;
}

export interface FxRate {
  pair: string; // "USD/PKR"
  rate: number;
  changePercent: number;
  source: string;
}

export interface WatchlistEntry {
  ticker: string;
  addedAt: string;
}

export interface Watchlist {
  id: string;
  name: string;
  group: string;
  entries: WatchlistEntry[];
}

export interface PortfolioHolding {
  id: string;
  ticker: string;
  shares: number;
  purchasePrice: number;
  purchaseDate: string;
  currency: Currency;
}

export interface PriceAlert {
  id: string;
  ticker: string;
  type: "above" | "below";
  targetPrice: number;
  active: boolean;
  createdAt: string;
}
