import { Info } from "lucide-react";
import { clsx } from "clsx";
import type { MarketStructure, SignalStrength } from "@/lib/indicators";
import { signalLabel, signalTone, confidenceBand, SIGNAL_ADVICE_NOTE } from "@/lib/signalDisplay";

const TONE_STYLES: Record<ReturnType<typeof signalTone>, string> = {
  positive: "bg-positive-bg text-positive border-positive/30",
  negative: "bg-negative-bg text-negative border-negative/30",
  neutral: "bg-neutral-bg text-neutral border-neutral/20",
};

/**
 * Presentational signal card (Auditor D3 fix). Renders the technical setup with
 * neutral wording, a coarse confidence band (no precise % score), and a
 * non-dismissible "not financial advice" note ON the panel itself.
 */
export function SignalPanel({
  signal,
  confidenceScore,
  marketStructure,
  signalReasons,
}: {
  signal: SignalStrength;
  confidenceScore: number;
  marketStructure: MarketStructure;
  signalReasons: string[];
}) {
  const label = signalLabel(signal);
  const tone = signalTone(signal);
  const band = confidenceBand(confidenceScore);

  return (
    <div className="lg:col-span-1 rounded-xl border border-border-subtle bg-bg-surface p-5">
      <h3 className="text-sm font-semibold text-text-primary mb-3">Technical Setup</h3>
      <div className={clsx("rounded-lg border px-4 py-3 text-center mb-3", TONE_STYLES[tone])}>
        <div className="text-lg font-bold">{label}</div>
        <div className="text-xs mt-1">Indicator agreement: {band}</div>
      </div>

      <div className="text-xs text-text-secondary mb-2">
        Market structure: <span className="text-text-primary capitalize">{marketStructure}</span>
      </div>
      <ul className="space-y-1.5 text-xs text-text-secondary list-disc list-inside">
        {signalReasons.map((r, i) => (
          <li key={i}>{r}</li>
        ))}
      </ul>

      {/* Non-dismissible advice note ON the panel (distinct from footer disclaimer). */}
      <div
        role="note"
        className="mt-4 flex items-start gap-2 rounded-lg border border-warning/20 bg-warning-bg/60 px-3 py-2"
      >
        <Info className="h-3.5 w-3.5 shrink-0 text-warning mt-0.5" />
        <p className="text-[11px] leading-snug text-text-secondary">{SIGNAL_ADVICE_NOTE}</p>
      </div>
    </div>
  );
}
