# Phase 4 — QA (Independent Test Verification)

**Project:** gold-signal-analyzer · **Cycle:** 1 (Foundation) · **Date:** 2026-07-24
**Mandate:** Independent re-verification of B1–B4/R1–R6 against real `dotnet test` output (Constitution #2/#3). Adversarial — find what is under-tested, do not rubber-stamp.

---

## Verdict (as delivered): PASS-WITH-CONDITIONS → **condition now CLOSED**

QA re-ran build + suite from source (33/33 at review time) and read what each test actually asserts. **B1–B4 and R1–R5 genuinely verify** — the assertions are non-tautological. One required condition, **R6, was code-correct but untested**, and Phase 3's R6 traceability claim ("exercised via DI composition") was **false**. That blocking condition has since been closed (see §3); the suite is now **36/36 green**.

## 1. Real evidence (QA's own run, not trusted from Phase 3)

```
Build succeeded.  0 Warning(s)  0 Error(s)
Total tests: 33   Passed: 33   Failed: 0   Skipped: 0   (at review time)
```

## 2. Per-condition judgement (test + what it actually proves)

| ID | QA judgement | Basis |
|---|---|---|
| B1 | VERIFIED | `MigrationTests` uses a real OS-temp `ScratchDb` and **explicitly asserts** the dir ≠ `GsaHost.DefaultAppDataRoot`; exercises `AddGsaPersistence()` end-to-end via `host.StartAsync()`, then reads `sqlite_master` for the actual tables. All 5 host-building tests use a scratch path — none falls back to AppData. |
| B2 | VERIFIED | `MaskingEnricherTests` routes a real event through the real enricher + capturing sink; raw `hunter2`/`123456789` absent, nested `{@Config}.Token` masked, `ServerName` preserved. |
| B3 | VERIFIED (3 parts) | source-model reflection (a) + write-time denylist reject via real repo (b) + serialized-export scan (c, QA-F8). |
| B4 | VERIFIED (recurring) | enumerates all `appsettings*/*.config` under src every run, excludes bin/obj. Vacuous today (zero committed config files — strongest posture) but structurally correct for future adds. |
| R1 | VERIFIED | parses `.csproj <ProjectReference>` via `XDocument` (not compiled refs → immune to the false pass). |
| R2 | VERIFIED (both directions) | reflection set-equality catches an 18th (extra) AND a missing §30 impl; plus count==17 and 1:1 by order+title. |
| R3 | VERIFIED | parses `ScreenTemplates.xaml` (17 templates, identity) AND STA `LoadContent()`s the real runtime file — a missing/typo'd View throws, defeating a blank-`ContentControl` false-green. |
| R4 | VERIFIED | `git check-ignore` exit 0 for bin/obj/*.db/-wal/-shm/-journal/*.user/.vs/TestResults/logs. |
| R5 | VERIFIED | both scalar methods throw `NotConnectedException`; collections empty; `ConnectAsync` → NotConnected. |
| **R6** | **WAS WEAK (untested) → CLOSED** | Code correct (throws on all 3 members) but **no test asserted it**, and the DI-composition test never resolves `ICredentialStore`. A regression to a null-returning no-op (A07) would have stayed green. |

## 3. Blocking condition — closed after review

- **R6 test added** — `NotImplementedCredentialStoreTests` (2 tests): (a) the DI-resolved `ICredentialStore` IS `NotImplementedCredentialStore`; (b) every member throws. Re-run: **PASS**.
- **R1 gap-guard added** (QA non-blocking rec) — `ArchitectureTests.FR1_R1_every_inner_project_on_disk_is_covered_by_the_allow_map`: a new inner project can't silently escape the inward-only check. Re-run: **PASS**.
- Phase-3 doc corrected to remove the false R6 claim (phase-3-tech.md §4, §8).
- **Final suite: 36 passed / 0 failed, build 0/0.**

## 4. Should-fix nit status (QA verified against code)

- QA-F4 (ValidateOnBuild + ValidateScopes) — **LANDED** (`GsaHost`; `DiCompositionTests` builds the same validated host).
- QA-F7 (temp log path + CloseAndFlush) — **PARTIAL/acceptable**: log dir injectable + every test uses temp; static `CloseAndFlush` moot (instance logger via `AddSerilog(dispose:true)`; masking covered by B2's sink).
- QA-F11 (validation triggers on resolution) — **LANDED** (`ConfigValidationTests` forces `IOptions.Value` → `OptionsValidationException`).
- QA-F12 (exact-key masking, no over-masking) — **LANDED** (exact case-insensitive `HashSet`; B2 preserves `ServerName`).

## 5. Non-blocking items carried forward (Cycle-2 scope)

1. **Denylist is the single point of completeness** — B3/B4/masking only cover names in `SecretDenylist`; a secret named `passphrase`/`pin`/`otp` outside it is not caught. Accepted "convention-enforced + test-guarded" posture (B3c); review the denylist each cycle.
2. B4 is key-name based, not value/high-entropy based; widen when Cycle-2 introduces committed config.

**Bottom line:** B1–B4/R1–R6 all hold up to adversarial reading of the actual assertions; the one real gap (R6) is closed with a genuine test. QA sign-off condition met.
