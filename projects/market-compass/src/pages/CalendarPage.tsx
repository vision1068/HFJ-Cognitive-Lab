import { economicEvents } from "@/data/markets";
import { PageHeader } from "@/components/ui/PageHeader";
import { Badge } from "@/components/ui/Badge";
import { formatDate } from "@/lib/format";

export function CalendarPage() {
  const sorted = [...economicEvents].sort((a, b) => +new Date(a.date) - +new Date(b.date));

  return (
    <div className="p-4 md:p-6 max-w-3xl mx-auto">
      <PageHeader title="Economic Calendar" subtitle="Upcoming macroeconomic events that may affect markets you follow." />

      <div className="space-y-2.5">
        {sorted.map((e) => (
          <div key={e.title} className="card p-4 flex items-center gap-4">
            <div className="text-center w-16 shrink-0">
              <p className="text-xs text-text-secondary">{formatDate(e.date, { month: "short" })}</p>
              <p className="text-lg font-semibold text-text-primary">{new Date(e.date).getDate()}</p>
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-text-primary">{e.title}</p>
              <p className="text-xs text-text-secondary mt-0.5">{e.description}</p>
            </div>
            <div className="text-right shrink-0 space-y-1">
              <Badge tone="neutral">{e.region}</Badge>
              <Badge tone={e.impact === "High" ? "negative" : e.impact === "Medium" ? "warning" : "neutral"} className="block">{e.impact} Impact</Badge>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
