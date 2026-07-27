# Phase 3 — Build Close-Out · Cycle 2, Slice 1 (Read-Only Bridge Seam)

**Project:** gold-signal-analyzer · **Date:** 2026-07-26
**Status:** **CLOSED — all Phase 2.5 conditions built in; three governance gaps closed; build genuinely green.**
**Branch:** `gold-signal-analyzer-foundation` (working tree, nothing committed this turn).

---

## What was already built (verified this session, not assumed)

Python read-only bridge package under
`src/GoldSignalAnalyzer.MT5Bridge/python/bridge/`: `commands.py`, `handlers.py`,
`mt5_gateway.py`, `server.py`, `audit.py`, `run_bridge.py`, plus
`tests/test_readonly_allowlist.py` (10 RM2 assertions) and
`tests/test_server_protocol.py` (9 auth/protocol assertions).

.NET side: §9 `IMarketDataProvider` reconciliation done (marker closed —
see below); `WindowsCredentialStore` (DPAPI) in Infrastructure + DI;
`bridgetoken` on `SecretDenylist`; new tests `WindowsCredentialStoreTests`,
`BridgeTokenSecretTests`, `Architecture/MarketDataPortShapeTests`.

## Three gaps confirmed open at resume — now CLOSED

| # | Gap (Phase 2.5 origin) | Fix | Verified |
|---|---|---|---|
| 1 | No `.gitignore` coverage for `__pycache__/` / `*.pyc` / `.venv/` (cond D1) | Added a Python section to root `.gitignore` (`__pycache__/`, `*.py[cod]`, `.venv/`, cache dirs). | `git check-ignore …/commands.cpython-314.pyc` → path echoed (exit 0 = ignored). `.pyc` were untracked-but-unignored; `git ls-files` confirms none were ever committed. |
| 2 | No `CODEOWNERS` on RM2-critical paths (cond C2, BLOCKER) | Created `.github/CODEOWNERS` covering `commands.py`, `mt5_gateway.py`, `tests/test_readonly_allowlist.py`, plus server/handlers/run_bridge, the DPAPI store + denylist, and the wrapper itself (`.github/**`, `.gitignore`) → `@vision1068`. | File present; paths match the tree exactly. |
| 3 | No CI workflow running RM2 on 3.9 + 3.14 as a merge-blocking check (cond C2) | Created `.github/workflows/rm2-readonly-gate.yml`: matrix `['3.9','3.14']`, stdlib `unittest discover`, **fails if the RM2 test file is missing** (`test -f … || exit 1`) as well as if red. Path-filtered to the python dir. | YAML valid; asserts presence + runs the suite. |

**Branch protection** (making CODEOWNERS + CI actually *block* merges) is a
repo-Settings toggle only the human owner can flip — Settings → Branches →
Require review from Code Owners **and** Require status checks (`rm2-gate`) on
the default branch. This is **NOT done** and cannot be done by code (market-compass
lesson, 2026-07-02). Flagged as a binding owner action; see Phase 6.

## Extra finding this session — the "0 warnings" claim was an incremental-build artifact

The resume checkpoint recorded `dotnet build` as "0 warnings / 0 errors." That was
true only because the build was **incremental** and skipped recompilation. A clean
`dotnet build --no-incremental` surfaced **4 real CA1416** platform-compatibility
warnings on `WindowsCredentialStore.cs` — DPAPI `ProtectedData` is Windows-only,
but the Infrastructure project targets `net8.0` (not `net8.0-windows`), so the call
sites read as cross-platform.

This is precisely the "a green build is not a verified requirement" trap
(lessons-learned 2026-07-24). Fixed honestly, not suppressed:
- `[SupportedOSPlatform("windows")]` on `WindowsCredentialStore` (truthful — the whole
  product is a WPF net8.0-windows desktop tool), and on the `AddGsaInfrastructure`
  DI method that composes it. No `#pragma warning disable`.
- Rebuild `--no-incremental`: **0 Warning(s) / 0 Error(s)**, all 12 projects.

## §9 reconciliation — re-verified real (not trusted from a diff)

`grep -r "NEEDS CLARIFICATION" src/` → **no matches**. `IMarketDataProvider.cs`
carries a ratified 8-member read-only reconciliation note (ADR-7): `SubscribeTicks`
deliberately excluded (poll-only source), enforced by `MarketDataPortShapeTests`.
The one non-blocking caveat (brief.md references but does not contain literal §9)
is documented in-code and carried to Phase 6.

## Verification evidence (real command output this session)

- Python 3.9.13: `py -3.9 -m unittest discover -s . -v` → **Ran 19 tests … OK**
- Python 3.14.6: `py -3.14 -m unittest discover -s . -v` → **Ran 19 tests … OK**
- `dotnet build GoldSignalAnalyzer.sln --no-incremental` → **0 Warning(s) / 0 Error(s)** (after CA1416 fix)
- `dotnet test GoldSignalAnalyzer.sln` → **Passed! Failed: 0, Passed: 45, Skipped: 0**
- Filtered spot-check (rule 8): `--filter WindowsCredentialStoreTests|BridgeTokenSecretTests|MarketDataPortShapeTests` → **Passed: 9** (3 + 2 + 4).

## Scope discipline (unchanged, re-affirmed)

No live MT5 connectivity, no order-execution capability, no `gateway.connect()`
call at runtime were added. `run_bridge.py` explicitly does not attach to a
terminal. `test_10_no_mt5_call_at_module_load` still green. The permanent
no-live-order-execution invariant is untouched.

**Phase 3 verdict: CLOSED.** Advances to Phase 4 (QA).
