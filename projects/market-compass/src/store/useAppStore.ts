import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { PortfolioHolding, PriceAlert, Watchlist } from "@/types";

interface AppState {
  theme: "dark" | "light";
  toggleTheme: () => void;

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
      { ticker: "ENGROH", addedAt: new Date().toISOString() },
      { ticker: "MEBL", addedAt: new Date().toISOString() },
      { ticker: "SYS", addedAt: new Date().toISOString() },
      { ticker: "OGDC", addedAt: new Date().toISOString() },
    ],
  },
  {
    id: "wl-dividend",
    name: "Dividend Payers",
    group: "Dividend stocks",
    entries: [
      { ticker: "FFC", addedAt: new Date().toISOString() },
      { ticker: "MCB", addedAt: new Date().toISOString() },
      { ticker: "HUBC", addedAt: new Date().toISOString() },
    ],
  },
];

const defaultPortfolio: PortfolioHolding[] = [
  { id: "p1", ticker: "ENGROH", shares: 500, purchasePrice: 268.4, purchaseDate: "2025-02-10", currency: "PKR" },
  { id: "p2", ticker: "MEBL", shares: 300, purchasePrice: 165.2, purchaseDate: "2025-05-22", currency: "PKR" },
  { id: "p3", ticker: "FFC", shares: 800, purchasePrice: 118.9, purchaseDate: "2024-11-08", currency: "PKR" },
  { id: "p4", ticker: "OGDC", shares: 400, purchasePrice: 210.5, purchaseDate: "2025-01-15", currency: "PKR" },
];

const defaultAlerts: PriceAlert[] = [
  { id: "a1", ticker: "ENGROH", type: "above", targetPrice: 330, active: true, createdAt: new Date().toISOString() },
  { id: "a2", ticker: "OGDC", type: "below", targetPrice: 300, active: true, createdAt: new Date().toISOString() },
  { id: "a3", ticker: "SYS", type: "above", targetPrice: 600, active: false, createdAt: new Date().toISOString() },
];

export const useAppStore = create<AppState>()(
  persist(
    (set, get) => ({
      theme: "dark",
      toggleTheme: () => set((s) => ({ theme: s.theme === "dark" ? "light" : "dark" })),

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
    { name: "market-compass-psx-store" }
  )
);
