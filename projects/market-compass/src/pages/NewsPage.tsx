import { PageHeader } from "@/components/ui/PageHeader";
import { DataUnavailable } from "@/components/ui/DataState";

export function NewsPage() {
  return (
    <div className="p-4 md:p-6 max-w-4xl mx-auto">
      <PageHeader title="News & Insights" subtitle="Company news and sentiment for PSX-listed companies." />
      <DataUnavailable
        title="News feed not available on free data sources"
        reason="This build sources only real, live market data. Free PSX / Yahoo endpoints do not provide a reliable per-company news or sentiment feed, so nothing is shown here rather than fabricated headlines. Live prices, indices, and PSX-reported fundamentals remain available across the app."
      />
    </div>
  );
}
