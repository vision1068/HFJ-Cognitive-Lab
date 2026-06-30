import { clsx } from "clsx";
import { ArrowDown, ArrowUp } from "lucide-react";
import { formatPercent } from "@/lib/format";

export function PriceChange({ percent, abs, currency, size = "sm" }: { percent: number; abs?: number; currency?: string; size?: "sm" | "md" }) {
  const positive = percent >= 0;
  return (
    <span
      className={clsx(
        "inline-flex items-center gap-0.5 font-medium",
        positive ? "text-positive" : "text-negative",
        size === "sm" ? "text-xs" : "text-sm"
      )}
    >
      {positive ? <ArrowUp className="h-3 w-3" /> : <ArrowDown className="h-3 w-3" />}
      {abs !== undefined && currency ? `${currency}${Math.abs(abs).toFixed(2)} (` : ""}
      {formatPercent(percent, { showSign: false })}
      {abs !== undefined && currency ? ")" : ""}
    </span>
  );
}
