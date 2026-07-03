import { AlertCircle, Loader2, Info } from "lucide-react";
import { clsx } from "clsx";

export function LoadingState({ label = "Loading live PSX data…", className }: { label?: string; className?: string }) {
  return (
    <div className={clsx("flex flex-col items-center justify-center gap-3 py-16 text-text-secondary", className)}>
      <Loader2 className="h-6 w-6 animate-spin text-brand-400" />
      <p className="text-sm">{label}</p>
    </div>
  );
}

export function ErrorState({
  error,
  onRetry,
  className,
}: {
  error?: unknown;
  onRetry?: () => void;
  className?: string;
}) {
  const message = error instanceof Error ? error.message : "The live data source could not be reached.";
  return (
    <div className={clsx("card p-6 flex flex-col items-center text-center gap-3", className)}>
      <AlertCircle className="h-6 w-6 text-negative" />
      <div>
        <p className="text-sm font-medium text-text-primary">Couldn't load live market data</p>
        <p className="text-xs text-text-secondary mt-1 max-w-md">{message}</p>
      </div>
      {onRetry && (
        <button onClick={onRetry} className="text-xs font-medium bg-brand-500 hover:bg-brand-600 text-white px-3 py-1.5 rounded-lg transition-colors">
          Retry
        </button>
      )}
    </div>
  );
}

// Explicit "not available on free data sources" state. Used wherever a feature
// relies on data no free PSX API provides (financials, scores, news, etc.) —
// never replaced with fabricated numbers.
export function DataUnavailable({
  title = "Data not available",
  reason,
  className,
  compact = false,
}: {
  title?: string;
  reason?: string;
  className?: string;
  compact?: boolean;
}) {
  if (compact) {
    return (
      <span className={clsx("inline-flex items-center gap-1 text-xs text-text-secondary", className)}>
        <Info className="h-3 w-3 shrink-0" /> {title}
      </span>
    );
  }
  return (
    <div className={clsx("flex items-start gap-2.5 rounded-xl border border-border-subtle bg-bg-elevated/50 px-4 py-3", className)}>
      <Info className="h-4 w-4 shrink-0 text-text-secondary mt-0.5" />
      <div>
        <p className="text-sm font-medium text-text-primary">{title}</p>
        {reason && <p className="text-xs text-text-secondary mt-0.5">{reason}</p>}
      </div>
    </div>
  );
}

// Renders a numeric value or an explicit "N/A" when the source didn't provide it.
export function orNA(value: number | null | undefined, fmt: (n: number) => string): string {
  return value == null ? "N/A" : fmt(value);
}
