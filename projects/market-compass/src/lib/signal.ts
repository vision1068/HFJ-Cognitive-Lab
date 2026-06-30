import type { SignalLevel } from "@/types";

export const SIGNAL_STYLES: Record<SignalLevel, { bg: string; text: string; dot: string; label: string }> = {
  "Strong Positive": { bg: "bg-positive-bg", text: "text-positive", dot: "bg-positive", label: "Strong Buy Candidate" },
  Positive: { bg: "bg-positive-bg", text: "text-positive", dot: "bg-positive", label: "Positive / Watch" },
  Neutral: { bg: "bg-neutral-bg", text: "text-neutral", dot: "bg-neutral", label: "Neutral" },
  Caution: { bg: "bg-warning-bg", text: "text-warning", dot: "bg-warning", label: "Caution" },
  "High Risk": { bg: "bg-negative-bg", text: "text-negative", dot: "bg-negative", label: "High Risk" },
};

export function scoreLabel(score: number): string {
  if (score >= 80) return "Excellent";
  if (score >= 65) return "Strong";
  if (score >= 50) return "Healthy";
  if (score >= 35) return "Neutral";
  if (score >= 20) return "Weak";
  return "High Risk";
}

export function scoreColor(score: number): string {
  if (score >= 65) return "text-positive";
  if (score >= 50) return "text-neutral";
  if (score >= 35) return "text-warning";
  return "text-negative";
}
