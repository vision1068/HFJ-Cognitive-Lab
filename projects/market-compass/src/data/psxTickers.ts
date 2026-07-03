import type { Sector } from "@/types";

// Static descriptive metadata for the PSX companies we cover.
// These are stable real-world facts (name, sector, CEO, website, description).
// ALL market data (price, history, fundamentals) is fetched live — nothing here
// is a price or a valuation.
export interface PsxTicker {
  ticker: string;
  psxSymbol: string; // symbol used against dps.psx.com.pk endpoints
  name: string;
  logoInitials: string;
  logoColor: string;
  sector: Sector;
  industry: string;
  description: string;
  website: string;
  ceo: string;
}

export const psxTickers: PsxTicker[] = [
  {
    ticker: "ENGROH",
    psxSymbol: "ENGROH",
    name: "Engro Holdings Limited",
    logoInitials: "EN",
    logoColor: "#0ea5a4",
    sector: "Fertilizer",
    industry: "Diversified Conglomerate",
    description:
      "Engro Holdings is one of Pakistan's largest diversified conglomerates with interests in fertilizer, energy, foods, and petrochemicals.",
    website: "engro.com",
    ceo: "Ghias Khan",
  },
  {
    ticker: "HBL",
    psxSymbol: "HBL",
    name: "Habib Bank Limited",
    logoInitials: "HB",
    logoColor: "#16a34a",
    sector: "Banking",
    industry: "Commercial Banking",
    description:
      "Habib Bank Limited is Pakistan's largest bank by assets, offering retail, corporate, and Islamic banking services across the country and abroad.",
    website: "hbl.com",
    ceo: "Muhammad Aurangzeb",
  },
  {
    ticker: "MCB",
    psxSymbol: "MCB",
    name: "MCB Bank Limited",
    logoInitials: "MC",
    logoColor: "#0369a1",
    sector: "Banking",
    industry: "Commercial Banking",
    description:
      "MCB Bank is one of the oldest and most profitable banks in Pakistan, known for strong asset quality and consistent dividend payouts.",
    website: "mcb.com.pk",
    ceo: "Shoaib Mumtaz",
  },
  {
    ticker: "MEBL",
    psxSymbol: "MEBL",
    name: "Meezan Bank Limited",
    logoInitials: "ME",
    logoColor: "#15803d",
    sector: "Banking",
    industry: "Islamic Banking",
    description:
      "Meezan Bank is Pakistan's largest Islamic bank, with rapid branch network growth and a strong Shariah-compliant deposit franchise.",
    website: "meezanbank.com",
    ceo: "Irfan Siddiqui",
  },
  {
    ticker: "OGDC",
    psxSymbol: "OGDC",
    name: "Oil & Gas Development Company",
    logoInitials: "OG",
    logoColor: "#b45309",
    sector: "Oil & Gas",
    industry: "Exploration & Production",
    description:
      "OGDCL is Pakistan's largest oil and gas exploration and production company, with a track record of high reserves and strong dividends.",
    website: "ogdcl.com",
    ceo: "Ahmed Hayat Lak",
  },
  {
    ticker: "PPL",
    psxSymbol: "PPL",
    name: "Pakistan Petroleum Limited",
    logoInitials: "PP",
    logoColor: "#a16207",
    sector: "Oil & Gas",
    industry: "Exploration & Production",
    description:
      "Pakistan Petroleum Limited is a leading E&P company with major gas fields including Sui, supplying a significant share of Pakistan's natural gas.",
    website: "ppl.com.pk",
    ceo: "Imran Abbasy",
  },
  {
    ticker: "LUCK",
    psxSymbol: "LUCK",
    name: "Lucky Cement Limited",
    logoInitials: "LU",
    logoColor: "#475569",
    sector: "Cement",
    industry: "Cement Manufacturing",
    description:
      "Lucky Cement is Pakistan's largest cement producer and exporter, with growing diversification into power, chemicals, and automobiles.",
    website: "lucky-cement.com",
    ceo: "Muhammad Ali Tabba",
  },
  {
    ticker: "FFC",
    psxSymbol: "FFC",
    name: "Fauji Fertilizer Company",
    logoInitials: "FF",
    logoColor: "#65a30d",
    sector: "Fertilizer",
    industry: "Fertilizer Manufacturing",
    description:
      "Fauji Fertilizer Company is Pakistan's leading urea producer, known for strong cash generation and consistent dividend distribution.",
    website: "ffc.com.pk",
    ceo: "Jahangir Piracha",
  },
  {
    ticker: "SYS",
    psxSymbol: "SYS",
    name: "Systems Limited",
    logoInitials: "SY",
    logoColor: "#7c3aed",
    sector: "Technology",
    industry: "IT Services & Software Export",
    description:
      "Systems Limited is Pakistan's largest listed IT services exporter, providing software development and digital transformation services globally.",
    website: "systemsltd.com",
    ceo: "Asif Peer",
  },
  {
    ticker: "HUBC",
    psxSymbol: "HUBC",
    name: "Hub Power Company",
    logoInitials: "HU",
    logoColor: "#0891b2",
    sector: "Power",
    industry: "Independent Power Producer",
    description:
      "Hub Power Company is one of Pakistan's largest independent power producers, diversifying into coal, LNG, renewables, and fuel retail.",
    website: "hubpower.com",
    ceo: "Kamran Kamal",
  },
];

export const psxTickerBySymbol: Record<string, PsxTicker> = Object.fromEntries(
  psxTickers.map((t) => [t.ticker, t])
);
