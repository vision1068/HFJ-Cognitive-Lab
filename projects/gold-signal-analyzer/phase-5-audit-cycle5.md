# Cycle 5 — Audit Phase (First-Run Setup Wizard, FR-28)

**Project:** gold-signal-analyzer · **Date:** 2026-07-31 · **Reviewer:** auditor role
**Scope reviewed:** FR-28 (first-run setup wizard + gate) added to the read-only WPF shell.
Governance question: does capturing configuration change the risk posture or touch the live seam?

## Verdict: **PASS** — still read-only / config-capture-only; C-3 boundary untouched.

Capturing and persisting **non-secret** configuration does not move the product across any
regulated or irreversible line. The wizard performs no live connection, collects no secret,
and adds no order affordance. The live MT5 source is surfaced but structurally blocked.

## Invariant re-verification (evidence, not assertion)
| Invariant | How re-checked this cycle | Result |
|-----------|---------------------------|--------|
| **INV-1** no order/execution surface | grep `OrderSend\|order_send\|PlaceOrder\|ExecuteTrade\|SubmitOrder\|.Buy(\|.Sell(\|IMt5BridgeClient` over new production files → 1 doc-comment false-positive (0 real); reflection test: `SetupWizardViewModel` has no member containing open/close/buy/sell/execute/submit/order/trade/place | **0 / pass** |
| **INV-2 / INV-3** no secret, no scraping | wizard collects NO secret (D5-3); no HTTP/network egress; **0 `PackageReference`** added (csproj untouched) | **pass** |
| **INV-4** no fabricated "live" data | only non-live sources selectable (`SampleDemo`/`CsvFile`, `IsLive=false`); `Mt5Live` selection is a validation failure (D5-2) — the live source is structurally blocked, not merely documented | **pass** |
| **INV-5** confidence traceable, never a % | symbol match confidence is the real `GoldSymbolResolver` 0..1 score shown as `match confidence 0.90`; grep `%`/`probability` = 3 negative-assertion hits only | **pass** |
| **C-1** disclaimer intact | setup gate inserted **before** the dashboard build; always-visible banner `Border` + first-run acknowledgement gate unchanged; banner test green within 200 | **pass** |
| **C-3** live/order boundary | no live-attach, no `IMt5BridgeClient` constructed, no connect attempted (D5-5); `Mt5Live` surfaced-but-blocked with the named-approver reason | **untouched** |
| **NEW — no secret in persisted `SetupProfile`** | reflection guard extended (`ProductionGatingTests` `[Theory]` now covers `SetupProfile`, forbidden list incl. `otp`); persisted JSON scanned for `password/passwd/secret/investor/token/apikey/otp` = 0 | **pass** |
| **NEW — Testing.dll absent from Release / no package drift** | `ls` of Wpf Release output shows no `Testing.dll`; `deps.json` "GoldSignalAnalyzer.Testing" refs = 0; PackageReference diff over Presentation/Wpf/Tests = 0 | **pass** |

## Governance notes
- **Surface-but-block the live source was the right call (D5-2).** Listing `Mt5Live` and failing
  its selection with the exact C-3 named-approver reason is more honest than hiding it, and it
  keeps the live seam visible to the user and the auditor without opening it.
- **Zero secret is both structural and behavioural.** `SetupProfile` has no secret member
  (reflection-proven) and the persisted file is proven secret-free; `CredentialKey` holds only a
  logical *name*. Real secret capture is correctly deferred to the future C-3 connect flow via the
  existing, untouched DPAPI `ICredentialStore` seam.
- **No new data leaves the machine.** The wizard reads a static config symbol list (names, not
  prices) and writes a local JSON file; no network egress, no new dependency.
- **Placement deviation is documented, not silent.** The store lives in `Presentation` (Fork 1),
  superseding NFR-SETUP-3's literal Application/Infrastructure sketch; every NFR-SETUP-3 acceptance
  is still met. This is an implementation-placement choice within the NFR, not a constitutional change.

## Conditions
- No new conditions. **C-3 remains the only open condition**, unchanged and untouched.
