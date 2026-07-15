// reportBuilder — top-level engine entry point.
//
// generatePredicReport(ticker, duration): builds the PSX context, runs every
// in-scope section builder, re-guards every figure (AC-1 belt & suspenders so no
// rendered number can escape without source + asOf), computes the scorecard, and
// stamps the report with the registry + policy versions and render time.

import type {
  Duration,
  PredicReport,
  SectionResult,
  SectionFigure,
  NaSection,
} from "./types";
import { buildContext } from "./dataBinding";
import { inScopeSections, POLICY_VERSION } from "./durationPolicy";
import { SECTION_BUILDERS, REGISTRY_VERSION } from "./sectionRegistry";
import { computeScorecard } from "./scorecard";
import { guard, isNa } from "./realDataGuard";

function naReasonOf(section: SectionResult): string {
  const firstNa = section.figures.find((f) => isNa(f.figure));
  if (firstNa && isNa(firstNa.figure)) return firstNa.figure.reason;
  return section.note ?? "No PSX source available";
}

export async function generatePredicReport(
  ticker: string,
  duration: Duration,
): Promise<PredicReport> {
  const ctx = await buildContext(ticker, duration);

  const sections: SectionResult[] = [];
  for (const id of inScopeSections(duration)) {
    const raw = SECTION_BUILDERS[id](ctx);
    // Re-guard every figure — nothing rendered may lack source + asOf.
    const figures: SectionFigure[] = raw.figures.map((f) => ({
      label: f.label,
      figure: guard(f.figure),
    }));
    sections.push({ ...raw, figures });
  }

  const scorecard = computeScorecard(ctx);

  const naSections: NaSection[] = sections
    .filter((s) => s.status === "na")
    .map((s) => ({ id: s.id, title: s.title, reason: naReasonOf(s) }));

  return {
    ticker: ctx.ticker,
    companyName: ctx.company.name,
    duration,
    generatedAt: new Date().toISOString(),
    registryVersion: REGISTRY_VERSION,
    policyVersion: POLICY_VERSION,
    sections,
    scorecard,
    coverage: scorecard.coverage,
    naSections,
  };
}
