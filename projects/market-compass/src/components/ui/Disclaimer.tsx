import { ShieldAlert, Ban } from "lucide-react";
import { clsx } from "clsx";

export function Disclaimer({ compact = false, className }: { compact?: boolean; className?: string }) {
  if (compact) {
    return (
      <p className={clsx("text-xs text-text-secondary flex items-start gap-1.5", className)}>
        <ShieldAlert className="h-3.5 w-3.5 shrink-0 mt-0.5 text-warning" />
        Market Compass provides market research and educational insights only. Not financial advice.
      </p>
    );
  }
  return (
    <div className={clsx("flex items-start gap-3 rounded-xl border border-warning/20 bg-warning-bg px-4 py-3", className)}>
      <ShieldAlert className="h-5 w-5 shrink-0 text-warning mt-0.5" />
      <p className="text-sm text-text-secondary">
        <span className="font-medium text-text-primary">Market Compass provides market research and educational insights only. It is not financial advice.</span>{" "}
        Always conduct your own research or consult a licensed financial adviser before investing.
      </p>
    </div>
  );
}

// Prominent NOT-FINANCIAL-ADVICE disclaimer for the Predict report. Rendered
// directly above the verdict/scorecard so a reader never sees the /100 score or
// the Buy/Sell label without this framing first. Deliberately stronger than the
// generic Disclaimer above.
export function AdviceDisclaimer({ className }: { className?: string }) {
  return (
    <div
      role="note"
      aria-label="Not financial advice"
      className={clsx(
        "flex items-start gap-3 rounded-xl border-2 border-warning/40 bg-warning-bg px-4 py-3.5",
        className
      )}
    >
      <Ban className="h-5 w-5 shrink-0 text-warning mt-0.5" />
      <div className="text-sm text-text-secondary">
        <p className="font-semibold text-text-primary">
          This is not investment advice.
        </p>
        <p className="mt-1">
          The score and verdict below are an automated, mechanical summary — not a
          recommendation, not a solicitation, and not personalized to your
          circumstances. Figures are limited to what the PSX Data Portal publishes;
          anything it does not publish is shown as unavailable, never estimated.
          Do your own research or consult a licensed financial adviser before
          investing.
        </p>
      </div>
    </div>
  );
}
