---
name: secure-coding
description: >
  OWASP Top 10 mapped to concrete code patterns for our actual stacks:
  Power Platform / Dataverse / CRM on-premise, and Node.js/React/TypeScript
  (Market Compass, Flight Checker). Read this before writing any
  implementation code — backend, frontend, middleware, crm-developer,
  power-platform, mobile. Adapted from ConnectSW's secure-coding.md
  (github.com/Tamoura/Claude-Code-creates-the-SW-company).
---

# Secure Coding Protocol

Security vulnerabilities in production are a development failure, not
a QA failure. This maps OWASP Top 10 (2021) to concrete patterns —
not general advice, but what to actually do in our two stacks.

## A01: Broken Access Control

**Node/React stack:**
- Verify resource ownership before returning data: `if (record.userId !== req.user.id) return 403`
- Check role before privileged operations, not just authentication
- Never trust a client-supplied ID without an ownership check server-side

**Power Platform / CRM stack:**
- Field-level security on sensitive columns, not just entity-level
- Security roles scoped to business unit, never "System Administrator" for service accounts
- Plugin pre-images used to validate the caller had rights to the pre-change state, not just the post-change state

## A02: Cryptographic Failures

- Passwords: bcrypt, cost factor ≥ 12 (Node stack)
- Never `Math.random()` for tokens/secrets — use `crypto.randomBytes()`
- Token comparison uses `crypto.timingSafeEqual()`, never `===`
- CRM: connection strings and service account credentials in Azure Key Vault or CRM connection references — never in plugin code or unmanaged config

## A03: Injection

- SQL: parameterized queries only (Prisma, or tagged `$queryRaw` templates) — never string-concatenated SQL
- CRM: FetchXML/QueryExpression with parameters, never string-built FetchXML from user input
- No `eval()`, `new Function()`, or user-supplied input into `RegExp()`
- All API routes validate input with a schema (Zod or equivalent) before handler logic runs

## A04: Insecure Design

- Fail fast: validate required config/connection strings at startup, crash loudly rather than fail silently at request time
- CRM plugins: always assume the 2-minute sandbox timeout — long operations go async via queue/service handoff, never inline

## A05: Security Misconfiguration

- Helmet (or equivalent) middleware on every Node API with a real CSP — no `unsafe-inline`/`unsafe-eval`
- Rate limiting on every public endpoint; auth endpoints rate-limited stricter than general traffic
- CRM: managed solutions only in Test/UAT/Prod — unmanaged components are a misconfiguration risk in themselves
- No wildcard CORS in production — explicit origin allowlist only

## A06: Vulnerable Components

- Run `npm audit` (or equivalent) before adding any new dependency
- CI blocks on HIGH/CRITICAL vulnerabilities — no `continue-on-error` on the security job
- CRM: verify SDK/package versions match the on-premise CRM version before referencing new APIs

## A07: Authentication Failures

- Access tokens short-lived (≈15 min), refresh tokens longer with revocation capability
- Auth endpoints rate-limited (e.g. 5 attempts/min) — stricter than general API limits
- Generic error message on login failure ("invalid credentials") — never reveal whether the username exists
- CRM: dedicated service accounts only for integrations, never a named user's credentials

## A08: Software and Data Integrity

- Validate all inter-service/inter-agent messages against a schema before acting on them
- Never execute LLM-generated code or commands directly — route through an explicit allowlist of operations
- CRM: plugin registration steps (stage, mode, filtering attributes) documented and reviewed — a wrongly-scoped plugin is an integrity risk

## A09: Logging and Monitoring Failures

- Log security events with structured context: event type, user/service account ID, IP, timestamp — never log secrets, tokens, or passwords, even partially
- CRM: audit trail enabled at the entity level for anything financially or legally significant; audit log itself is append-only

## A10: Server-Side Request Forgery (SSRF)

- Any server-side fetch of a user-supplied URL validates against an explicit allowlist first
- Block private IP ranges (`10.`, `172.16-31.`, `192.168.`, `127.`) unless the target is intentionally internal and reviewed

## Frontend Security (XSS)

- Never `dangerouslySetInnerHTML` without sanitizing through DOMPurify first
- Prefer React's default text rendering (auto-escaped) over any raw HTML injection
- Auth tokens in httpOnly cookies, never `localStorage` (XSS-readable)

## Secrets Management

- All secrets in environment variables / GitHub Actions secrets / Azure Key Vault — never hardcoded, never committed
- `.env.example` documents required variables with no real values
- `.gitignore` must exclude `.env`, `.env.local` — verify this explicitly on every new project (this was missed once on Market Compass and had to be fixed after the fact)

## Forbidden Patterns — Quick Reference

| Pattern | Risk |
|---|---|
| `eval(userInput)` / `new Function(str)` | Code injection |
| `Math.random()` for tokens/secrets | Predictable, guessable |
| `token === storedToken` | Timing attack |
| `new RegExp(userInput)` | ReDoS |
| `dangerouslySetInnerHTML` without sanitizing | XSS |
| API key or connection string in source | Credential exposure |
| Client-supplied ID trusted without ownership check | Broken access control |
| Plugin doing synchronous long-running work | Sandbox timeout, unreliable |

## Security Self-Review Checklist (before marking any task complete)

- [ ] All inputs validated with a schema
- [ ] Resource ownership checked server-side, not just client-side
- [ ] Roles/permissions checked before privileged operations
- [ ] No hardcoded secrets (grep the diff before committing)
- [ ] No `.env`/credentials committed — check `.gitignore` covers them
- [ ] Rate limiting present on public/auth endpoints
- [ ] No unsanitized HTML injection
- [ ] Security-relevant events logged with context, no secrets in the log
- [ ] Dependency audit run and clean (or documented exception)
