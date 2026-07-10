import { PageHeader } from "@/components/ui/PageHeader";
import { DataUnavailable } from "@/components/ui/DataState";

export function CalendarPage() {
  return (
    <div className="p-4 md:p-6 max-w-3xl mx-auto">
      <PageHeader title="Economic Calendar" subtitle="Upcoming macroeconomic events relevant to the PSX." />
      <DataUnavailable
        title="Economic calendar not available on free data sources"
        reason="There is no free, machine-readable feed of Pakistan macroeconomic events (SBP policy, CPI, etc.) that can be verified in real time, so no events are shown rather than placeholder dates. Live market data remains available across the app."
      />
    </div>
  );
}
