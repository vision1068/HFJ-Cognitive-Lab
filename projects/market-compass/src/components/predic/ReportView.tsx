import { AlertTriangle } from "lucide-react";
import type { PredicReport } from "@/services/predic/types";
import { AdviceDisclaimer } from "@/components/ui/Disclaimer";
import { Scorecard } from "./Scorecard";
import { SectionCard } from "./SectionCard";

// Renders a full Predic report. Ordering, top to bottom:
//   1. AdviceDisclaimer  — prominent, DIRECTLY above the verdict/scorecard.
//   2. Scorecard         — /100, verdict, per-category /10.
//   3. Low-confidence banner (only when coverage < 0.5).
//   4. Data-reliability summary — coverage % + the explicit list of N/A sections.
//   5. Every section, ordered by id (na sections stay visible).
export function ReportView({ report }: { report: PredicReport }) {
  const sections = [...report.sections].sort((a, b) => a.id - b.id);

  return (
    <div className="space-y-4">
      {/* Report meta */}
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <div>
          <h2 className="text-lg font-bold text-text-primary">
            {report.companyName}{" "}
            <span className="text-sm font-normal text-text-secondary">{report.ticker}</span>
          </h2>
          <p className="text-xs text-text-secondary">
            Horizon {report.duration} · generated {new Date(report.generatedAt).toLocaleString()} ·
            registry {report.registryVersion} · policy {report.policyVersion}
          </p>
        </div>
      </div>

      {/* 1. Prominent NOT-FINANCIAL-ADVICE disclaimer — directly above the verdict */}
      <AdviceDisclaimer />

      {/* 2. Verdict + scorecard */}
      <Scorecard scorecard={report.scorecard} />

      {/* 3. Low data confidence banner */}
      {report.scorecard.lowConfidence && (
        <div
          role="alert"
          className="flex items-start gap-3 rounded-xl border-2 border-negative/40 bg-negative-bg px-4 py-3"
        >
          <AlertTriangle className="h-5 w-5 shrink-0 text-negative mt-0.5" />
          <div className="text-sm text-text-secondary">
            <p className="font-semibold text-text-primary">Low data confidence</p>
            <p className="mt-0.5">
              Fewer than half of the weighted dimensions have a real PSX source at this horizon
              ({Math.round(report.coverage * 100)}% coverage). Treat the score and verdict as
              indicative only — several inputs are unavailable rather than measured.
            </p>
          </div>
        </div>
      )}

      {/* 4. Data reliability summary */}
      <section className="card p-4" aria-label="Data reliability">
        <h3 className="text-sm font-semibold text-text-primary mb-2">Data reliability</h3>
        <p className="text-xs text-text-secondary">
          Coverage: <span className="font-semibold text-text-primary">{Math.round(report.coverage * 100)}%</span> of
          weighted dimensions are backed by a real PSX Data Portal source.
        </p>
        {report.naSections.length > 0 ? (
          <div className="mt-2">
            <p className="text-xs text-text-secondary mb-1">
              Sections with no available data ({report.naSections.length}):
            </p>
            <ul className="space-y-1">
              {report.naSections.map((s) => (
                <li key={s.id} className="text-xs text-text-secondary">
                  <span className="font-medium text-text-primary">{s.title}</span> — {s.reason}
                </li>
              ))}
            </ul>
          </div>
        ) : (
          <p className="mt-2 text-xs text-text-secondary">
            Every in-scope section has at least some real PSX data at this horizon.
          </p>
        )}
      </section>

      {/* 5. Sections */}
      <div className="grid gap-4 md:grid-cols-2">
        {sections.map((s) => (
          <SectionCard key={s.id} section={s} />
        ))}
      </div>
    </div>
  );
}
