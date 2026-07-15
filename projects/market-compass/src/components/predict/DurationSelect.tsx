import type { Duration } from "@/services/predict/types";

// Exactly the five in-scope horizons. The label is human-readable; the value is
// the engine's Duration code.
const DURATION_OPTIONS: { value: Duration; label: string }[] = [
  { value: "1M", label: "1 Month" },
  { value: "1Y", label: "1 Year" },
  { value: "3Y", label: "3 Years" },
  { value: "5Y", label: "5 Years" },
  { value: "20Y", label: "20 Years" },
];

export function DurationSelect({
  value,
  onChange,
  id = "predict-duration",
}: {
  value: Duration;
  onChange: (d: Duration) => void;
  id?: string;
}) {
  return (
    <div>
      <label htmlFor={id} className="text-xs text-text-secondary mb-1.5 block">
        Horizon
      </label>
      <select
        id={id}
        aria-label="Horizon"
        value={value}
        onChange={(e) => onChange(e.target.value as Duration)}
        className="w-full bg-bg-elevated border border-border-subtle rounded-lg px-3 py-2 text-sm text-text-primary outline-none"
      >
        {DURATION_OPTIONS.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
    </div>
  );
}
