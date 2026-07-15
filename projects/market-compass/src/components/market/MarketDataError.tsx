import { KeyRound, AlertTriangle, SearchX, RefreshCw } from "lucide-react";
import type { DataConnectionStatus } from "@/hooks/useMarketData";

/**
 * Full-width error / setup state shown in place of charts and signals
 * whenever real market data could not be retrieved or calculated.
 * There is no mock-data fallback path — this component is the only
 * thing rendered when `status !== "connected"`.
 */
export function MarketDataError({
  status,
  message,
  onRetry,
}: {
  status: Exclude<DataConnectionStatus, "connected" | "loading">;
  message?: string;
  onRetry?: () => void;
}) {
  if (status === "no-key") {
    return (
      <div className="rounded-xl border border-warning/30 bg-warning-bg/40 p-8 text-center">
        <KeyRound className="mx-auto h-10 w-10 text-warning mb-3" />
        <h3 className="text-lg font-semibold text-text-primary mb-2">FMP API Key Required</h3>
        <p className="text-sm text-text-secondary max-w-md mx-auto mb-4">
          Market Compass only shows real market data from Financial Modeling Prep. No mock or demo data is displayed.
          Add your API key to start seeing live prices, charts, and signals.
        </p>
        <div className="mx-auto max-w-md rounded-lg bg-bg-elevated border border-border-subtle p-4 text-left font-mono text-xs text-text-secondary">
          <div className="text-text-primary mb-1"># .env (project root)</div>
          VITE_FMP_API_KEY=your_real_fmp_api_key_here
        </div>
        <p className="text-xs text-text-secondary mt-4">
          Get a free key at{" "}
          <a
            href="https://site.financialmodelingprep.com/developer/docs"
            target="_blank"
            rel="noreferrer"
            className="text-brand-400 hover:underline"
          >
            financialmodelingprep.com
          </a>
          , then restart the dev server.
        </p>
      </div>
    );
  }

  if (status === "no-data") {
    return (
      <div className="rounded-xl border border-border-default bg-bg-elevated p-8 text-center">
        <SearchX className="mx-auto h-10 w-10 text-text-secondary mb-3" />
        <h3 className="text-lg font-semibold text-text-primary mb-2">No Data Available for This Symbol or Timeframe</h3>
        <p className="text-sm text-text-secondary max-w-md mx-auto mb-4">
          {message ?? "FMP did not return market data for this selection. Try a different symbol or timeframe."}
        </p>
        <p className="text-xs text-text-secondary">Technical signals are disabled until real data is available.</p>
      </div>
    );
  }

  // status === "error"
  return (
    <div className="rounded-xl border border-negative/30 bg-negative-bg/40 p-8 text-center">
      <AlertTriangle className="mx-auto h-10 w-10 text-negative mb-3" />
      <h3 className="text-lg font-semibold text-text-primary mb-2">FMP API Error</h3>
      <p className="text-sm text-text-secondary max-w-md mx-auto mb-4">
        {message ?? "Market data is temporarily unavailable due to API rate limits. Please refresh after a moment."}
      </p>
      {onRetry && (
        <button
          onClick={onRetry}
          className="inline-flex items-center gap-2 rounded-lg bg-brand-500 hover:bg-brand-600 px-4 py-2 text-sm font-medium text-white transition-colors"
        >
          <RefreshCw className="h-4 w-4" />
          Retry
        </button>
      )}
    </div>
  );
}
