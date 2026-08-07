# Cycle 5 Spec — Gold Signal Analyzer: First-Run Setup Wizard (FR-28)

**Project:** gold-signal-analyzer
**Cycle / slice:** Add a guided **first-run setup wizard** that captures, validates and
persists the app's **non-secret** configuration (data source, gold symbol, connection
settings, account balance) and gates the dashboard on first launch.
**Branch / worktree:** `agents/mt5-connectivity-bridge-gold-signal`
**Date:** 2026-07-31
**Owner:** vision1068 (human)

> Extends Cycle 1 (`brief.md`, read-only MT5 bridge scaffold), Cycle 2
> (`cycle2-spec.md`, analytical core), Cycle 3 (`cycle3-spec.md`, UI shell) and
> Cycle 4 (`cycle4-spec.md`, charts + notifications). Same independent-build /
> no-live-connectivity / no-order-execution constraints apply verbatim
> (S-1…S-3, INV-1…INV-5). This slice adds **configuration capture only** — it
> collects and validates settings and writes them to a local profile. It performs
> **no live MT5 connection**, stores **no secret**, and introduces **no
> order/execution surface of any kind**. It stays firmly inside the existing
> read-only / paper / educational envelope; the live seam (**C-3**) is untouched.

## Why this slice exists (business framing — CEO Phase 1)

Every prior cycle hard-coded the app's configuration in the WPF composition root
(`App.OnStartup` always builds a fixed sample candle series). A real user has no way
to tell the app *which* data they want analysed, *which* broker symbol is their gold
instrument, or *what* account balance to size the paper risk plan against. The
`phase-6-ceo-cycle4.md` sign-off named the setup wizard (FR-28) as the highest-value
next slice that stays completely inside the safe read-only envelope — it is the
concrete precondition for the tool being usable by anyone other than a developer, and
the natural predecessor to packaging (FR-34). It was chosen over real-time
auto-refresh and a paper-execution action surface precisely because those two edge
toward the C-3 live/execution seam, whereas a configuration wizard does not.

## Permanent invariants (re-asserted, verified this phase)
- **INV-1** No order execution/modification/closing — the wizard adds **no** affordance
  that could place, simulate, or arm an order. It has no trade/execute command.
- **INV-2 / INV-3** No stored PA/email/OTP/trading-password/withdrawal credential; no
  scraping. **This slice collects NO secret at all** — see D5-3.
- **INV-4** No fabricated market data presented as live. The wizard chooses a **data
  source**; the only selectable sources are non-live (SampleDemo, historical CsvFile).
  Selecting the live MT5 source is **blocked** (C-3, D5-2).
- **INV-5** No invented commentary. The symbol-match confidence shown is the real
  `GoldSymbolResolver` score (0..1), labelled as a detection/match confidence — never a
  probability of profit and never a `%`.

## Scope decisions confirmed for this slice (binding)
- **D5-1 Wizard = a linear, validated step machine** rendered in the existing WPF shell:
  `Welcome → DataSource → Symbol → Connection → Account → Review`. Each step validates
  before the user may advance; **Finish** is enabled only when every step is valid. All
  step, validation and persistence logic lives in testable (net8.0) layers; the XAML
  only binds.
- **D5-2 Live MT5 data source is surfaced but BLOCKED.** The data-source step lists the
  MT5-live option so the user understands it exists, but **selecting it is a validation
  failure** carrying the C-3 named-approver reason. This surfaces the live seam honestly
  and structurally without opening it. Only **SampleDemo** and historical **CsvFile**
  (both non-live, `IsLive=false`) are selectable.
- **D5-3 The wizard collects NO secret.** Because this slice performs no connection,
  it needs no bridge token / password / OTP, and it collects none. `SetupProfile`
  carries only non-secret configuration (a logical `CredentialKey` string at most —
  the *name* of a future credential, never its value). Secret capture belongs to the
  future C-3 connect flow via the existing DPAPI `ICredentialStore` seam, which this
  slice does not build. This makes "no secret in config" both structural (no secret
  member, reflection-proven) and behavioural (the wizard never touches a secret).
- **D5-4 No new heavyweight dependency.** Persistence uses in-framework
  `System.Text.Json`; no charting/DI/serialization NuGet package is added. The
  connection step reuses the existing `BridgeEndpoint` loopback invariant and
  `GoldSymbolResolver`; **zero new NuGet packages**.
- **D5-5 The connection step captures settings but attempts no connection.** Endpoint
  host/port are validated **loopback-only** (reusing `BridgeEndpoint`) and Mode B
  captures a non-secret account login + server name — for *later* use. No
  `IMt5BridgeClient` is constructed and no connect is performed in this slice.

## Requirements (this slice)

| ID | Requirement | Acceptance criteria |
|----|-------------|---------------------|
| **FR-28** | A guided first-run setup wizard that captures & validates non-secret configuration, persists it durably, and gates the dashboard on first launch. No live connection; no secret stored; no order surface. | AC-28.1 Linear step machine `Welcome→DataSource→Symbol→Connection→Account→Review`; `NextCommand` advances only when the current step is valid; `BackCommand` allowed on every step except the first; `CurrentError` exposes the current step's validation message. AC-28.2 DataSource: `SampleDemo` and `CsvFile` (non-empty path required) are selectable; selecting `Mt5Live` is **invalid** with the exact C-3 reason `"Live MT5 data requires named-approver authorization (C-3) and is not enabled in this build."`. AC-28.3 Symbol step ranks a supplied broker-symbol list via `GoldSymbolResolver`; an empty or non-gold selection is invalid; a valid gold selection carries its real detection confidence (0..1, labelled match confidence, never a `%`). AC-28.4 Connection step validates the endpoint as **loopback-only** (`BridgeEndpoint`) with port 1..65535; Mode B additionally requires a non-secret account login + server name, Mode A requires neither; **no connection is attempted**. AC-28.5 Account step requires `AccountBalance > 0`. AC-28.6 `FinishCommand` is enabled only when **all** steps are valid; it builds an immutable `SetupProfile`, persists it via `ISetupProfileStore.Save`, and raises `Completed` so the shell closes. AC-28.7 First-run gate: `NeedsSetup` is true when no profile is stored, false once one is saved. |
| **NFR-SETUP-1** | Secure by construction: no secret ever enters the profile or the wizard. | `SetupProfile` carries no member whose name contains `password/passwd/pwd/secret/investor/pin/token/apikey/otp` (reflection-proven, extending the existing `Mt5ConnectionOptions` guard). The wizard exposes no secret-valued input. `CredentialKey` holds only a logical key name (non-secret). |
| **NFR-SETUP-2** | No execution and no live seam introduced. | Reflection over the wizard VM: no member name contains `open/close/buy/sell/execute/submit/order/trade/place` (same guard family as the Cycle-4 notifier), and it holds no `IMt5BridgeClient` field/property. Grep over the new Presentation/Application/Wpf files: `OrderSend/order_send/PlaceOrder/ExecuteTrade/Buy(/Sell(` = 0. No connect call exists. |
| **NFR-SETUP-3** | Durability + testable-VM / dumb-XAML split; no new dependency. | `SetupProfile`, `DataSourceKind`, `ISetupProfileStore` + `InMemorySetupProfileStore` live in Application (net8.0); `JsonFileSetupProfileStore` in Infrastructure; `SetupWizardViewModel` + validation in the net8.0 `Presentation` project referenced by the existing net8.0 test project — all exercised by `dotnet test` headlessly. **Durability is proven with a fresh store instance** on the same file reading back exact values (incl. exact `decimal` balance). The `Wpf` shell binds only (a hidden-header `TabControl` on `CurrentStepIndex` + bool bindings; no custom value converters). No new `PackageReference` (verified in csproj diff); `System.Text.Json` is in-framework. |

## Out of scope (this slice)
- Any live data path, live MT5 attach, or "test connection" against a real terminal
  (**C-3** — named-approver gate, untouched). The Connection step captures & validates
  settings only.
- Secret/credential capture of any kind (D5-3) — deferred to the future C-3 connect flow.
- FR-34 (packaging/installer), OS-level toast, real-time auto-refresh, paper-execution.
- Editing an existing profile from within the running dashboard (a Settings screen) —
  this slice covers the **first-run** wizard + gate only. Re-running setup is achieved by
  removing the profile file; an in-app settings editor is a later slice.
- Localisation / theming of the wizard.

## `[NEEDS CLARIFICATION]`
None outstanding. "Setup wizard" is pinned concretely above (D5-1…D5-5) from the FR-28
line in the source spec and the Cycle-4 CEO next-slice list. The read-only /
no-secret / no-connection / no-execution framing is unchanged and unambiguous; the
one genuinely forward-looking choice (surface-but-block the live source rather than hide
it) is documented as D5-2 for the CEO gate rather than assumed silently.
</content>
</invoke>
