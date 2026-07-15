import { Info } from "lucide-react";
import type { SourcedFigure } from "@/services/predict/types";
import { isValue } from "@/services/predict/realDataGuard";

// Formats a plain numeric figure value. Kept local (not currency-aware) because a
// SourcedFigure carries its own optional unit — percentages, ratios, counts, PKR.
function formatValue(value: number): string {
  const abs = Math.abs(value);
  const maximumFractionDigits = abs >= 100 ? 0 : abs >= 1 ? 2 : 4;
  return value.toLocaleString(undefined, { maximumFractionDigits });
}

// Renders ONE SourcedFigure. A `value` shows the number + unit with a small
// source/as-of provenance badge (and a "derived" tag when computed); an `na`
// shows an explicit muted "Data not available — {reason}" pill. A null/0/dash is
// NEVER substituted for na — the two branches are structurally separate.
export function SourcedFigureView({ figure }: { figure: SourcedFigure }) {
  if (!isValue(figure)) {
    return (
      <span
        data-testid="na-figure"
        className="inline-flex items-start gap-1.5 rounded-md bg-bg-elevated/60 px-2 py-1 text-xs text-text-secondary"
      >
        <Info className="h-3.5 w-3.5 shrink-0 mt-px" />
        <span>
          <span className="font-medium">Data not available</span>
          {figure.reason ? ` — ${figure.reason}` : ""}
        </span>
      </span>
    );
  }

  return (
    <span className="inline-flex flex-wrap items-baseline gap-x-1.5 gap-y-1">
      <span className="text-sm font-semibold text-text-primary tabular-nums">
        {formatValue(figure.value)}
        {figure.unit ? <span className="ml-0.5 text-xs font-normal text-text-secondary">{figure.unit}</span> : null}
      </span>
      {figure.provenance === "derived" && (
        <span className="inline-flex items-center rounded-full bg-neutral-bg px-1.5 py-px text-[10px] font-medium text-neutral">
          derived
        </span>
      )}
      <span className="text-[10px] text-text-secondary">
        {figure.source} · as of {figure.asOf}
      </span>
    </span>
  );
}
