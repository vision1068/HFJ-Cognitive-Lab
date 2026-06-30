import type { ReactNode } from "react";
import { clsx } from "clsx";
import type { SignalLevel } from "@/types";
import { SIGNAL_STYLES } from "@/lib/signal";

export function SignalBadge({ signal, className }: { signal: SignalLevel; className?: string }) {
  const s = SIGNAL_STYLES[signal];
  return (
    <span className={clsx("inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium", s.bg, s.text, className)}>
      <span className={clsx("h-1.5 w-1.5 rounded-full", s.dot)} />
      {s.label}
    </span>
  );
}

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
