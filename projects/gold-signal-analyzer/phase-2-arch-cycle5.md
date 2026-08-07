# Cycle 5 — Architecture Phase (First-Run Setup Wizard, FR-28)

**Project:** gold-signal-analyzer · **Branch:** `agents/mt5-connectivity-bridge-gold-signal`
**Date:** 2026-07-31 · **Spec:** `cycle5-spec.md` · **Builds on:** Cycle 4 charts + notifications

Every decision below is anchored to the same north star as every prior cycle: the tool
stays **read-only, stores no secret, and has no order/execution path**. This slice adds
**configuration capture only** — it collects, validates and persists non-secret settings
and gates the dashboard on first launch. It performs no live connection (D5-5), collects
no credential (D5-3), and surfaces-but-blocks the live source (D5-2), so the C-3 live seam
is untouched. Three forks were resolved up front, all grounded in "no new dependency +
keep the testable-VM / dumb-XAML split."

## Fork 1 — Where the profile + store live (persistence placement)

Two real precedents in this codebase conflict, so this is a deliberate architecture call,
not a fait accompli.

| Option | Verdict |
|--------|---------|
| **A — Everything in `Presentation` (net8.0), mirroring `IAcknowledgementStore`/`FileAcknowledgementStore` file-for-file** | **CHOSEN.** The setup gate is *structurally the same problem* as the disclaimer gate — a first-run gate backed by a tiny persisted file — and that gate already lives entirely in `Presentation` (which does file I/O and is referenced by the test project). Colocating keeps the wizard self-contained, puts the `NeedsSetup` gate right next to the disclaimer gate it slots behind in `App.OnStartup`, and needs **zero Infrastructure churn**. `System.Text.Json` is in-framework, so `JsonFileSetupProfileStore` doing JSON file I/O in `Presentation` is exactly consistent with `FileAcknowledgementStore` doing marker-file I/O there. |
| B — `ISetupProfileStore` + `InMemory…` in `Application`, `JsonFileSetupProfileStore` in `Infrastructure` (the `IJournalStore`/`SqliteJournalStore` "big split", and the literal wording of NFR-SETUP-3) | **Rejected.** That precedent fits a heavyweight, DB-backed, cross-cutting store (SQLite journal); splitting a tiny first-run JSON marker across two assemblies is heavier than the problem and diverges from the sibling acknowledgement gate. `Presentation` references only Domain + Application (never Infrastructure), so Option B would also fragment the wizard across three assemblies for no testability gain — the test project can already exercise `Presentation` headlessly. **Documented deviation:** NFR-SETUP-3's literal placement sketch is superseded here by the closer `IAcknowledgementStore` analog; every NFR-SETUP-3 *acceptance* (durability via a fresh instance, no new package, dumb XAML, no custom converters) is still met in full. |

## Fork 2 — Data-source step: hide the live option, or surface-and-block it?

| Option | Verdict |
|--------|---------|
| Hide the live MT5 source entirely until C-3 | **Rejected.** Hiding it is less honest and hides the seam from the user and the auditor. |
| **List `Mt5Live` but make selecting it a validation failure carrying the exact C-3 reason** | **CHOSEN (D5-2).** The option is visible (`IsSelectable=false`, precomputed so XAML needs no converter) so the user understands the capability exists; selecting it yields the exact named-approver message and blocks `Next`. This surfaces the live seam **structurally without opening it** — only `SampleDemo` and historical `CsvFile` (both `IsLive=false`) are selectable. |

## Fork 3 — Secret handling in the wizard (D5-3)

| Option | Verdict |
|--------|---------|
| Capture a bridge token / read-only password now, for the future connect | **Rejected.** This slice performs no connection, so it needs no secret — collecting one would create a secret-at-rest surface for no benefit and contradict INV-2/NFR-SETUP-1. |
| **Collect NO secret; persist only a logical `CredentialKey` *name*** | **CHOSEN (D5-3).** `SetupProfile` carries only non-secret config; `CredentialKey` is the *name* of a future credential, never its value. Secret capture belongs to the future C-3 connect flow via the existing DPAPI `ICredentialStore` seam — which this slice deliberately does **not** reference. "No secret" is thus both structural (no secret member, reflection-proven, extending the `Mt5ConnectionOptions` guard) and behavioural (the persisted JSON is proven secret-free). |

## Layering (keeps validation/persistence out of the view)
```
Application (reused, no new files)
   BridgeEndpoint.Parse/Loopback  ← loopback-only invariant (Connection step)
   GoldSymbolResolver.RankGoldCandidates/SelectManually  ← real gold scoring (Symbol step)
   Mt5ConnectionMode (enum)       ← Mode A / Mode B, non-secret
Presentation/Setup (net8.0, TESTABLE — no WPF ref)
   SetupProfile + DataSourceKind  ← immutable, no-secret record (reflection-proven)
   ISetupProfileStore + InMemory… + JsonFileSetupProfileStore  ← durable JSON, in-framework
   SetupWizardViewModel           ← linear step machine + per-step validation + CurrentError
Wpf/SetupWizardWindow.xaml (DUMB) ← hidden-header TabControl on CurrentStepIndex, bool bindings
   App.OnStartup                  ← NeedsSetup gate, inserted after the disclaimer gate
```
No wizard element constructs an `IMt5BridgeClient` or issues a connect (D5-5), and the VM
holds no trade/execute command (INV-1 / NFR-SETUP-2, reflection-proven).

## What changes, by project
- **Application** (+0 files): reuses `BridgeEndpoint`, `GoldSymbolResolver`, `Mt5ConnectionMode` as-is — no new indicator/validation math to verify.
- **Presentation** (+3 files): `Setup/SetupProfile.cs` (`DataSourceKind` + `SetupProfile`), `Setup/ISetupProfileStore.cs` (interface + `InMemorySetupProfileStore` + `JsonFileSetupProfileStore`), `Setup/SetupWizardViewModel.cs` (`DataSourceOption`, `SymbolChoice`, the step machine).
- **Infrastructure** (+0 files): untouched (Fork 1 keeps the store in Presentation).
- **Wpf** (+2 files, DUMB): `SetupWizardWindow.xaml` (+`.cs`); `App.xaml.cs` gains the `NeedsSetup` gate line + a `SampleBrokerSymbols()` config list. No csproj/package change.
- **Tests** (+2 files, +1 case): `SetupWizardViewModelTests` (17), `JsonFileSetupProfileStoreTests` (4); `ProductionGatingTests` secret-guard `[Fact]`→`[Theory]` now covers `SetupProfile` (+1).

## Diagram — data flow (config capture only, no execution seam)
```mermaid
flowchart LR
    U[User input\n(non-secret config)] --> WIZ[SetupWizardViewModel\n(step machine + validation)]
    SYM[Available broker symbols\n(names, not prices — IsLive=false)] --> RES[GoldSymbolResolver\n(Application, pure)]
    RES --> WIZ
    WIZ --> BEP[BridgeEndpoint.Parse\n(loopback-only, no connect)]
    BEP --> WIZ
    WIZ --> PROF[SetupProfile\n(immutable, no secret)]
    PROF --> STORE[JsonFileSetupProfileStore\n(System.Text.Json, durable)]
    STORE --> GATE[App.OnStartup\nNeedsSetup gate]
    GATE --> DASH[Dashboard\n(read-only)]
    WIZ -.->|no order path| X((✗ execution))
    WIZ -.->|no action button| X
    PROF -.->|no secret member| X
```
No branch of this flow reaches an order/bridge-trade path or a live connection — INV-1..INV-5
stay structural.
