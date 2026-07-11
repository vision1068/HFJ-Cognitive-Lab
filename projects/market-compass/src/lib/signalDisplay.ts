// Display-layer mapping for technical-analysis signals (Auditor D3 fix).
//
// The indicator math in `indicators.ts` still emits imperative "Strong Buy" /
// "Buy" / "Neutral" / "Sell" / "Strong Sell" values (that type is unchanged).
// The UI, however, must NOT present imperative buy/sell recommendations — a
// keyword-driven indicator score is not investment advice. So every place the
// signal is rendered maps it here to a NEUTRAL, descriptive "technical setup"
// label, and the precise 0-100 confidence number is collapsed to a coarse
// qualitative band so it can't be mistaken for predictive rigor.

import type { SignalStrength } from "@/lib/indicators";

export type SignalTone = "positive" | "negative" | "neutral";
export type ConfidenceBand = "Low" | "Moderate" | "High";

// Non-imperative wording. No "Buy"/"Sell" words anywhere in these strings.
const SIGNAL_LABELS: Record<SignalStrength, string> = {
  "Strong Buy": "Strong Bullish setup",
  Buy: "Bullish setup",
  Neutral: "Neutral",
  Sell: "Bearish setup",
  "Strong Sell": "Strong Bearish setup",
};

const SIGNAL_TONES: Record<SignalStrength, SignalTone> = {
  "Strong Buy": "positive",
  Buy: "positive",
  Neutral: "neutral",
  Sell: "negative",
  "Strong Sell": "negative",
};

/** Neutral, descriptive label for a raw indicator signal. Never "Buy"/"Sell". */
export function signalLabel(signal: SignalStrength): string {
  return SIGNAL_LABELS[signal];
}

/** Colour tone for the setup label (bullish=positive, bearish=negative). */
export function signalTone(signal: SignalStrength): SignalTone {
  return SIGNAL_TONES[signal];
}

/**
 * Collapse the precise 0-100 confidence score into a coarse qualitative band.
 * A single number like "78/100" implies false precision; a band does not.
 */
export function confidenceBand(score: number): ConfidenceBand {
  if (score >= 70) return "High";
  if (score >= 40) return "Moderate";
  return "Low";
}

// Non-dismissible note shown ON the signal panel itself (distinct from the
// global "not financial advice" footer disclaimer).
export const SIGNAL_ADVICE_NOTE =
  "Automated technical-indicator calculation. Not financial advice, not a recommendation, not personalized.";
