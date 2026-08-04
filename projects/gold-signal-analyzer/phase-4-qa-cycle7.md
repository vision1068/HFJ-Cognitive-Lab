# Phase 4 — QA Gate — Cycle 7 (Live Read-Only MT5 Feed)

**Verdict: PASS.**

Adversarial re-run of the full suite against the actual working tree (not trusting any
prior "done" claim), plus the new FR-38 audit-log tests. All commands executed this cycle.

## Test evidence (real command output)

| Suite | Command | Result |
|-------|---------|--------|
| C# unit/integration (Release) | `dotnet test GoldSignalAnalyzer.sln -c Release` | **Failed: 0, Passed: 218, Skipped: 0** |
| WPF entry build (Release) | `dotnet build GoldSignalAnalyzer.Wpf.csproj -c Release` | **Build succeeded. 0 Warning(s), 0 Error(s)** |
| Python bridge | `python -m unittest discover -s tests` | **Ran 32 tests … OK** |

Baseline was 212 C# tests; +6 net-new FR-38 audit tests → 218. The GUI project was built
explicitly (standing lesson: `dotnet test` does not build the WinExe entry project).

## What was tested (traceability)

- **FR-36** (read-only attach + conversion): `test_source_calls_only_read_only_mt5_apis`,
  `test_source_exposes_no_order_verb`, `test_initialize_mode_a_passes_no_credentials`,
  candle/tick/timeframe conversion tests; C# wizard `Mt5Live` selectable + no-secret persist.
- **FR-37** (live coordinator + status strip): `LiveSignalCoordinatorTests` (7) — disconnected,
  no-mapping, fresh-allowed, stale, future-dated, empty-pull, no-order-member;
  `LiveStatusViewModelTests` (4).
- **FR-38** (audit log — NEW): suppressed→no-numeric-fields, allowed→captures-displayed,
  in-memory Recent ordering, file append-only+durable, secret-free JSONL, no-execution-member.

## Edge cases explicitly covered

- Suppression on every unsafe condition returns a **null** analysis (no fabricated price).
- Future-dated tick is treated as stale (not trusted as live).
- Empty candle pull suppresses rather than analysing nothing.
- Audit append-only: a second fresh writer does not erase the first entry (durability proven
  across a reopen, per the Cycle-2 "durable means read-back on a fresh instance" lesson).

## Structural gates (not just green tests)

- Order/execution verbs in production C#/Python: **0**.
- `#if DEBUG` / `[Conditional]` anywhere: **0** (Debug/Release safety parity).
- `GoldSignalAnalyzer.Testing.dll` in WPF Release output: **ABSENT** (test provider gated out
  by assembly graph, not a runtime flag).

## Not covered here (routed / deferred, not hidden)

- Real MT5 terminal E2E connect — no MT5/broker credentials in this environment (expected);
  validated via injected-fake tests only. **Owner manual step.**
- Cycle-6 AC-34.3 published-exe launch from a wiped profile — needs an interactive desktop
  session; NOT run here. **Owner manual step.** (Carried forward, not closed.)
