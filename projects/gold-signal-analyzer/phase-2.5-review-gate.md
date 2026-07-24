# Phase 2.5 — Plan-Review Gate (Adversarial)

**Project:** gold-signal-analyzer · **Cycle:** 1 (Foundation)
**Gate:** Orchestrator rule 7 — adversarial review of the Phase 2 plan BEFORE any code. Reviewers: `qa` and `auditor`, run in parallel, tasked to find what is wrong, not to approve.
**Reviewed artefacts:** `phase-2-arch.md`, `brief.md`, live repo `.gitignore`, Gate-1 checklist, quality-verification + secure-coding protocols.
**Date:** 2026-07-24

---

## Consolidated Verdict (single, per rule 11)

**PASS-WITH-CONDITIONS.** Both reviewers independently reached PASS-WITH-CONDITIONS. The clean-architecture spine, inward-only dependency rule, no-order-port graph, honest deferral of all live-MT5 connectivity, and no-secret-by-construction config posture are sound. It is **not** a rubber stamp: **4 BLOCKER conditions** must be resolved in the plan/build before Phase 3 is accepted, plus required conditions that must land in Phase 3 outputs, roadmap conditions folded into later cycles, and hardening should-fixes.

Because every blocker is a precise, testable constraint (not a redesign), they are resolved here as **binding design amendments** to Phase 2 (revision iteration 1 of the max-3 allowed) and become **Phase 3 acceptance criteria**. Phase 4 QA independently re-verifies each. No escalation to the user is required — no blocker remained contested.

---

## BLOCKERS — must be resolved before Phase 3 build is accepted

| # | Source | Finding | Binding resolution (Phase 3 acceptance criterion) |
|---|--------|---------|---------------------------------------------------|
| **B1** | QA-F1 | Migrate-on-startup uses a hardcoded `%LOCALAPPDATA%\gsa.db`; `AppConfiguration` has no DB-path field, so host-/collection-building tests (incl. `DiCompositionTests`) would create/migrate/lock the **real user DB** on dev/CI machines. Scratch override is not airtight. | Add `PersistenceOptions.DbPath` bound from config with an AppData default. Every host-/collection-building test supplies a scratch temp path; **no** test falls back to the AppData default. At least one test exercises `AddGsaPersistence()` end-to-end against the scratch path (proves the startup-migration path, not just a hand-built context). |
| **B2** | QA-F2 | The `SensitiveDataMaskingEnricher` (the primary write-time secret control, NFR-7 = Cycle-1) has **no test** — only `MaskingHelper` is tested. An untested requirement is unverified (Constitution #3). | Add `MaskingEnricherTests`: log an event carrying denylisted properties (`password`, `token`, `accountnumber`, …) through the real enricher + a capturing sink; assert output contains `***MASKED***` and **not** the raw value. |
| **B3** | Auditor-F1 | The "structurally incapable of persisting a plaintext secret" claim is test-enforced on the **export DTO only**. The bound `AppConfiguration` source model has no no-secret test, and `AppSettings` is a schemaless **key/value table** that structurally *can* hold a secret under any key — so the guarantee is prose, not structure, in two places. | (a) Add an `AppConfiguration`-wide no-secret-property reflection test mirroring `ConfigExportTests` onto the **source** model. (b) Constrain the `AppSettings` repository to **reject denylisted keys** at write time (with a test). (c) Where a claim can't be made structural, downgrade the §1/§6 wording from "structurally incapable" to "no secret column — convention-enforced + test-guarded" (audit-claim honesty). |
| **B4** | Auditor-F2 | Config **binding source** is unspecified; the generic host's default `appsettings.json` is typically committed → an unguarded plaintext-secret path (the doh-flight-checker class). The one-time scaffold grep does not stop a later hand-added secret in a committed config file. | Name the config source explicitly. Any future secret routes to a **gitignored** local file / .NET User Secrets, never a committed `appsettings`. Add a build-gate test that greps **all committed** config files (`appsettings*.json`, `*.config`) against the secret denylist — a recurring gate, not a one-time scaffold check. |

## REQUIRED conditions — must land in Phase 3 outputs

| # | Source | Condition |
|---|--------|-----------|
| R1 | QA-F3 | Enforce FR-1's dependency rule by **parsing `.csproj` `<ProjectReference>`** elements for Domain **and every inner project** (or NetArchTest with explicit boundaries) — NOT by reflecting over compiled assembly references (the compiler strips unused refs → false PASS). |
| R2 | QA-F5 | Check the **literal spec §30 screen list** into the repo and add a test asserting the 17 registered VMs map 1:1 to §30. Fix the §7/§12 traceability anchors to point at **§30 screen entries**, not functional sections. Until then FR-29 correspondence stays a documented `[NEEDS CLARIFICATION]`, not a silent pass. |
| R3 | QA-F6 | Add a **VM→View `DataTemplate` coverage test** for all 17 screens (count VM→View entries in the resource dictionary, or STA-instantiate each View) — headless VM navigation alone leaves a blank `ContentControl` bug green. |
| R4 | QA-F9 / Aud-F3 | Land the `.gitignore` **.NET section as the FIRST file written in Phase 3**, before any `bin/obj` exists — verified with `git check-ignore`. Include `bin/`, `obj/`, `*.user`, `*.suo`, `**/logs/`, `*.db`, `*.db-shm`, `*.db-wal`, **`*.db-journal`**, **`.vs/`**, **`TestResults/`**. (Lesson 2026-06-22: first commit must carry the ignore, verified not assumed.) |
| R5 | Auditor-F8 | `NullMarketDataProvider` must **throw** on every **scalar-returning** method (`GetLatestTickAsync`, `GetSymbolSpecAsync`) — a default/zero-valued `Tick`/`SymbolSpec` is fabricated market data (NFR-5). Empty **collections** only. Test asserts no scalar method returns a default instance. |
| R6 | Auditor-F9 | The deferred `ICredentialStore` is **unregistered (fail-fast on resolve)** or a `NotImplementedCredentialStore` that **throws** on every member — never a benign no-op returning null/empty (auth-bypass-by-default, A07). |

## ROADMAP conditions — fold into arch §13 / later-cycle contracts (do NOT build now)

| # | Source | Condition |
|---|--------|-----------|
| RM1 | Auditor-F5 | Cycle-2 bridge auth **token** is generated at setup and stored only in Windows Credential Manager/DPAPI via `ICredentialStore` — never in config or the DB. |
| RM2 | Auditor-F6 | **Critical.** The Python `MetaTrader5` bridge is `order_send`-capable and is NOT bound by the .NET no-order-port graph. Roadmap Phase 2 must constrain the bridge to a **read-only allowlist** of MT5 API calls (quotes/candles/symbol-info only), with a test asserting no order/trade API is reachable. The .NET-side absence of an order port does not bind the Python side — this protects the strongest invariant (spec §1/§43). |
| RM3 | Auditor-F7 | Roadmap Phase 2 explicitly re-affirms the **permanent** prohibition: never store Exness PA password / email / OTP / withdrawal credential; Mode-A/Mode-B read-only, never request the trading password (spec §5/§35/§44). |
| RM4 | Auditor-F10 | Model the disclaimer (FR-35) as a **blocking, versioned, audited acknowledgement gate** with a `DisclaimerAcknowledgement(version, acknowledgedUtc)` persistence concept — not merely nav-screen 17. |
| RM5 | Auditor-F11 | Establish a `SignalDisclosure`/wrapper concept so a signal cannot be rendered without its "not investment advice / score ≠ win-probability" disclosure (Predic lesson: enforce in type/wrapper, not review). Record that "personal use only" is itself a governance control; Phase 10 distribution re-triggers an advice/licensing review. |

## SHOULD-FIX / NITS — best-effort in Phase 3, else logged

- QA-F4: enable `ServiceProviderOptions.ValidateOnBuild = true` + `ValidateScopes`; test builds the same validated provider. Document that runtime-lazy resolutions stay out of scope.
- QA-F7: injectable **log** path pointed at a temp dir in tests; `Log.CloseAndFlush()` before asserting file existence.
- QA-F8: `ConfigExportTests` asserts against the **serialized output** of `ConfigExportService`, not just the DTO shape.
- QA-F11: `ConfigValidationTests` must **start the host** (or force `IOptions<AppConfiguration>.Value`) to trigger `ValidateOnStart` — `Build()` alone won't throw.
- QA-F12 / Auditor-F12: masking denylist uses **exact, case-insensitive key names**, not a `path` substring (avoid over-masking non-secret `Mt5TerminalPath`/`ServerName`, preserving §39 auditability).
- QA-F10: reduce the C1 `IMarketDataProvider` surface to the minimum `NullMarketDataProvider` + DI actually need, or clearly quarantine the guessed methods; keep the `[NEEDS CLARIFICATION]` marker open for Cycle-2 reconciliation.
- Auditor-F14: design the `IsAvailableInProduction` selection gate alongside the property so it does not become dead code when `TestMarketDataProvider` lands.
- Auditor-F4 (recurse): masking enricher should recurse into `StructureValue`/`SequenceValue`; design must forbid `{@…}` destructuring of any credential/config object (exception-borne secrets are covered by the no-secret-by-construction primary control, which the enricher cannot reach).

---

## Gate 1 — Spec Consistency result (merged)

| Check | Result | Note |
|---|---|---|
| Every FR has an ID + acceptance criterion | **PASS** | brief §3 tables, FR-1..FR-35 |
| No `[NEEDS CLARIFICATION]` unresolved | **CONDITIONAL PASS — documented deferral** | One open marker (arch §8, `IMarketDataProvider` §9 verbatim shape). Correctly scoped to Roadmap FR-10 / Cycle 2; nothing in C1 builds against its exact shape. Recorded as an **accepted, explicitly-scoped deferral** — not silently ticked green. |
| Scope (in/out/permanent) explicit | **PASS** | brief §2 + arch scope boundary |
| Prior outputs don't contradict spec | **PASS (one noted tension)** | arch's non-verbatim `IMarketDataProvider` vs FR-10 AC "matches §9 exactly" — roadmap-only, tolerable |

## Coordinator-mandated checks

- **(a) No credentials anywhere** — CONFIRMED (both reviewers). No secret field in `AppConfiguration`; channel settings are booleans; `ICredentialStore` deferred with no C1 impl. Reinforced by B3 (source-model + AppSettings test) and B4 (committed-config grep).
- **(b) `.gitignore` covers future local-secret file AND .NET artefacts** — PARTIAL TODAY → closed by R4. Secrets are covered now (`.env*`, `*.pem`, `*.key`, `secrets.json`); the .NET/db/log section does not yet exist and MUST be the first Phase-3 write.
- **(c) Roadmap defers ALL live MT5 connectivity** — CONFIRMED. Scope boundary, §3 data-flow (only the app box built), ADR-4, §13 roadmap; `NullMarketDataProvider` does no terminal I/O; `MarketData`/`MT5Bridge` are stub shells; no MetaTrader5 package reference or socket in Foundation. RM2 additionally hardens the future Python bridge against its own execution capability.

---

## Disposition

Revision iteration **1 of 3** — all 4 blockers resolved on paper via binding amendments above; no blocker remains contested, so re-review converges and the gate **passes with conditions**. Phase 3 proceeds with B1–B4 + R1–R6 as hard acceptance criteria; RM1–RM5 are recorded against the roadmap for later cycles; should-fixes are best-effort. Phase 4 QA independently re-verifies every blocker and required condition against real `dotnet test` output.
