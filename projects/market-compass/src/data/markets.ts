import type { EconomicEvent, MarketIndex } from "@/types";

export const marketIndices: MarketIndex[] = [
  { name: "KSE-100", region: "Pakistan", value: 86421.34, changePercent: 0.84, changeAbs: 720.1 },
  { name: "KMI-30", region: "Pakistan", value: 142318.6, changePercent: 0.62, changeAbs: 879.4 },
  { name: "S&P 500", region: "United States", value: 5987.42, changePercent: 0.41, changeAbs: 24.5 },
  { name: "Nasdaq Composite", region: "United States", value: 19542.18, changePercent: 0.73, changeAbs: 141.2 },
  { name: "Dow Jones", region: "United States", value: 43218.55, changePercent: -0.18, changeAbs: -77.9 },
  { name: "FTSE 100", region: "United Kingdom", value: 8312.7, changePercent: 0.22, changeAbs: 18.3 },
  { name: "Nikkei 225", region: "Japan", value: 39842.1, changePercent: -0.55, changeAbs: -220.6 },
  { name: "DAX", region: "Europe", value: 19284.3, changePercent: 0.35, changeAbs: 67.4 },
  { name: "Euro Stoxx 50", region: "Europe", value: 4982.6, changePercent: 0.12, changeAbs: 6.0 },
  { name: "MSCI World", region: "Global", value: 3812.9, changePercent: 0.29, changeAbs: 11.1 },
];

export const economicEvents: EconomicEvent[] = [
  { date: addDays(1), title: "SBP Monetary Policy Statement", region: "Pakistan", impact: "High", description: "State Bank of Pakistan announces interest rate decision; markets expect rates to hold steady amid easing inflation." },
  { date: addDays(2), title: "US Non-Farm Payrolls", region: "United States", impact: "High", description: "Monthly US jobs report; a key input for Federal Reserve rate-path expectations." },
  { date: addDays(3), title: "Pakistan CPI Inflation Release", region: "Pakistan", impact: "High", description: "Monthly consumer price index data, closely watched ahead of the next SBP policy meeting." },
  { date: addDays(5), title: "ECB Interest Rate Decision", region: "Europe", impact: "Medium", description: "European Central Bank policy meeting; markets pricing in a cautious, data-dependent stance." },
  { date: addDays(6), title: "US CPI Inflation Data", region: "United States", impact: "High", description: "Headline and core inflation readings that influence Fed policy expectations and bond yields." },
  { date: addDays(8), title: "OPEC+ Production Meeting", region: "Global", impact: "Medium", description: "Oil producers meet to review output quotas, with implications for energy-sector equities." },
  { date: addDays(10), title: "Bank of Japan Policy Meeting", region: "Japan", impact: "Medium", description: "BoJ rate decision and forward guidance, relevant for yen-sensitive exporters." },
  { date: addDays(12), title: "UK GDP Growth Report", region: "United Kingdom", impact: "Medium", description: "Quarterly GDP growth estimate for the UK economy." },
  { date: addDays(14), title: "Pakistan Trade Balance Report", region: "Pakistan", impact: "Low", description: "Monthly import/export data affecting currency and current account outlook." },
  { date: addDays(15), title: "US FOMC Meeting Minutes", region: "United States", impact: "Medium", description: "Detailed account of the Federal Reserve's most recent policy meeting." },
];

function addDays(n: number) {
  const d = new Date();
  d.setDate(d.getDate() + n);
  return d.toISOString().slice(0, 10);
}

export const dailyBriefing = {
  summary:
    "Global markets traded mostly higher today as easing inflation data lifted risk sentiment across major indices. The KSE-100 extended its rally on strong foreign and local institutional buying in banking and fertilizer names, while US indices were supported by resilient tech earnings. Asian markets were mixed as investors weighed Bank of Japan policy signals.",
  keyEvents: [
    "US inflation data came in softer than expected, reinforcing rate-cut expectations later this year.",
    "Pakistan's current account showed a narrower deficit, supporting the rupee against the dollar.",
    "Oil prices eased on higher-than-expected US inventory builds, weighing on regional energy stocks.",
    "Gold held near recent highs as investors continued to seek safe-haven assets amid mixed macro signals.",
  ],
  commodities: [
    { name: "Brent Crude", value: "$78.42", change: -0.6 },
    { name: "Gold", value: "$2,648", change: 0.4 },
    { name: "USD/PKR", value: "278.35", change: 0.1 },
    { name: "EUR/USD", value: "1.062", change: -0.2 },
  ],
};
