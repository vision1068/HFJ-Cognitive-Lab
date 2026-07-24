# Phase 3 — Foundation Build (Technical)

**Project:** gold-signal-analyzer · **Cycle:** 1 (Foundation) · **Date:** 2026-07-24
**Gate carried in:** Phase 2.5 Plan-Review — PASS-WITH-CONDITIONS, binding **B1–B4** + **R1–R6** as hard Phase 3 acceptance criteria.
**Status:** COMPLETE — build green, all acceptance criteria test-verified against real `dotnet` output.

---

## 1. Verification evidence (real command output, not "should work")

Run from `projects/gold-signal-analyzer/src`, SDK `8.0.100`, EF tools `8.0.29`.

```
dotnet build GoldSignalAnalyzer.sln -c Debug --nologo
Build succeeded.
    0 Warning(s)
    0 Error(s)

dotnet test GoldSignalAnalyzer.sln -c Debug --nologo
Passed!  - Failed:     0, Passed:    36, Skipped:     0, Total:    36
```

Independently re-run by the orchestrator after the frontend slice landed (rule 8 — subagent "done" claim not trusted; command re-executed). Final count is **36** after Phase-4 QA's R6 finding was closed (see §4 R6 and §8).

## 2. What was already built coming into this session (backend/persistence/security/architecture)

The scaffolded .NET 8 clean-architecture solution (12 projects + Tests) was present with the security/persistence/architecture spine implemented: `AppConfiguration` (no secret fields), Options-pattern validation with `ValidateOnStart`, Serilog masking enricher, `ConfigExportService` whitelist DTO, `SecretDenylist`, `AppSettingsRepository` write-time secret rejection, `NullMarketDataProvider`, `NotImplementedCredentialStore`, `GoldSignalDbContext` + design-time factory + migrate-on-start hosted service, and 24 tests. Two of those 24 were **RED** on entry.

## 3. Gaps closed this session

| Gap | Root cause | Fix | Evidence |
|---|---|---|---|
| **B1 / B3 tests RED** — `SQLite Error 1: 'no such table: AppSettings'` | `MigrateAsync()` wired correctly but **zero EF migration classes existed** (`Migrations/` folder absent); `Migrate()` created only `__EFMigrationsHistory`, no domain tables. | Installed `dotnet-ef 8.0.29`; generated `InitialCreate` migration into the Persistence project via the existing design-time factory. | `MigrationTests.B1…` + `AppSettingsSecurityTests.B3…` now green. |
| **R2 / R3 / FR-5 / FR-29 unbuilt** — only `ShellViewModel` existed; no 17 screens, no navigation, no screen-list, no coverage tests. | Frontend slice of Phase 3 had not been implemented (both source comments read "Frontend expands this"). | `frontend` specialist built the 17 §30 placeholder VM+View pairs, `INavigationService`, checked-in §30 list, and the R2/R3/NavigationTests. | +9 tests, all green (see §4). |

## 4. Acceptance-criteria traceability — every B/R maps to a passing test

| ID | Requirement | Verifying test(s) | Result |
|---|---|---|---|
| **B1** | `AddGsaPersistence()` migrates a **scratch** temp DB (never AppData); creates the 3 tables + EF history | `MigrationTests.B1_AddGsaPersistence_migrates_scratch_db_and_creates_expected_tables` (asserts scratch path ≠ AppData root) | PASS |
| **B2** | Real masking enricher masks denylisted props incl. nested `{@…}`; preserves non-secret siblings | `MaskingEnricherTests.B2_…including_nested_and_preserves_non_secrets` | PASS |
| **B3** | (a) source-model no-secret reflection; (b) `AppSettings` write-time denylist rejection; (c) serialized-export no-secret | `ConfigExportTests.B3_AppConfiguration_source_model_has_no_secret_property`, `ConfigExportTests.B3_exported_json_contains_no_denylisted_key`, `AppSettingsSecurityTests.B3_writing_denylisted_key_throws_and_non_secret_key_roundtrips` | PASS |
| **B4** | Recurring grep of **all committed** `appsettings*/*.config` against denylist (excl. bin/obj) | `CommittedConfigSecretScanTests.B4_no_committed_config_file_contains_a_secret_key` | PASS |
| **R1** | Inward-only rule enforced by parsing `.csproj <ProjectReference>` (not compiled refs) for Domain + 11 inner projects | `ArchitectureTests.FR1_Domain_references_nothing` + `FR1_R1_inner_project_references_are_inward_only` (×11) | PASS |
| **R2** | 17 registered VMs map 1:1 to the checked-in §30 screen list (count + identity + order); no 18th, none missing | `ScreenListTests` (×4: 17-count, 1:1-by-order-and-title, no-extra/no-missing reflection, all-resolvable-from-container) | PASS |
| **R3** | VM→View `DataTemplate` coverage for all 17 (headless parse + STA `LoadContent`) — blank-ContentControl bug not left green | `DataTemplateCoverageTests` (×2: declares-exactly-one-per-VM, instantiates-correct-View) | PASS |
| **R4** | `.gitignore` .NET section is first-written, verified with `git check-ignore` (bin/obj/db/wal/journal/.vs/TestResults/.user) | `git check-ignore` — all 8 probe paths ignored (exit 0) | PASS |
| **R5** | `NullMarketDataProvider` **throws** on scalar methods (no fabricated Tick/SymbolSpec); empty collections only | `NullMarketDataProviderTests` (×3: scalar-tick-throws, scalar-spec-throws, collections-empty+not-connected) | PASS |
| **R6** | `ICredentialStore` binding **throws** on every member (no auth-bypass no-op) | `NotImplementedCredentialStoreTests.R6_DI_resolved_credential_store_is_the_throwing_type_not_a_noop` (asserts the DI-resolved binding IS `NotImplementedCredentialStore`) + `R6_every_member_throws_and_never_returns_a_value` | PASS |
| **FR-1/FR-2** | 12 projects build; DI resolves shell + market-data port + all 17 VMs | `DiCompositionTests.FR2_…` + `Arch190_each_of_the_17_screen_view_models_resolves_from_the_container` | PASS |
| **FR-5/FR-29** | Navigate to each of 17 screens; `CurrentViewModel` changes; one nav item per §30 screen | `NavigationTests` (×2) | PASS |

## 5. Notable engineering decision (frontend, load-bearing)

WPF markup-compiled XAML in this toolchain cannot resolve same-assembly types via `x:Type` (VMs live in the Desktop assembly, not a separate one — the "no new projects" constraint forbids the usual split). Reproduced: `Views/ScreenTemplates.xaml` failed `MC3050`. Fix: `ScreenTemplates.xaml` ships as a **loose `Resource` (not markup-compiled `Page`)** with explicit `;assembly=GoldSignalAnalyzer.Desktop` on clr-namespaces, merged at runtime in `App.OnStartup` via `XamlReader.Load`; the R3 STA test loads that same file and `LoadContent()`s all 17 templates, so the runtime path is test-covered (a missing/typo'd View throws there, not at first navigation).

## 6. Explicitly NOT built (correct scope — deferred, honest)

- All live-MT5 connectivity, scoring, indicators, order paths — Cycle 2+ (stub shells only; `ArchitectureTests` proves no forbidden refs).
- Screen 17 is a **nav placeholder**, not the RM4 blocking/versioned disclaimer acknowledgement gate — that persistence concept is a later cycle.
- RM1–RM5 roadmap conditions recorded against the roadmap, not implemented (per gate).
- One accepted deferral remains open: `IMarketDataProvider` byte-exact §9 shape reconciles at Cycle-2 intake (documented `[NEEDS CLARIFICATION]`, nothing in C1 builds against its exact shape).

## 7. Handoff to Phase 4 (QA) / Phase 5 (Audit)

Both phases independently re-verified B1–B4/R1–R6 against actual `dotnet test` output (Constitution #2/#3) — they re-ran, not trusting this document.

## 8. Post-review correction (Phase-4 QA finding closed)

QA's independent Phase-4 re-verification (correctly) rejected the original R6 traceability: R6 code was correct but had **no behavioral test**, and this document's original claim that R6 was "exercised via DI composition" was **false** — `DiCompositionTests` never resolves `ICredentialStore`. Closed here, not waived:
- Added `NotImplementedCredentialStoreTests` (2 tests): DI-resolved binding IS the throwing `NotImplementedCredentialStore`, and every member throws — a regression to a null/empty no-op (the A07 bypass R6 prevents) now turns the suite red.
- Added `ArchitectureTests.FR1_R1_every_inner_project_on_disk_is_covered_by_the_allow_map` (QA non-blocking R1 gap-guard): a new inner project can no longer escape the inward-only check by being absent from the allow-map.

Final evidence after the fix: **build 0/0, test 36 passed / 0 failed** (33 → 36).
