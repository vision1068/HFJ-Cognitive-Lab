export type Currency = "PKR" | "USD" | "GBP" | "EUR" | "JPY";

export type MarketRegion = "Pakistan" | "United States" | "United Kingdom" | "Japan" | "Europe" | "Global";

export type Exchange = "PSX" | "NYSE" | "NASDAQ" | "LSE" | "TSE" | "XETRA" | "EURONEXT" | "SIX";

export type Sector =
  | "Banking"
  | "Fertilizer"
  | "Cement"
  | "Oil & Gas"
  | "Power"
  | "Technology"
  | "Textile"
  | "Pharmaceuticals"
  | "Insurance"
  | "Consumer Goods"
  | "Automotive"
  | "E-Commerce"
  | "Semiconductors"
  | "Telecommunications"
  | "Industrials";

export type SignalLevel = "Strong Positive" | "Positive" | "Neutral" | "Caution" | "High Risk";

export type InvestorProfile =
  | "Long-term investor"
  | "Dividend investor"
  | "Growth investor"
  | "Value investor"
  | "Short-term trader"
  | "High-risk investor";

export type SuggestedAction =
  | "Research Further"
  | "Add to Watchlist"
  | "Consider Gradual Investment"
  | "Wait for Better Entry Price"
  | "Avoid Until Fundamentals Improve";

export interface PricePoint {
  date: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface FinancialPeriod {
  period: string; // e.g. "FY2023", "Q3 2024"
  revenue: number;
  netProfit: number;
  operatingProfit: number;
  eps: number;
  freeCashFlow: number;
  debt: number;
  cash: number;
  netMargin: number;
  roe: number;
  roa: number;
}

export interface DividendRecord {
  date: string;
  amountPerShare: number;
  yieldAtDate: number;
}

export interface NewsItem {
  id: string;
  ticker: string;
  headline: string;
  source: string;
  publishedAt: string;
  summary: string;
  sentiment: "Positive" | "Neutral" | "Negative";
  impact: "Low" | "Medium" | "High";
}

export interface ScoreBreakdown {
  fundamentals: { score: number; weight: number; reason: string };
  growth: { score: number; weight: number; reason: string };
  valuation: { score: number; weight: number; reason: string };
  technical: { score: number; weight: number; reason: string };
  sentiment: { score: number; weight: number; reason: string };
  risk: { score: number; weight: number; reason: string };
  overall: number;
  signal: SignalLevel;
}

export interface HealthScore {
  overall: number;
  revenueGrowth: number;
  profitGrowth: number;
  earningsQuality: number;
  cashFlow: number;
  debtLiquidity: number;
  valuation: number;
  dividendSustainability: number;
  technicalMomentum: number;
  marketSentiment: number;
  riskScore: number;
}

export interface RiskFactor {
  label: string;
  level: "Low" | "Medium" | "High";
  description: string;
}

export interface AIResearchSummary {
  overallSignal: SignalLevel;
  thesis: string;
  positiveSignals: string[];
  warningSigns: string[];
  investorProfiles: InvestorProfile[];
  suggestedAction: SuggestedAction;
}

export interface Company {
  ticker: string;
  name: string;
  logoInitials: string;
  logoColor: string;
  exchange: Exchange;
  country: string;
  region: MarketRegion;
  sector: Sector;
  industry: string;
  currency: Currency;
  description: string;
  website: string;
  ceo: string;

  price: number;
  changePercent: number;
  changeAbs: number;
  marketCap: number; // in currency units
  week52High: number;
  week52Low: number;
  dividendYield: number;
  peRatio: number;
  pbRatio: number;
  pegRatio: number;
  evEbitda: number;
  eps: number;
  beta: number;
  analystTarget: number | null;
  esgScore: number | null;

  sparkline: number[];
  priceHistory: PricePoint[];
  financialsAnnual: FinancialPeriod[];
  financialsQuarterly: FinancialPeriod[];
  dividends: DividendRecord[];
  news: NewsItem[];

  health: HealthScore;
  scoreBreakdown: ScoreBreakdown;
  riskFactors: RiskFactor[];
  aiSummary: AIResearchSummary;

  fairValueLow: number;
  fairValueHigh: number;
  valuationVerdict: "Undervalued" | "Fairly Valued" | "Overvalued";
  sectorAvgPE: number;
  growthRating: "Very High Growth Potential" | "High Growth Potential" | "Moderate Growth Potential" | "Low Growth Potential" | "Weak Growth Outlook";
  growthDrivers: string[];

  nextEarningsDate: string;
  lastUpdated: string;
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

export interface MarketIndex {
  name: string;
  region: MarketRegion;
  value: number;
  changePercent: number;
  changeAbs: number;
}

export interface EconomicEvent {
  date: string;
  title: string;
  region: MarketRegion;
  impact: "Low" | "Medium" | "High";
  description: string;
}

export interface PriceAlert {
  id: string;
  ticker: string;
  type: "above" | "below";
  targetPrice: number;
  active: boolean;
  createdAt: string;
}
