import type { Currency } from "@/types";

// Approximate illustrative FX rates to USD (units of currency per 1 USD).
export const FX_TO_USD: Record<Currency, number> = {
  USD: 1,
  PKR: 278.35,
  GBP: 0.79,
  EUR: 0.95,
  JPY: 152.4,
};

export function convertCurrency(amount: number, from: Currency, to: Currency): number {
  const usd = amount / FX_TO_USD[from];
  return usd * FX_TO_USD[to];
}
