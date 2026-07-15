// Predict investment-report ENGINE — public type surface.
//
// PSX-ONLY, HONEST-AND-THIN. Every rendered numeric figure is a `SourcedFigure`.
// The `value` variant is structurally impossible to construct without a `source`
// and an `asOf` date (NFR-1 invariant) — see realDataGuard.ts. Sections with no
// real PSX source render explicit `na`, never invented numbers (Constitution
// Article 1: no invented data).

export type Duration = "1M" | "1Y" | "3Y" | "5Y" | "20Y";

// "fact"    = value copied straight from the PSX source (price, market cap, EPS).
// "derived" = value computed from facts (returns, yields, ratios, medians).
export type Provenance = "fact" | "derived";

// The ONLY shape a rendered number may take. A `value` carries mandatory
// provenance (source + asOf); the alternative is an honest `na` with a reason.
export type SourcedFigure =
  | {
      kind: "value";
      value: number;
      unit?: string;
      source: string;
      asOf: string;
      provenance: Provenance;
      derivedFrom?: string[];
      formula?: string;
    }
  | { kind: "na"; reason: string };

// 17 report sections, addressed by stable numeric id.
export type SectionId =
  | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9
  | 10 | 11 | 12 | 13 | 14 | 15 | 16 | 17;

export type SectionStatus = "full" | "partial" | "na";

export interface SectionFigure {
  label: string;
  figure: SourcedFigure;
}

export interface SectionResult {
  id: SectionId;
  title: string;
  status: SectionStatus;
  figures: SectionFigure[];
  note?: string;
}

export interface CategoryScore {
  key: string;
  label: string;
  score10: number | null; // 0..10, or null when the category has no PSX source
  naReason?: string;
  weight: number; // base (pre-renormalization) weight for this duration
}

export type Verdict =
  | "Strong Buy"
  | "Buy"
  | "Hold"
  | "Sell"
  | "Strong Sell"
  | "Not Rated";

export interface Scorecard {
  total100: number;
  verdict: Verdict;
  categories: CategoryScore[];
  coverage: number; // 0..1, weight-weighted share of scorable categories
  lowConfidence: boolean; // coverage < 0.5
}

export interface NaSection {
  id: SectionId;
  title: string;
  reason: string;
}

export interface PredictReport {
  ticker: string;
  companyName: string;
  duration: Duration;
  generatedAt: string; // ISO — report render time (meta, NOT a data date)
  registryVersion: string;
  policyVersion: string;
  sections: SectionResult[];
  scorecard: Scorecard;
  coverage: number; // mirror of scorecard.coverage for top-level convenience
  naSections: NaSection[];
}
