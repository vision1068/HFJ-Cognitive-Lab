// Market Compass — PSX-only real-data model.
// All market data is sourced live from the Pakistan Stock Exchange Data Portal
// (dps.psx.com.pk) and Yahoo Finance (USD/PKR only). Fields that no free data
// source can provide are typed as nullable and rendered as "Data not available".

export type Currency = "PKR";

export type Exchange = "PSX";

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

export interface Company {
  ticker: string;
  psxSymbol: string; // symbol used on PSX endpoints
  name: string;
  logoInitials: string;
  logoColor: string;
  exchange: Exchange;
  country: "Pakistan";
  sector: Sector;
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
