import { clsx } from "clsx";
import { CheckCircle2, Loader2, AlertTriangle, SearchX, KeyRound } from "lucide-react";
import type { DataConnectionStatus } from "@/hooks/useMarketData";

const STATUS_CONFIG: Record<
  DataConnectionStatus,
  { label: string; icon: typeof CheckCircle2; tone: string; spin?: boolean }
> = {
  connected: { label: "Live Data Connected", icon: CheckCircle2, tone: "bg-positive-bg text-positive" },
  loading: { label: "Loading Market Data", icon: Loader2, tone: "bg-neutral-bg text-neutral", spin: true },
  error: { label: "FMP API Error", icon: AlertTriangle, tone: "bg-negative-bg text-negative" },
  "no-data": { label: "No Data Available for This Symbol or Timeframe", icon: SearchX, tone: "bg-warning-bg text-warning" },
  "no-key": { label: "FMP API Key Required", icon: KeyRound, tone: "bg-warning-bg text-warning" },
};

export function DataStatusBadge({
  status,
  lastUpdated,
  className,
}: {
  status: DataConnectionStatus;
  lastUpdated?: Date;
  className?: string;
}) {
  const cfg = STATUS_CONFIG[status];
  const Icon = cfg.icon;

  return (
    <div className={clsx("inline-flex items-center gap-2", className)}>
      <span className={clsx("inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium", cfg.tone)}>
        <Icon className={clsx("h-3.5 w-3.5", cfg.spin && "animate-spin")} />
        {cfg.label}
      </span>
      {status === "connected" && lastUpdated && (
        <span className="text-xs text-text-secondary">
          Last updated {lastUpdated.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit", second: "2-digit" })}
        </span>
      )}
    </div>
  );
}
