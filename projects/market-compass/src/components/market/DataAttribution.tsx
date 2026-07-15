import { clsx } from "clsx";

// Visible data-source attribution (Auditor D2). Honest provenance for the live
// market data rendered throughout the app.
export function DataAttribution({ className }: { className?: string }) {
  return (
    <p className={clsx("text-[11px] text-text-secondary", className)}>
      Data source: Yahoo Finance and the PSX Data Portal (dps.psx.com.pk). Quotes may be delayed.
    </p>
  );
}
