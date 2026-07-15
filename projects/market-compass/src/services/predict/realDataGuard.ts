// realDataGuard — the Article-1 backstop (no invented data).
//
// `sourced()` and `na()` are the ONLY sanctioned ways to construct a figure.
// `guard()` is the final gate every figure passes through before it is rendered:
// it demotes any "value" that is not backed by a finite number, a non-empty
// source, and a valid ISO as-of date down to an honest `na`. An empty-string or
// whitespace source/asOf is treated as UNSOURCED and coerced to `na`.

import type { SourcedFigure, Provenance } from "./types";

export interface SourceOpts {
  unit?: string;
  source: string;
  asOf: string;
  provenance: Provenance;
  derivedFrom?: string[];
  formula?: string;
}

/**
 * Construct a sourced numeric figure. Even though `source`/`asOf` are required
 * at the type level, callers can still pass empty strings at runtime — so the
 * result is routed through `guard`, which downgrades anything unsourced to `na`.
 */
export function sourced(value: number, opts: SourceOpts): SourcedFigure {
  return guard({
    kind: "value",
    value,
    unit: opts.unit,
    source: opts.source,
    asOf: opts.asOf,
    provenance: opts.provenance,
    derivedFrom: opts.derivedFrom,
    formula: opts.formula,
  });
}

/** The honest "no PSX source for this" figure. */
export function na(reason: string): SourcedFigure {
  return { kind: "na", reason: reason && reason.trim() ? reason : "Data not available" };
}

function isNonEmptyString(s: unknown): s is string {
  return typeof s === "string" && s.trim().length > 0;
}

function isValidIsoDate(s: unknown): boolean {
  if (!isNonEmptyString(s)) return false;
  const t = Date.parse(s);
  return Number.isFinite(t);
}

/**
 * Coerce a figure to `na` unless it is a fully-sourced finite value.
 * Idempotent: `na` figures pass straight through.
 */
export function guard(fig: SourcedFigure): SourcedFigure {
  if (fig.kind === "na") return fig;
  if (typeof fig.value !== "number" || !Number.isFinite(fig.value)) {
    return na("Value is not a finite number");
  }
  if (!isNonEmptyString(fig.source)) {
    return na("Missing data source");
  }
  if (!isValidIsoDate(fig.asOf)) {
    return na("Missing or invalid as-of date");
  }
  return fig;
}

export type ValueFigure = Extract<SourcedFigure, { kind: "value" }>;
export type NaFigure = Extract<SourcedFigure, { kind: "na" }>;

export function isValue(fig: SourcedFigure): fig is ValueFigure {
  return fig.kind === "value";
}

export function isNa(fig: SourcedFigure): fig is NaFigure {
  return fig.kind === "na";
}
