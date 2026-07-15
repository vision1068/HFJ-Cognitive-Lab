import { PageHeader } from "@/components/ui/PageHeader";

export function LiveAnalysisPage() {
  return (
    <div className="p-4 md:p-6 max-w-[1400px] mx-auto">
      <PageHeader
        title="Technical Analysis"
        subtitle="Advanced technical analysis features are being updated for PSX Data Portal"
      />
      <div className="rounded-xl border border-border-subtle bg-bg-surface p-8 text-center">
        <p className="text-sm text-text-secondary mb-4">
          Technical indicators and multi-timeframe analysis will be available in a future update.
        </p>
        <p className="text-xs text-text-secondary">
          For now, view live prices and fundamentals on the Markets and Company pages.
        </p>
      </div>
    </div>
  );
}
