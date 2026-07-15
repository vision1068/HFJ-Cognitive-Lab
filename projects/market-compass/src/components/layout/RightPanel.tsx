import { Loader2 } from "lucide-react";
import { PriceChange } from "@/components/ui/PriceChange";
import { DataUnavailable } from "@/components/ui/DataState";
import { useIndices } from "@/hooks/useMarketData";

export function RightPanel() {
  const { data: indices, isLoading: indicesLoading } = useIndices();

  return (
    <aside className="hidden xl:flex flex-col w-80 shrink-0 border-l border-border-subtle bg-bg-surface overflow-y-auto">
      <div className="p-4 border-b border-border-subtle">
        <h3 className="text-sm font-semibold text-text-primary mb-3">PSX Indices</h3>
        {indicesLoading && (
          <div className="flex items-center gap-2 text-xs text-text-secondary py-2">
            <Loader2 className="h-3.5 w-3.5 animate-spin" /> Loading live values…
          </div>
        )}
        <div className="space-y-2.5">
          {indices?.map((idx) => (
            <div key={idx.name} className="flex items-center justify-between text-sm">
              <span className="text-text-secondary">{idx.name}</span>
              <div className="text-right">
                <p className="font-medium text-text-primary">
                  {idx.value.toLocaleString(undefined, { maximumFractionDigits: 2 })}
                </p>
                <PriceChange percent={idx.changePercent} abs={idx.changeAbs} />
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="p-4 border-b border-border-subtle">
        <h3 className="text-sm font-semibold text-text-primary mb-3">FX</h3>
        {/* Forex data removed - PSX only */}
      </div>

      <div className="p-4 flex-1">
        <h3 className="text-sm font-semibold text-text-primary mb-3">Latest News</h3>
        <DataUnavailable
          title="News feed not available"
          reason="Free PSX data sources do not provide a company news feed. Prices, indices, and fundamentals shown across the app are live and real."
        />
      </div>

      <div className="p-4 border-t border-border-subtle">
        <p className="text-[11px] text-text-secondary">
          Live data: PSX Data Portal (prices, indices, fundamentals). EOD updates after market close.
        </p>
      </div>
    </aside>
  );
}
