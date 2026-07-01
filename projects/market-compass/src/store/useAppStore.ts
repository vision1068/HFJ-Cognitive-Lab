import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { Currency, PortfolioHolding, PriceAlert, Watchlist } from "@/types";

interface AppState {
  theme: "dark" | "light";
  toggleTheme: () => void;

  baseCurrency: Currency;
  setBaseCurrency: (c: Currency) => void;

  watchlists: Watchlist[];
  addWatchlist: (name: string, group: string) => void;
  removeWatchlist: (id: string) => void;
  toggleWatch: (watchlistId: string, ticker: string) => void;
  isWatched: (ticker: string) => boolean;

  portfolio: PortfolioHolding[];
  addHolding: (holding: Omit<PortfolioHolding, "id">) => void;
  removeHolding: (id: string) => void;

  alerts: PriceAlert[];
  addAlert: (alert: Omit<PriceAlert, "id" | "createdAt">) => void;
  removeAlert: (id: string) => void;
  toggleAlert: (id: string) => void;

  sidebarCollapsed: boolean;
  toggleSidebar: () => void;
}

const defaultWatchlists: Watchlist[] = [
  {
    id: "wl-default",
    name: "My Watchlist",
    group: "Personal",
    entries: [
      { ticker: "ENGRO", addedAt: new Date().toISOString() },
      { ticker: "MEBL", addedAt: new Date().toISOString() },
      { ticker: "AAPL", addedAt: new Date().toISOString() },
      { ticker: "NVDA", addedAt: new Date().toISOString() },
      { ticker: "SYS", addedAt: new Date().toISOString() },
    ],
  },
  {
    id: "wl-dividend",
    name: "Dividend Stocks",
    group: "Dividend stocks",
    entries: [
      { ticker: "FFC", addedAt: new Date().toISOString() },
      { ticker: "MCB", addedAt: new Date().toISOString() },
      { ticker: "JPM", addedAt: new Date().toISOString() },
    ],
  },
  {
    id: "wl-growth",
    name: "Growth Stocks",
    group: "Growth stocks",
    entries: [
      { ticker: "TSLA", addedAt: new Date().toISOString() },
      { ticker: "SYS", addedAt: new Date().toISOString() },
      { ticker: "GOOGL", addedAt: new Date().toISOString() },
    ],
  },
];

const defaultPortfolio: PortfolioHolding[] = [
  { id: "p1", ticker: "ENGRO", shares: 500, purchasePrice: 268.4, purchaseDate: "2024-02-10", currency: "PKR" },
  { id: "p2", ticker: "MEBL", shares: 300, purchasePrice: 165.2, purchaseDate: "2024-05-22", currency: "PKR" },
  { id: "p3", ticker: "AAPL", shares: 15, purchasePrice: 192.3, purchaseDate: "2024-01-15", currency: "USD" },
  { id: "p4", ticker: "NVDA", shares: 40, purchasePrice: 98.6, purchaseDate: "2024-06-03", currency: "USD" },
  { id: "p5", ticker: "FFC", shares: 800, purchasePrice: 118.9, purchaseDate: "2023-11-08", currency: "PKR" },
];

const defaultAlerts: PriceAlert[] = [
  { id: "a1", ticker: "ENGRO", type: "above", targetPrice: 330, active: true, createdAt: new Date().toISOString() },
  { id: "a2", ticker: "AAPL", type: "below", targetPrice: 210, active: true, createdAt: new Date().toISOString() },
  { id: "a3", ticker: "TSLA", type: "above", targetPrice: 270, active: false, createdAt: new Date().toISOString() },
];

export const useAppStore = create<AppState>()(
  persist(
    (set, get) => ({
      theme: "dark",
      toggleTheme: () => set((s) => ({ theme: s.theme === "dark" ? "light" : "dark" })),

      baseCurrency: "USD",
      setBaseCurrency: (c) => set({ baseCurrency: c }),

      watchlists: defaultWatchlists,
      addWatchlist: (name, group) =>
        set((s) => ({
          watchlists: [...s.watchlists, { id: `wl-${Date.now()}`, name, group, entries: [] }],
        })),
      removeWatchlist: (id) => set((s) => ({ watchlists: s.watchlists.filter((w) => w.id !== id) })),
      toggleWatch: (watchlistId, ticker) =>
        set((s) => ({
          watchlists: s.watchlists.map((w) => {
            if (w.id !== watchlistId) return w;
            const exists = w.entries.some((e) => e.ticker === ticker);
            return {
              ...w,
              entries: exists
                ? w.entries.filter((e) => e.ticker !== ticker)
                : [...w.entries, { ticker, addedAt: new Date().toISOString() }],
            };
          }),
        })),
      isWatched: (ticker) => get().watchlists.some((w) => w.entries.some((e) => e.ticker === ticker)),

      portfolio: defaultPortfolio,
      addHolding: (holding) => set((s) => ({ portfolio: [...s.portfolio, { ...holding, id: `p-${Date.now()}` }] })),
      removeHolding: (id) => set((s) => ({ portfolio: s.portfolio.filter((p) => p.id !== id) })),

      alerts: defaultAlerts,
      addAlert: (alert) =>
        set((s) => ({
          alerts: [...s.alerts, { ...alert, id: `a-${Date.now()}`, createdAt: new Date().toISOString() }],
        })),
      removeAlert: (id) => set((s) => ({ alerts: s.alerts.filter((a) => a.id !== id) })),
      toggleAlert: (id) => set((s) => ({ alerts: s.alerts.map((a) => (a.id === id ? { ...a, active: !a.active } : a)) })),

      sidebarCollapsed: false,
      toggleSidebar: () => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),
    }),
    { name: "market-compass-store" }
  )
);
