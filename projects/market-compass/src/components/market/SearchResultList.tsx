import { Link } from "react-router-dom";
import type { Company, SearchResult } from "@/types";
import { CompanyLogo } from "@/components/ui/CompanyLogo";
import { PriceChange } from "@/components/ui/PriceChange";
import { SourceBadge } from "@/components/market/SourceBadge";
import { formatCurrency } from "@/lib/format";

/**
 * Renders "Smart Search" hits (FR-5) as routable rows. PSX hits are enriched
 * from the loaded universe (`companyByTicker`) for logo + live price; global
 * (Yahoo) hits that carry no local quote show an explicit "no local quote"
 * note rather than a fabricated price. Every row carries a source badge.
 */
export function SearchResultList({
  results,
  companyByTicker,
}: {
  results: SearchResult[];
  companyByTicker: Map<string, Company>;
}) {
  return (
    <div className="space-y-2.5">
      {results.map((r) => {
        const c = companyByTicker.get(r.ticker);
        const hasPrice = c != null && c.price > 0;
        return (
          <Link
            key={`${r.source}-${r.ticker}`}
            to={`/company/${r.ticker}`}
            className="card card-hover p-4 flex items-center gap-4 flex-wrap sm:flex-nowrap"
          >
            <CompanyLogo
              initials={c?.logoInitials ?? r.ticker.slice(0, 2).toUpperCase()}
              color={c?.logoColor ?? "#64748b"}
              size={40}
            />
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-2 flex-wrap">
                <p className="font-medium text-text-primary truncate">{c?.name ?? r.name}</p>
                <span className="text-xs text-text-secondary">{r.ticker}</span>
                <SourceBadge source={r.source} />
              </div>
              <p className="text-xs text-text-secondary truncate">
                {r.exchange}
                {c?.sector ? ` · ${c.sector}` : ""}
              </p>
            </div>
            <div className="text-right w-32 shrink-0">
              {hasPrice ? (
                <>
                  <p className="font-medium text-text-primary">{formatCurrency(c.price, c.currency)}</p>
                  <PriceChange percent={c.changePercent} />
                </>
              ) : (
                <p className="text-xs text-text-secondary">No local quote</p>
              )}
            </div>
          </Link>
        );
      })}
    </div>
  );
}
