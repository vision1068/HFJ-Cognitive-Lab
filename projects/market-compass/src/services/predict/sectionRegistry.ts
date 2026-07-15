// sectionRegistry — versioned catalogue of the 17 report-section builders.
//
// Each builder maps the PSX context to a SectionResult whose every numeric
// figure is constructed via `sourced`/`na` (so it carries source + asOf) or is
// an explicit `na`. PSX has no financial statements, dividends, governance,
// analyst or forecast data, so those sections are honest full-N/A. Scenario /
// forward-projection figures are ALWAYS na for PSX.

import type {
  SectionId,
  SectionResult,
  SectionFigure,
  SectionStatus,
  SourcedFigure,
} from "./types";
import { sourced, na, isValue } from "./realDataGuard";
import type { PredictContext } from "./dataBinding";
import { lookbackDays, smaWindow } from "./durationPolicy";
import {
  annualizedVolPct,
  maxDrawdownPct,
  periodReturnPct,
  pricePosition52wk,
  sma,
} from "./priceMath";

export const REGISTRY_VERSION = "1.0.0";

export type Builder = (ctx: PredictContext) => SectionResult;

const VALUATION_SMA_WINDOW = 200; // fixed long-run SMA for the valuation view

// A fixed 1-month lookback for the "1-month return" figure, independent of the
// report horizon.
const ONE_MONTH_DAYS = 21;

// ---- figure helpers ---------------------------------------------------------

/** Price-derived figure: dated by the true EOD date, sourced "PSX Data Portal". */
function priceFig(
  ctx: PredictContext,
  value: number | null,
  unit: string,
  provenance: "fact" | "derived",
  extra?: { derivedFrom?: string[]; formula?: string },
): SourcedFigure {
  if (value == null || !Number.isFinite(value) || ctx.priceAsOf == null) {
    return na("No PSX price data available");
  }
  return sourced(value, {
    unit,
    source: ctx.priceSource,
    asOf: ctx.priceAsOf,
    provenance,
    derivedFrom: extra?.derivedFrom,
    formula: extra?.formula,
  });
}

/** Snapshot-fundamental figure: dated by lastUpdated, sourced "(snapshot)". */
function snapFig(
  ctx: PredictContext,
  value: number | null,
  unit: string,
  provenance: "fact" | "derived",
  extra?: { derivedFrom?: string[]; formula?: string },
): SourcedFigure {
  if (value == null || !Number.isFinite(value)) {
    return na("No PSX snapshot fundamental available");
  }
  return sourced(value, {
    unit,
    source: ctx.snapshotSource,
    asOf: ctx.snapshotAsOf,
    provenance,
    derivedFrom: extra?.derivedFrom,
    formula: extra?.formula,
  });
}

/**
 * PSX price history rarely spans a 20Y (or even 5Y) nominal lookback. Horizon
 * figures still compute over whatever history exists (see periodReturnPct /
 * maxDrawdownPct clamping), but the label must say so — otherwise a ~1-year
 * price series silently masquerades as a 20-year one.
 */
function horizonCoverageNote(ctx: PredictContext, nominalLookbackDays: number): string | undefined {
  const availableDays = Math.max(0, ctx.company.priceHistory.length - 1);
  if (availableDays >= nominalLookbackDays) return undefined;
  return (
    `${ctx.duration} horizon figures target a ${nominalLookbackDays}-trading-day lookback, ` +
    `but only ${availableDays} day(s) of PSX price history are available — these figures use ` +
    `the full available history, not the full ${ctx.duration} window.`
  );
}

function appendNote(base: string | undefined, extra: string | undefined): string | undefined {
  if (!extra) return base;
  return base ? `${base} ${extra}` : extra;
}

function deriveStatus(figures: SectionFigure[]): SectionStatus {
  if (figures.length === 0) return "na";
  const values = figures.filter((f) => isValue(f.figure)).length;
  if (values === 0) return "na";
  if (values === figures.length) return "full";
  return "partial";
}

function section(
  id: SectionId,
  title: string,
  figures: SectionFigure[],
  note?: string,
): SectionResult {
  return { id, title, status: deriveStatus(figures), figures, note };
}

/** Pure full-N/A section — no PSX source exists for it. */
function naSection(id: SectionId, title: string, reason: string): SectionResult {
  return {
    id,
    title,
    status: "na",
    figures: [{ label: title, figure: na(reason) }],
    note: reason,
  };
}

function earningsYieldPct(ctx: PredictContext): SourcedFigure {
  const c = ctx.company;
  if (c.eps != null && c.eps > 0 && c.price > 0) {
    return snapFig(ctx, (c.eps / c.price) * 100, "%", "derived", {
      derivedFrom: ["EPS", "Price"],
      formula: "eps / price * 100",
    });
  }
  if (c.peRatio != null && c.peRatio > 0) {
    return snapFig(ctx, (1 / c.peRatio) * 100, "%", "derived", {
      derivedFrom: ["P/E"],
      formula: "1 / peRatio * 100",
    });
  }
  return na("No PSX EPS or P/E to derive earnings yield");
}

function pricePositionFig(ctx: PredictContext): SourcedFigure {
  const c = ctx.company;
  const pos = pricePosition52wk(c.price, c.week52Low, c.week52High);
  if (pos == null) return na("No PSX 52-week range available");
  return priceFig(ctx, pos * 100, "%", "derived", {
    derivedFrom: ["Price", "52-Week High", "52-Week Low"],
    formula: "(price - low) / (high - low) * 100",
  });
}

// ---- the 17 builders --------------------------------------------------------

const build1: Builder = (ctx) => {
  const c = ctx.company;
  const figures: SectionFigure[] = [
    { label: "Last Price", figure: priceFig(ctx, c.price, c.currency, "fact") },
    { label: "Previous Close", figure: priceFig(ctx, c.previousClose, c.currency, "fact") },
    {
      label: "Day Change",
      figure: priceFig(ctx, c.changePercent, "%", "derived", {
        derivedFrom: ["Price", "Previous Close"],
        formula: "(price - previousClose) / previousClose * 100",
      }),
    },
    { label: "Day Open", figure: priceFig(ctx, c.dayOpen, c.currency, "fact") },
    { label: "Day High", figure: priceFig(ctx, c.dayHigh, c.currency, "fact") },
    { label: "Day Low", figure: priceFig(ctx, c.dayLow, c.currency, "fact") },
    { label: "Volume", figure: priceFig(ctx, c.volume, "shares", "fact") },
  ];
  const note =
    `${c.name} — ${c.sector}${c.industry ? " / " + c.industry : ""}. ` +
    `Listed on ${c.exchange} (${c.country}).` +
    (c.ceo ? ` CEO: ${c.ceo}.` : "") +
    (c.website ? ` ${c.website}` : "");
  return section(1, "Company Overview", figures, note);
};

const build2: Builder = (ctx) => {
  const c = ctx.company;
  const figures: SectionFigure[] = [
    { label: "Market Cap", figure: snapFig(ctx, c.marketCap, c.currency, "fact") },
    { label: "Shares Outstanding", figure: snapFig(ctx, c.sharesOutstanding, "shares", "fact") },
    { label: "52-Week High", figure: priceFig(ctx, c.week52High, c.currency, "fact") },
    { label: "52-Week Low", figure: priceFig(ctx, c.week52Low, c.currency, "fact") },
  ];
  return section(2, "Market Cap & Share Information", figures);
};

const build3: Builder = () =>
  naSection(3, "Income Statement", "No PSX source publishes financial statements");

const build4: Builder = () =>
  naSection(4, "Balance Sheet", "No PSX source publishes balance-sheet data");

const build5: Builder = () =>
  naSection(5, "Cash Flow Statement", "No PSX source publishes cash-flow statements");

const build6: Builder = () =>
  naSection(6, "Dividend History", "No PSX source publishes dividend history");

const build7: Builder = (ctx) => {
  const c = ctx.company;
  const figures: SectionFigure[] = [
    { label: "EPS (trailing)", figure: snapFig(ctx, c.eps, c.currency, "fact") },
    { label: "Earnings Yield", figure: earningsYieldPct(ctx) },
    { label: "Return on Equity", figure: na("No PSX source publishes ROE") },
    { label: "Net Margin", figure: na("No PSX source publishes income statement") },
  ];
  return section(7, "Profitability", figures);
};

const build8: Builder = (ctx) => {
  const c = ctx.company;
  const smaV = sma(c.priceHistory, VALUATION_SMA_WINDOW);
  const smaPremium =
    smaV != null && smaV > 0 && c.price > 0 ? ((c.price - smaV) / smaV) * 100 : null;
  const figures: SectionFigure[] = [
    { label: "Trailing P/E", figure: snapFig(ctx, c.peRatio, "x", "fact") },
    { label: "Earnings Yield", figure: earningsYieldPct(ctx) },
    { label: "Price vs 52-Week Range", figure: pricePositionFig(ctx) },
    {
      label: "Price vs 200-Day SMA",
      figure: priceFig(ctx, smaPremium, "%", "derived", {
        derivedFrom: ["Price", "200-Day SMA"],
        formula: "(price - sma200) / sma200 * 100",
      }),
    },
    { label: "Bear Case Target", figure: na("No PSX source for forward projections") },
    { label: "Base Case Target", figure: na("No PSX source for forward projections") },
    { label: "Bull Case Target", figure: na("No PSX source for forward projections") },
  ];
  return section(8, "Valuation", figures);
};

const build9: Builder = () =>
  naSection(9, "Ownership & Governance", "No PSX source publishes ownership or governance data");

const build10: Builder = () =>
  naSection(10, "Analyst Ratings & Price Targets", "No PSX source publishes analyst ratings or price targets");

const build11: Builder = () =>
  naSection(11, "ESG & Sustainability", "No PSX source publishes ESG data");

const build12: Builder = (ctx) => {
  const c = ctx.company;
  const peerTickers = ctx.peers.map((p) => p.ticker);
  const peSnap = ctx.peerMedianPE;
  const mcSnap = ctx.peerMedianMarketCap;

  const peVsPeer =
    c.peRatio != null && c.peRatio > 0 && peSnap != null && peSnap > 0
      ? (c.peRatio / peSnap) * 100
      : null;
  const mcVsPeer =
    c.marketCap != null && c.marketCap > 0 && mcSnap != null && mcSnap > 0
      ? (c.marketCap / mcSnap) * 100
      : null;

  const figures: SectionFigure[] = [
    {
      label: "Peer Median P/E",
      figure: snapFig(ctx, peSnap, "x", "derived", { derivedFrom: peerTickers, formula: "median(peer P/E)" }),
    },
    {
      label: "P/E vs Peer Median",
      figure: snapFig(ctx, peVsPeer, "%", "derived", {
        derivedFrom: ["P/E", "Peer Median P/E"],
        formula: "peRatio / peerMedianPE * 100",
      }),
    },
    {
      label: "Peer Median Market Cap",
      figure: snapFig(ctx, mcSnap, c.currency, "derived", { derivedFrom: peerTickers, formula: "median(peer marketCap)" }),
    },
    {
      label: "Market Cap vs Peer Median",
      figure: snapFig(ctx, mcVsPeer, "%", "derived", {
        derivedFrom: ["Market Cap", "Peer Median Market Cap"],
        formula: "marketCap / peerMedianMarketCap * 100",
      }),
    },
  ];
  const note =
    peerTickers.length > 0
      ? `Compared against ${peerTickers.length} same-sector PSX peer(s): ${peerTickers.join(", ")}.`
      : "No same-sector PSX peers available for comparison.";
  return section(12, "Competitive Comparison", figures, note);
};

const build13: Builder = (ctx) => {
  const c = ctx.company;
  const lb = lookbackDays(ctx.duration);

  const belowHigh =
    c.week52High != null && c.week52High > 0 ? ((c.price - c.week52High) / c.week52High) * 100 : null;
  const aboveLow =
    c.week52Low != null && c.week52Low > 0 ? ((c.price - c.week52Low) / c.week52Low) * 100 : null;

  const figures: SectionFigure[] = [
    {
      label: "Distance Below 52-Week High",
      figure: priceFig(ctx, belowHigh, "%", "derived", {
        derivedFrom: ["Price", "52-Week High"],
        formula: "(price - high) / high * 100",
      }),
    },
    {
      label: "Distance Above 52-Week Low",
      figure: priceFig(ctx, aboveLow, "%", "derived", {
        derivedFrom: ["Price", "52-Week Low"],
        formula: "(price - low) / low * 100",
      }),
    },
    {
      label: "Max Drawdown (horizon)",
      figure: priceFig(ctx, maxDrawdownPct(c.priceHistory, lb), "%", "derived"),
    },
    {
      label: "Annualized Volatility",
      figure: priceFig(ctx, annualizedVolPct(c.priceHistory, lb), "%", "derived"),
    },
    {
      label: "Statement-Based Red Flags",
      figure: na("No PSX source publishes financial statements"),
    },
  ];
  const note = appendNote(
    "Price-based signals only; statement-based flags require data PSX does not publish.",
    horizonCoverageNote(ctx, lb),
  );
  return section(13, "Red-Flag Detection", figures, note);
};

const build14: Builder = (ctx) => {
  const c = ctx.company;
  const lb = lookbackDays(ctx.duration);
  const peerMed = ctx.peerMedianPE;
  const discountToPeer =
    c.peRatio != null && c.peRatio > 0 && peerMed != null && peerMed > 0
      ? ((peerMed - c.peRatio) / peerMed) * 100
      : null;

  const figures: SectionFigure[] = [
    { label: "Proximity to 52-Week High", figure: pricePositionFig(ctx) },
    {
      label: "Discount to Peer Median P/E",
      figure: snapFig(ctx, discountToPeer, "%", "derived", {
        derivedFrom: ["P/E", "Peer Median P/E"],
        formula: "(peerMedianPE - peRatio) / peerMedianPE * 100",
      }),
    },
    {
      label: "Momentum Return (horizon)",
      figure: priceFig(ctx, periodReturnPct(c.priceHistory, lb), "%", "derived"),
    },
  ];
  return section(14, "Positive Factors", figures, horizonCoverageNote(ctx, lb));
};

const build15: Builder = (ctx) => {
  const c = ctx.company;
  const lb = lookbackDays(ctx.duration);
  const sw = smaWindow(ctx.duration);
  const smaV = sma(c.priceHistory, sw);
  const smaPremium =
    smaV != null && smaV > 0 && c.price > 0 ? ((c.price - smaV) / smaV) * 100 : null;

  const figures: SectionFigure[] = [
    {
      label: "Return (selected horizon)",
      figure: priceFig(ctx, periodReturnPct(c.priceHistory, lb), "%", "derived"),
    },
    {
      label: "Return (1-Month)",
      figure: priceFig(ctx, periodReturnPct(c.priceHistory, ONE_MONTH_DAYS), "%", "derived"),
    },
    {
      label: `Price vs ${sw}-Day SMA`,
      figure: priceFig(ctx, smaPremium, "%", "derived", {
        derivedFrom: ["Price", `${sw}-Day SMA`],
        formula: "(price - sma) / sma * 100",
      }),
    },
  ];
  return section(15, "Price Performance & Momentum", figures, horizonCoverageNote(ctx, lb));
};

const build16: Builder = (ctx) => {
  const c = ctx.company;
  const lb = lookbackDays(ctx.duration);
  const figures: SectionFigure[] = [
    {
      label: "Annualized Volatility",
      figure: priceFig(ctx, annualizedVolPct(c.priceHistory, lb), "%", "derived"),
    },
    {
      label: "Max Drawdown (horizon)",
      figure: priceFig(ctx, maxDrawdownPct(c.priceHistory, lb), "%", "derived"),
    },
  ];
  return section(16, "Volatility & Drawdown", figures, horizonCoverageNote(ctx, lb));
};

const build17: Builder = () =>
  naSection(17, "Forward Guidance & Scenarios", "No PSX source for forward projections");

export const SECTION_BUILDERS: Record<SectionId, Builder> = {
  1: build1,
  2: build2,
  3: build3,
  4: build4,
  5: build5,
  6: build6,
  7: build7,
  8: build8,
  9: build9,
  10: build10,
  11: build11,
  12: build12,
  13: build13,
  14: build14,
  15: build15,
  16: build16,
  17: build17,
};
