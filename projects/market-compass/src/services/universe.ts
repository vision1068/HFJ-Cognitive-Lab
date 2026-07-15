import type { UniverseSymbol } from "@/types";
import { PSX_BASE, fetchJson } from "./http";
import fallbackUniverse from "@/data/psxUniverse.json";

// FR-4: the full PSX universe. Live source is dps.psx.com.pk/symbols (via the
// same-origin /api/psx proxy). If that fails, or returns an implausibly small
// list, we fall back to the committed src/data/psxUniverse.json seed so the app
// always has a broad universe to search and quote.

const MIN_PLAUSIBLE_UNIVERSE = 400;

// [NEEDS CLARIFICATION] The exact field names of the dps.psx.com.pk/symbols
// payload could NOT be verified from the build sandbox (egress to the PSX portal
// was blocked). This parser accepts the most likely shapes — a JSON array whose
// rows carry a symbol plus a name and a sector under any of the aliases below.
// Verify against a real response and tighten when the live shape is confirmed.
interface RawPsxSymbol {
  symbol?: string;
  symbolName?: string;
  name?: string;
  companyName?: string;
  sectorName?: string;
  sector?: string;
  isETF?: boolean;
  isDebt?: boolean;
  isNonEquity?: boolean;
}

function isTradableEquity(row: RawPsxSymbol): boolean {
  if (!row.symbol || typeof row.symbol !== "string") return false;
  if (row.isETF || row.isDebt || row.isNonEquity) return false;
  // Futures / index / derivative symbols carry a suffix separator (e.g. "MEBL-OCT").
  if (/[^A-Z0-9]/i.test(row.symbol)) return false;
  return true;
}

export function parsePsxSymbols(raw: unknown): UniverseSymbol[] {
  if (!Array.isArray(raw)) return [];
  const out: UniverseSymbol[] = [];
  const seen = new Set<string>();
  for (const item of raw as RawPsxSymbol[]) {
    if (!isTradableEquity(item)) continue;
    const symbol = (item.symbol as string).trim().toUpperCase();
    if (seen.has(symbol)) continue;
    seen.add(symbol);
    out.push({
      symbol,
      name: (item.symbolName ?? item.name ?? item.companyName ?? symbol).toString().trim(),
      sector: (item.sectorName ?? item.sector ?? "UNCLASSIFIED").toString().trim(),
    });
  }
  return out;
}

export function getFallbackUniverse(): UniverseSymbol[] {
  return fallbackUniverse as UniverseSymbol[];
}

/**
 * FR-4: load the PSX universe, preferring the live endpoint and falling back to
 * the committed seed on any failure or an implausibly small live list.
 */
export async function fetchUniverse(): Promise<UniverseSymbol[]> {
  try {
    const raw = await fetchJson<unknown>(`${PSX_BASE}/symbols`);
    const parsed = parsePsxSymbols(raw);
    if (parsed.length >= MIN_PLAUSIBLE_UNIVERSE) return parsed;
    // Live list too small / unparseable — use the committed seed.
    return getFallbackUniverse();
  } catch {
    return getFallbackUniverse();
  }
}
