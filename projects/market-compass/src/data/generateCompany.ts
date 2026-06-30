import type {
  Company,
  FinancialPeriod,
  HealthScore,
  NewsItem,
  PricePoint,
  RiskFactor,
  ScoreBreakdown,
  SignalLevel,
  AIResearchSummary,
  InvestorProfile,
  SuggestedAction,
} from "@/types";
import type { SeedCompany } from "./seedCompanies";
import { mulberry32, randInt, randRange, seedFromString, pick } from "./random";

const BIAS_RANGE: Record<SeedCompany["bias"], [number, number]> = {
  strong: [78, 95],
  positive: [64, 82],
  neutral: [48, 66],
  caution: [34, 52],
  risk: [15, 36],
};

function clamp(n: number, min = 0, max = 100) {
  return Math.max(min, Math.min(max, n));
}

function signalFromScore(score: number): SignalLevel {
  if (score >= 80) return "Strong Positive";
  if (score >= 65) return "Positive";
  if (score >= 50) return "Neutral";
  if (score >= 35) return "Caution";
  return "High Risk";
}

function generatePriceHistory(rng: () => number, basePrice: number, days: number, trendBias: number): PricePoint[] {
  // Walk backwards from today's exact price so the most recent close always equals basePrice,
  // avoiding an artificial jump on the last day from forcing a randomly-drifted series to match.
  const closes: number[] = new Array(days + 1);
  closes[days] = basePrice;
  let price = basePrice;
  for (let i = days - 1; i >= 0; i--) {
    const drift = trendBias * 0.0009 + randRange(rng, -0.018, 0.018);
    price = Math.max(price / (1 + drift), 0.5);
    closes[i] = price;
  }

  const points: PricePoint[] = [];
  const now = new Date();
  for (let i = 0; i <= days; i++) {
    const date = new Date(now);
    date.setDate(date.getDate() - (days - i));
    const close = closes[i];
    const open = close * randRange(rng, 0.99, 1.01);
    const high = Math.max(open, close) * randRange(rng, 1.0, 1.02);
    const low = Math.min(open, close) * randRange(rng, 0.98, 1.0);
    const volume = Math.floor(randRange(rng, 200_000, 8_000_000));
    points.push({
      date: date.toISOString().slice(0, 10),
      open: Number(open.toFixed(2)),
      high: Number(high.toFixed(2)),
      low: Number(low.toFixed(2)),
      close: Number(close.toFixed(2)),
      volume,
    });
  }
  return points;
}

function generateFinancials(rng: () => number, baseRevenue: number, periods: number, label: "FY" | "Q", growthBias: number, sharesOutstanding: number): FinancialPeriod[] {
  const out: FinancialPeriod[] = [];
  let revenue = baseRevenue * (1 - growthBias * 0.04 * periods * 0.5);
  const currentYear = new Date().getFullYear();
  for (let i = periods - 1; i >= 0; i--) {
    const growth = growthBias * randRange(rng, 0.02, 0.07) + randRange(rng, -0.015, 0.015);
    revenue = revenue * (1 + growth);
    const margin = clamp(randRange(rng, 8, 28) + growthBias * 4, 4, 38) / 100;
    const netProfit = revenue * margin;
    const operatingProfit = netProfit * randRange(rng, 1.15, 1.4);
    const eps = Number((netProfit / sharesOutstanding).toFixed(2));
    const freeCashFlow = netProfit * randRange(rng, 0.65, 1.25);
    const debt = revenue * randRange(rng, 0.15, 0.65) * (1 - growthBias * 0.1);
    const cash = revenue * randRange(rng, 0.08, 0.3);
    const roe = clamp(randRange(rng, 6, 28) + growthBias * 3, 2, 45);
    const roa = clamp(roe * randRange(rng, 0.35, 0.55), 1, 25);
    out.push({
      period: label === "FY" ? `FY${currentYear - i}` : `Q${4 - (i % 4)} ${currentYear - Math.floor(i / 4)}`,
      revenue: Math.round(revenue),
      netProfit: Math.round(netProfit),
      operatingProfit: Math.round(operatingProfit),
      eps,
      freeCashFlow: Math.round(freeCashFlow),
      debt: Math.round(debt),
      cash: Math.round(cash),
      netMargin: Number((margin * 100).toFixed(1)),
      roe: Number(roe.toFixed(1)),
      roa: Number(roa.toFixed(1)),
    });
  }
  return out;
}

const NEWS_TEMPLATES = {
  Positive: [
    (n: string) => `${n} reports stronger-than-expected quarterly profit growth`,
    (n: string) => `${n} announces expansion into new markets`,
    (n: string) => `Analysts upgrade ${n} on improving margins`,
    (n: string) => `${n} declares higher dividend payout`,
    (n: string) => `${n} secures major new contract`,
  ],
  Neutral: [
    (n: string) => `${n} holds investor briefing on quarterly performance`,
    (n: string) => `${n} announces board changes`,
    (n: string) => `${n} in line with sector performance this quarter`,
    (n: string) => `${n} maintains guidance for the fiscal year`,
  ],
  Negative: [
    (n: string) => `${n} flags rising input costs pressuring margins`,
    (n: string) => `${n} shares slip on weaker demand outlook`,
    (n: string) => `Analysts flag valuation concerns for ${n}`,
    (n: string) => `${n} faces regulatory scrutiny in key market`,
  ],
};

function generateNews(rng: () => number, ticker: string, name: string, sentimentBias: number): NewsItem[] {
  const count = randInt(rng, 4, 7);
  const news: NewsItem[] = [];
  const sources = ["Bloomberg", "Reuters", "CNBC", "Dawn Business", "Business Recorder", "Financial Times", "MarketWatch", "PSX Newswire"];
  for (let i = 0; i < count; i++) {
    const roll = rng() * 100 + sentimentBias * 20;
    const sentiment: NewsItem["sentiment"] = roll > 60 ? "Positive" : roll > 35 ? "Neutral" : "Negative";
    const template = pick(rng, NEWS_TEMPLATES[sentiment]);
    const daysAgo = i * randInt(rng, 1, 3) + randInt(rng, 0, 2);
    const date = new Date();
    date.setDate(date.getDate() - daysAgo);
    news.push({
      id: `${ticker}-news-${i}`,
      ticker,
      headline: template(name),
      source: pick(rng, sources),
      publishedAt: date.toISOString(),
      summary: `${name} continues to be closely watched by investors amid sector-wide developments. This report covers the latest update relevant to near-term performance and outlook.`,
      sentiment,
      impact: pick(rng, ["Low", "Medium", "High"] as const),
    });
  }
  return news.sort((a, b) => +new Date(b.publishedAt) - +new Date(a.publishedAt));
}

function biasScore(rng: () => number, bias: SeedCompany["bias"], jitter = 10): number {
  const [min, max] = BIAS_RANGE[bias];
  return clamp(Math.round(randRange(rng, min, max) + randRange(rng, -jitter / 2, jitter / 2)));
}

const POSITIVE_POOL = [
  "Revenue and earnings have grown consistently over the past several quarters",
  "Debt levels remain manageable relative to sector peers",
  "Operating margins are improving year over year",
  "Dividend track record signals confidence from management",
  "Recent price action shows improving technical momentum",
  "Sector tailwinds support continued demand growth",
  "News sentiment has trended positive over recent weeks",
  "Strong free cash flow generation supports reinvestment and payouts",
  "Return on equity is above the sector median",
];

const WARNING_POOL = [
  "Valuation multiples are elevated relative to historical averages",
  "Free cash flow has been inconsistent in recent periods",
  "Debt-to-equity ratio is above the sector average",
  "Operating margin has compressed in recent quarters",
  "Regulatory developments could affect near-term performance",
  "Price trend has shown weakening technical momentum",
  "Sector faces cyclical headwinds that could pressure results",
  "Recent insider activity warrants monitoring where data is available",
];

const ALL_PROFILES: InvestorProfile[] = [
  "Long-term investor",
  "Dividend investor",
  "Growth investor",
  "Value investor",
  "Short-term trader",
  "High-risk investor",
];

const ACTIONS_BY_SIGNAL: Record<SignalLevel, SuggestedAction[]> = {
  "Strong Positive": ["Add to Watchlist", "Consider Gradual Investment"],
  Positive: ["Add to Watchlist", "Research Further", "Consider Gradual Investment"],
  Neutral: ["Research Further", "Wait for Better Entry Price"],
  Caution: ["Wait for Better Entry Price", "Research Further"],
  "High Risk": ["Avoid Until Fundamentals Improve", "Wait for Better Entry Price"],
};

export function generateCompany(seed: SeedCompany): Company {
  const rng = mulberry32(seedFromString(seed.ticker));
  const growthBias = seed.bias === "strong" ? 1 : seed.bias === "positive" ? 0.6 : seed.bias === "neutral" ? 0.15 : seed.bias === "caution" ? -0.3 : -0.7;

  const priceHistoryMax = generatePriceHistory(rng, seed.basePrice, 365 * 3, growthBias);
  const priceHistory1Y = priceHistoryMax.slice(-365);
  const changeAbs = Number((priceHistory1Y[priceHistory1Y.length - 1].close - priceHistory1Y[priceHistory1Y.length - 2].close).toFixed(2));
  const changePercent = Number(((changeAbs / priceHistory1Y[priceHistory1Y.length - 2].close) * 100).toFixed(2));

  const week52Slice = priceHistory1Y.slice(-252);
  const week52High = Math.max(...week52Slice.map((p) => p.high));
  const week52Low = Math.min(...week52Slice.map((p) => p.low));

  const marketCap = Math.round(seed.basePrice * randRange(rng, 8e7, 6e9));
  const sharesOutstanding = marketCap / seed.basePrice;

  const baseRevenue = seed.basePrice * randRange(rng, 1.8e6, 6e6);
  const financialsAnnual = generateFinancials(rng, baseRevenue, 5, "FY", growthBias, sharesOutstanding);
  const financialsQuarterly = generateFinancials(rng, baseRevenue / 4, 8, "Q", growthBias, sharesOutstanding);

  const latestAnnual = financialsAnnual[financialsAnnual.length - 1];
  const prevAnnual = financialsAnnual[financialsAnnual.length - 2];
  const revenueGrowthRate = Number((((latestAnnual.revenue - prevAnnual.revenue) / prevAnnual.revenue) * 100).toFixed(1));
  const earningsGrowthRate = Number((((latestAnnual.netProfit - prevAnnual.netProfit) / prevAnnual.netProfit) * 100).toFixed(1));
  const peRatio = Number(clamp(randRange(rng, 6, 45) - growthBias * -3, 4, 60).toFixed(1));
  const sectorAvgPE = Number((peRatio * randRange(rng, 0.8, 1.25)).toFixed(1));
  const pbRatio = Number(randRange(rng, 0.8, 9).toFixed(2));
  const pegRatio = Number(clamp(peRatio / Math.max(revenueGrowthRate, 3), 0.3, 6).toFixed(2));
  const evEbitda = Number(randRange(rng, 4, 22).toFixed(1));
  const dividendYield = Number(clamp(randRange(rng, 0, 9) + growthBias, 0, 12).toFixed(2));
  const eps = latestAnnual.eps;
  const beta = Number(randRange(rng, 0.5, 1.8).toFixed(2));
  const analystTarget = rng() > 0.15 ? Number((seed.basePrice * randRange(rng, 0.92, 1.28)).toFixed(2)) : null;
  const esgScore = rng() > 0.2 ? Math.round(randRange(rng, 35, 92)) : null;

  const fairValueLow = Number((seed.basePrice * randRange(rng, 0.78, 0.98)).toFixed(2));
  const fairValueHigh = Number((seed.basePrice * randRange(rng, 1.02, 1.25)).toFixed(2));
  const valuationVerdict = seed.basePrice < fairValueLow ? "Undervalued" : seed.basePrice > fairValueHigh ? "Overvalued" : "Fairly Valued";

  const growthRatingScore = biasScore(rng, seed.bias);
  const growthRating =
    growthRatingScore >= 82 ? "Very High Growth Potential" :
    growthRatingScore >= 67 ? "High Growth Potential" :
    growthRatingScore >= 50 ? "Moderate Growth Potential" :
    growthRatingScore >= 35 ? "Low Growth Potential" : "Weak Growth Outlook";

  const growthDriverPool = [
    "Expanding addressable market and rising sector demand",
    "New product launches and innovation pipeline",
    "International / regional expansion plans",
    "Strong competitive positioning and brand strength",
    "New projects, contracts, or capacity expansion underway",
    "Consistent dividend growth track record",
    "Gaining market share from competitors",
  ];
  const growthDrivers = [...growthDriverPool].sort(() => rng() - 0.5).slice(0, randInt(rng, 3, 5));

  const health: HealthScore = {
    overall: 0,
    revenueGrowth: biasScore(rng, seed.bias),
    profitGrowth: biasScore(rng, seed.bias),
    earningsQuality: biasScore(rng, seed.bias),
    cashFlow: biasScore(rng, seed.bias),
    debtLiquidity: biasScore(rng, seed.bias),
    valuation: biasScore(rng, seed.bias === "strong" ? "neutral" : seed.bias),
    dividendSustainability: biasScore(rng, seed.bias),
    technicalMomentum: biasScore(rng, seed.bias),
    marketSentiment: biasScore(rng, seed.bias),
    riskScore: clamp(100 - biasScore(rng, seed.bias)),
  };
  health.overall = Math.round(
    (health.revenueGrowth + health.profitGrowth + health.earningsQuality + health.cashFlow + health.debtLiquidity + health.valuation + health.dividendSustainability + health.technicalMomentum + health.marketSentiment + (100 - health.riskScore)) / 10
  );

  const fundamentalsScore = biasScore(rng, seed.bias);
  const growthScore = growthRatingScore;
  const valuationScore = biasScore(rng, seed.bias === "strong" ? "neutral" : seed.bias);
  const technicalScore = biasScore(rng, seed.bias);
  const sentimentScore = biasScore(rng, seed.bias);
  const riskFactorScore = biasScore(rng, seed.bias);

  const overall = Math.round(
    fundamentalsScore * 0.3 + growthScore * 0.2 + valuationScore * 0.15 + technicalScore * 0.15 + sentimentScore * 0.1 + riskFactorScore * 0.1
  );
  const signal = signalFromScore(overall);

  const scoreBreakdown: ScoreBreakdown = {
    fundamentals: {
      score: fundamentalsScore,
      weight: 30,
      reason: `Revenue grew ${revenueGrowthRate}% and net profit grew ${earningsGrowthRate}% in the latest fiscal year, with operating margin of ${latestAnnual.netMargin}%.`,
    },
    growth: {
      score: growthScore,
      weight: 20,
      reason: `${growthRating} driven by ${growthDrivers[0]?.toLowerCase()}.`,
    },
    valuation: {
      score: valuationScore,
      weight: 15,
      reason: `Trading at ${peRatio}x earnings vs. sector average of ${sectorAvgPE}x, considered ${valuationVerdict.toLowerCase()} based on estimated fair value range.`,
    },
    technical: {
      score: technicalScore,
      weight: 15,
      reason: technicalScore > 60 ? "Price trend is above key moving averages with healthy volume support." : technicalScore > 40 ? "Price is consolidating near key moving averages." : "Price trend shows weakening momentum below key moving averages.",
    },
    sentiment: {
      score: sentimentScore,
      weight: 10,
      reason: sentimentScore > 60 ? "Recent news flow and analyst commentary skew positive." : sentimentScore > 40 ? "News sentiment is mixed with balanced positive and negative coverage." : "Recent news flow includes notable negative coverage.",
    },
    risk: {
      score: riskFactorScore,
      weight: 10,
      reason: riskFactorScore > 60 ? "Risk factors are limited; debt and volatility remain within manageable ranges." : "Elevated risk factors including debt, volatility, or sector headwinds warrant caution.",
    },
    overall,
    signal,
  };

  const riskFactors: RiskFactor[] = [
    { label: "Debt Level", level: health.debtLiquidity > 65 ? "Low" : health.debtLiquidity > 40 ? "Medium" : "High", description: "Assessment of debt-to-equity and interest coverage relative to sector peers." },
    { label: "Revenue Stability", level: health.revenueGrowth > 65 ? "Low" : health.revenueGrowth > 40 ? "Medium" : "High", description: "Consistency of revenue growth across recent reporting periods." },
    { label: "Cash Flow", level: health.cashFlow > 65 ? "Low" : health.cashFlow > 40 ? "Medium" : "High", description: "Strength and consistency of free cash flow generation." },
    { label: "Valuation Risk", level: valuationVerdict === "Overvalued" ? "High" : valuationVerdict === "Fairly Valued" ? "Medium" : "Low", description: "Risk that current price already reflects optimistic future growth." },
    { label: "Currency / Country Risk", level: seed.region === "Pakistan" ? "Medium" : "Low", description: "Exposure to currency depreciation, inflation, and local regulatory/political risk." },
    { label: "Technical Volatility", level: beta > 1.3 ? "High" : beta > 0.9 ? "Medium" : "Low", description: `Beta of ${beta} relative to the broader market index.` },
  ];

  const positiveSignals = [...POSITIVE_POOL].sort(() => rng() - 0.5).slice(0, randInt(rng, 3, 5));
  const warningSigns = [...WARNING_POOL].sort(() => rng() - 0.5).slice(0, randInt(rng, 2, 4));
  const investorProfiles = [...ALL_PROFILES].sort(() => rng() - 0.5).slice(0, randInt(rng, 2, 3));
  const suggestedAction = pick(rng, ACTIONS_BY_SIGNAL[signal]);

  const aiSummary: AIResearchSummary = {
    overallSignal: signal,
    thesis: `${seed.name} screens as a ${signal.toLowerCase()} candidate based on a blended score of ${overall}/100. ${
      growthBias > 0
        ? `The company has demonstrated consistent revenue and earnings growth, supported by ${growthDrivers[0]?.toLowerCase()}.`
        : `The company faces some near-term headwinds, with growth and margin trends that warrant closer monitoring.`
    } Valuation currently appears ${valuationVerdict.toLowerCase()} relative to estimated fair value.`,
    positiveSignals,
    warningSigns,
    investorProfiles,
    suggestedAction,
  };

  const news = generateNews(rng, seed.ticker, seed.name, growthBias);

  const dividends = financialsAnnual.map((f, idx) => ({
    date: `${f.period.replace("FY", "")}-12-15`,
    amountPerShare: Number((dividendYield > 0 ? (seed.basePrice * (dividendYield / 100)) * randRange(rng, 0.7, 1.1) : 0).toFixed(2)),
    yieldAtDate: Number(clamp(dividendYield + randRange(rng, -1, 1) * (idx + 1) * 0.2, 0, 14).toFixed(2)),
  }));

  const nextEarningsDate = (() => {
    const d = new Date();
    d.setDate(d.getDate() + randInt(rng, 5, 75));
    return d.toISOString().slice(0, 10);
  })();

  const sparkline = priceHistory1Y.slice(-30).map((p) => p.close);

  return {
    ticker: seed.ticker,
    name: seed.name,
    logoInitials: seed.name
      .split(" ")
      .filter((w) => !["of", "the", "&", "Inc.", "Inc", "Corporation", "Company", "Limited", "plc", "PLC", "S.A.", "Co."].includes(w))
      .slice(0, 2)
      .map((w) => w[0])
      .join("")
      .toUpperCase(),
    logoColor: seed.logoColor,
    exchange: seed.exchange,
    country: seed.country,
    region: seed.region,
    sector: seed.sector,
    industry: seed.industry,
    currency: seed.currency,
    description: seed.description,
    website: seed.website,
    ceo: seed.ceo,

    price: Number(seed.basePrice.toFixed(2)),
    changePercent,
    changeAbs,
    marketCap,
    week52High: Number(week52High.toFixed(2)),
    week52Low: Number(week52Low.toFixed(2)),
    dividendYield,
    peRatio,
    pbRatio,
    pegRatio,
    evEbitda,
    eps,
    beta,
    analystTarget,
    esgScore,

    sparkline,
    priceHistory: priceHistoryMax,
    financialsAnnual,
    financialsQuarterly,
    dividends,
    news,

    health,
    scoreBreakdown,
    riskFactors,
    aiSummary,

    fairValueLow,
    fairValueHigh,
    valuationVerdict,
    sectorAvgPE,
    growthRating,
    growthDrivers,

    nextEarningsDate,
    lastUpdated: new Date().toISOString(),
  };
}
