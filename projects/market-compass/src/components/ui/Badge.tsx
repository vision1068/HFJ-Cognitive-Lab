import type { ReactNode } from "react";
import { clsx } from "clsx";

export function Badge({ children, tone = "neutral", className }: { children: ReactNode; tone?: "positive" | "negative" | "warning" | "neutral" | "brand"; className?: string }) {
  const tones: Record<string, string> = {
    positive: "bg-positive-bg text-positive",
    negative: "bg-negative-bg text-negative",
    warning: "bg-warning-bg text-warning",
    neutral: "bg-neutral-bg text-neutral",
    brand: "bg-brand-500/15 text-brand-400",
  };
  return <span className={clsx("inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium", tones[tone], className)}>{children}</span>;
}
