import { seedCompanies } from "./seedCompanies";
import { generateCompany } from "./generateCompany";
import type { Company } from "@/types";

export const companies: Company[] = seedCompanies.map(generateCompany);

export const companiesByTicker: Record<string, Company> = Object.fromEntries(
  companies.map((c) => [c.ticker, c])
);

export function getCompany(ticker: string): Company | undefined {
  return companiesByTicker[ticker];
}
