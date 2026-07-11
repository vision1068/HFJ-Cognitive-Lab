#!/usr/bin/env node
// Regenerates src/data/psxUniverse.json from the LIVE PSX Data Portal symbol list.
//
// Usage:  node scripts/fetch-psx-universe.mjs
//
// The committed psxUniverse.json is only a fallback seed (curated real symbols +
// clearly-labelled SEED-PLACEHOLDER rows). Run this from a network that can reach
// dps.psx.com.pk to replace it with the authoritative live universe.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const UA =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.resolve(__dirname, "../src/data/psxUniverse.json");

// [NEEDS CLARIFICATION] Exact field names of the /symbols payload are unverified
// (portal egress was blocked when this was written). Adjust the aliases below to
// match the real response, then re-run and eyeball the output before committing.
function isTradableEquity(row) {
  if (!row || typeof row.symbol !== "string") return false;
  if (row.isETF || row.isDebt || row.isNonEquity) return false;
  if (/[^A-Z0-9]/i.test(row.symbol)) return false;
  return true;
}

function parse(raw) {
  if (!Array.isArray(raw)) throw new Error("Unexpected /symbols shape: not an array");
  const seen = new Set();
  const out = [];
  for (const item of raw) {
    if (!isTradableEquity(item)) continue;
    const symbol = String(item.symbol).trim().toUpperCase();
    if (seen.has(symbol)) continue;
    seen.add(symbol);
    out.push({
      symbol,
      name: String(item.symbolName ?? item.name ?? item.companyName ?? symbol).trim(),
      sector: String(item.sectorName ?? item.sector ?? "UNCLASSIFIED").trim(),
    });
  }
  out.sort((a, b) => a.symbol.localeCompare(b.symbol));
  return out;
}

async function main() {
  const res = await fetch("https://dps.psx.com.pk/symbols", {
    headers: { "User-Agent": UA, Accept: "application/json" },
  });
  if (!res.ok) throw new Error(`PSX /symbols returned HTTP ${res.status}`);
  const raw = await res.json();
  const universe = parse(raw);
  if (universe.length < 100) {
    throw new Error(`Refusing to write only ${universe.length} symbols — response looks wrong.`);
  }
  fs.writeFileSync(OUT, JSON.stringify(universe, null, 2) + "\n");
  console.log(`Wrote ${universe.length} PSX symbols to ${OUT}`);
}

main().catch((err) => {
  console.error("fetch-psx-universe failed:", err.message);
  process.exitCode = 1;
});
