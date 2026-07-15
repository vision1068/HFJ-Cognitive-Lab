import type { SectionResult } from "@/services/predict/types";
import { Badge } from "@/components/ui/Badge";
import { SourcedFigureView } from "./SourcedFigureView";

const STATUS_META: Record<
  SectionResult["status"],
  { tone: "positive" | "warning" | "neutral"; label: string }
> = {
  full: { tone: "positive", label: "Full data" },
  partial: { tone: "warning", label: "Partial data" },
  na: { tone: "neutral", label: "No data" },
};

// One report section. Sections with status "na" are NOT hidden — they render with
// an explicit "No data" badge and reason so a thin report can never masquerade as
// a complete one (Constitution Article 1: honest N/A, never a fabricated value).
export function SectionCard({ section }: { section: SectionResult }) {
  const meta = STATUS_META[section.status];
  const isNa = section.status === "na";

  return (
    <section className="card p-4" aria-label={section.title}>
      <div className="flex items-center justify-between gap-2 mb-3">
        <h3 className="text-sm font-semibold text-text-primary">
          <span className="text-text-secondary font-normal mr-1.5">{section.id}.</span>
          {section.title}
        </h3>
        <Badge tone={meta.tone}>{meta.label}</Badge>
      </div>

      {isNa ? (
        <p className="text-xs text-text-secondary">
          <span className="font-medium text-text-primary">Data not available.</span>{" "}
          {section.note ?? "No PSX Data Portal source publishes the figures for this section."}
        </p>
      ) : (
        <dl className="space-y-2.5">
          {section.figures.map((f, i) => (
            <div key={`${f.label}-${i}`} className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
              <dt className="text-xs text-text-secondary">{f.label}</dt>
              <dd className="text-right">
                <SourcedFigureView figure={f.figure} />
              </dd>
            </div>
          ))}
          {section.note && <p className="text-[11px] text-text-secondary pt-1">{section.note}</p>}
        </dl>
      )}
    </section>
  );
}
