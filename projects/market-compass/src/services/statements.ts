import type { FinancialStatement, StatementSet } from "@/types";
import { YAHOO_BASE, fetchJson } from "./http";

// FR-7: multi-year financial statements. Yahoo v10 quoteSummary publishes them
// for GLOBAL symbols only. For PSX `.KA` symbols Yahoo returns HTTP 404
// ("No fundamentals data found for symbol") — VERIFIED — so PSX statements are
// an honest "not available", NEVER fabricated.

const PSX_UNAVAILABLE_REASON = "No free data source publishes PSX financial statements.";
const GLOBAL_UNAVAILABLE_REASON = "No financial statements were published for this symbol.";

const MODULES = "incomeStatementHistory,balanceSheetHistory,cashflowStatementHistory";

interface YahooRawValue {
  raw?: number;
  fmt?: string | null;
}
interface IncomeRow {
  endDate?: YahooRawValue;
  totalRevenue?: YahooRawValue;
  netIncome?: YahooRawValue;
}
interface BalanceRow {
  endDate?: YahooRawValue;
  totalAssets?: YahooRawValue;
}
interface CashflowRow {
  endDate?: YahooRawValue;
  netIncome?: YahooRawValue;
  totalCashFromOperatingActivities?: YahooRawValue;
  freeCashflow?: YahooRawValue;
}
interface QuoteSummaryResult {
  incomeStatementHistory?: { incomeStatementHistory?: IncomeRow[] };
  balanceSheetHistory?: { balanceSheetStatements?: BalanceRow[] };
  cashflowStatementHistory?: { cashflowStatements?: CashflowRow[] };
}
interface QuoteSummaryResponse {
  quoteSummary?: { result?: QuoteSummaryResult[] | null; error?: unknown };
}

function num(v: YahooRawValue | undefined): number | null {
  return v && typeof v.raw === "number" && Number.isFinite(v.raw) ? v.raw : null;
}
function periodKey(v: YahooRawValue | undefined): string | null {
  if (v?.fmt) return v.fmt;
  if (typeof v?.raw === "number") return new Date(v.raw * 1000).toISOString().slice(0, 10);
  return null;
}

export function isPsxSymbol(symbol: string): boolean {
  return /\.KA$/i.test(symbol);
}

/**
 * Maps a v10 quoteSummary payload (success OR the 404 "no fundamentals" body)
 * into a StatementSet. A null/empty result or an error node yields
 * `available:false` — the honest-N/A branch.
 */
export function parseStatements(symbol: string, raw: QuoteSummaryResponse): StatementSet {
  const result = raw?.quoteSummary?.result?.[0];
  if (!result || raw?.quoteSummary?.error) {
    return { symbol, available: false, reason: GLOBAL_UNAVAILABLE_REASON, currency: null, source: null, statements: [] };
  }

  const byPeriod = new Map<string, FinancialStatement>();
  const ensure = (key: string): FinancialStatement => {
    let row = byPeriod.get(key);
    if (!row) {
      row = { fiscalPeriodEnd: key, revenue: null, netIncome: null, totalAssets: null, operatingCashFlow: null, freeCashFlow: null };
      byPeriod.set(key, row);
    }
    return row;
  };

  for (const r of result.incomeStatementHistory?.incomeStatementHistory ?? []) {
    const key = periodKey(r.endDate);
    if (!key) continue;
    const row = ensure(key);
    row.revenue = num(r.totalRevenue);
    if (row.netIncome == null) row.netIncome = num(r.netIncome);
  }
  for (const r of result.balanceSheetHistory?.balanceSheetStatements ?? []) {
    const key = periodKey(r.endDate);
    if (!key) continue;
    ensure(key).totalAssets = num(r.totalAssets);
  }
  for (const r of result.cashflowStatementHistory?.cashflowStatements ?? []) {
    const key = periodKey(r.endDate);
    if (!key) continue;
    const row = ensure(key);
    row.operatingCashFlow = num(r.totalCashFromOperatingActivities);
    row.freeCashFlow = num(r.freeCashflow);
    if (row.netIncome == null) row.netIncome = num(r.netIncome);
  }

  const statements = Array.from(byPeriod.values()).sort((a, b) => b.fiscalPeriodEnd.localeCompare(a.fiscalPeriodEnd));
  if (statements.length === 0) {
    return { symbol, available: false, reason: GLOBAL_UNAVAILABLE_REASON, currency: null, source: null, statements: [] };
  }
  return { symbol, available: true, currency: null, source: "Yahoo Finance", statements };
}

/**
 * FR-7: fetch statements. PSX symbols short-circuit to honest N/A (Yahoo 404s
 * them); global symbols are fetched and mapped, with any error also yielding a
 * non-fabricated `available:false`.
 */
export async function fetchStatements(symbol: string): Promise<StatementSet> {
  if (isPsxSymbol(symbol)) {
    return { symbol, available: false, reason: PSX_UNAVAILABLE_REASON, currency: null, source: null, statements: [] };
  }
  try {
    const raw = await fetchJson<QuoteSummaryResponse>(
      `${YAHOO_BASE}/v10/finance/quoteSummary/${encodeURIComponent(symbol)}?modules=${MODULES}`
    );
    return parseStatements(symbol, raw);
  } catch {
    return { symbol, available: false, reason: GLOBAL_UNAVAILABLE_REASON, currency: null, source: null, statements: [] };
  }
}
