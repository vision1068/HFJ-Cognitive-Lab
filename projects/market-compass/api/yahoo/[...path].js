// Vercel serverless entry for Yahoo Finance. All logic (crumb handshake,
// host-pin, allowlist, rate-limit, secret redaction) lives in the shared
// _yahooProxy.js so dev (Vite middleware) and prod run identical code (NFR-5).
import { handleYahooProxy } from "./_yahooProxy.js";

export default function handler(req, res) {
  return handleYahooProxy(req, res);
}
