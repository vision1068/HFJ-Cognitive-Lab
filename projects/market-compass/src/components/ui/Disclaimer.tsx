import { ShieldAlert } from "lucide-react";
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
