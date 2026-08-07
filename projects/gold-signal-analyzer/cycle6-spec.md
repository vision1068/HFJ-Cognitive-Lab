# Cycle 6 Spec — Gold Signal Analyzer: Packaging / Distributable Build (FR-34)

**Project:** gold-signal-analyzer
**Cycle / slice:** Produce a **self-contained, distributable Windows build** of
`GoldSignalAnalyzer.Wpf` that a non-developer can run **without installing the .NET SDK,
Visual Studio, or the .NET Desktop Runtime** on their machine — the packaging step that
turns the now-configurable app (Cycle 5 first-run wizard) into something the owner can
hand to a real user.
**Branch / worktree:** `agents/mt5-connectivity-bridge-gold-signal`
**Date:** 2026-07-31
**Owner:** vision1068 (human)

> Extends Cycle 1 (`brief.md`, read-only MT5 bridge scaffold), Cycle 2
> (`cycle2-spec.md`, analytical core + durable journal), Cycle 3 (`cycle3-spec.md`, UI
> shell + disclaimer gate), Cycle 4 (`cycle4-spec.md`, charts + notifications) and
> Cycle 5 (`cycle5-spec.md`, first-run setup wizard). Same independent-build /
> no-live-connectivity / no-order-execution constraints apply verbatim (S-1…S-3,
> INV-1…INV-5). This slice adds **build/packaging only** — it changes *how the exact same
> tested application is produced and distributed*, not *what it does*. It introduces **no
> new runtime behaviour, no live path, no secret, and no order/execution surface of any
> kind**. It stays firmly inside the existing read-only / paper / educational envelope;
> the live seam (**C-3**) is untouched.

## Why this slice exists (business framing — CEO Phase 1)

Every prior cycle produced an app that only a developer with the .NET 8 SDK could run
(`dotnet build` / `dotnet run` from source, or the `bin/…` output that assumes a
globally-installed runtime). The Cycle-5 sign-off (`phase-6-ceo-cycle5.md`) named
**FR-34 packaging** as the well-motivated next slice: *"with a first-run wizard in place,
packaging is the natural next step to ship a configurable app to a non-developer … Stays
inside the read-only envelope."* The owner chose FR-34 explicitly over an in-app Settings
editor and real-time auto-refresh (the latter edges toward the C-3 live-data path). The
concrete deliverable is a build the owner can zip and give to a non-developer who then
**double-clicks one executable** — no SDK, no runtime install, no admin rights — and gets
the same disclaimer → wizard → dashboard flow that Cycle 5 verified by hand.

Packaging is also where a specific, already-recorded risk bites hardest: a clean
`dotnet publish` succeeding is **not** proof the packaged artifact runs. The
2026-07-31 lessons-learned entry (headless xUnit green ≠ the exe actually runs) is
directly load-bearing here — this cycle's acceptance is defined around *actually launching
the published exe from a wiped profile*, not around a publish exit code.

## Permanent invariants (re-asserted verbatim, verified this phase)
- **INV-1** No order execution/modification/closing — packaging adds **no** affordance
  that could place, simulate, or arm an order. No trade/execute command exists in the
  published build; the build process introduces none.
- **INV-2 / INV-3** No stored PA/email/OTP/trading-password/withdrawal credential; no
  scraping. **The published artifact bundles NO secret** — no token, password, or key is
  embedded in the packaged output (see D6-6, NFR-PKG-5). Data still lives per-user in
  `%LOCALAPPDATA%`, written at runtime, never shipped inside the package.
- **INV-4** No fabricated market data presented as live. Packaging changes nothing about
  the data path: the only selectable sources remain non-live (SampleDemo, historical
  CsvFile); the live MT5 source stays **blocked** (C-3, D5-2), and — critically for this
  cycle — that block is **not** conditionally compiled, so the Release/published build
  behaves identically to the tested Debug build (NFR-PKG-4).
- **INV-5** No invented commentary. Unchanged — packaging touches no analysis or display
  logic; the symbol-match confidence remains the real `GoldSymbolResolver` score, never a `%`.

## Standing conditions (re-asserted)
- **C-1** ✅ CLOSED (Cycle 3 — disclaimer gate + always-visible banner). Must remain intact
  and functional **in the packaged build**, proven by the manual launch (AC-34.3).
- **C-2** ✅ CLOSED (Cycle 2 — durable SQLite journal). The packaged self-contained build
  must ship its own SQLite native library so the journal still works with no machine-wide
  runtime (D6-1, AC-34.2).
- **C-3** (Live seam): live MT5 attach and ANY order capability remain **OUT** — enabling
  them requires the named-approver gate, unchanged. This slice introduces no surface that
  touches it; packaging must not silently re-enable, relax, or bundle anything on this seam.

## Scope decisions confirmed for this slice (binding)
- **D6-1 Self-contained, not framework-dependent.** The target audience is a non-developer,
  so the packaged build embeds its own .NET 8 + WPF runtime and the native SQLite library;
  the user needs **no** .NET install. (Framework-dependent — smaller, but requires the user
  to install the .NET 8 Desktop Runtime first — is the rejected alternative; documented as an
  architecture Fork in `phase-2-arch-cycle6.md`.)
- **D6-2 Distribution form = single self-extracting `.exe`, `win-x64`.** The owner hands out
  one file. Native libraries that cannot load in-place (WPF native + `e_sqlite3`) are embedded
  via `IncludeNativeLibrariesForSelfExtract` and self-extracted at runtime. (A self-contained
  *folder* the owner zips is the rejected-but-viable fallback; the choice is made on real
  publish evidence and documented as a Fork.)
- **D6-3 No installer / no installer toolchain.** This cycle ships a **publish-ready
  self-contained exe**, not an MSI/MSIX/Setup bundle. No WiX / Squirrel / MSIX / Inno Setup
  / ClickOnce dependency is added — that would be the first cycle to break the standing
  "zero new heavyweight dependency" discipline (NFR-SETUP-3 family). Documented as a Fork
  with the rejected installer alternative. A real installer is a deliberately deferred later
  slice if/when the owner wants Start-menu integration or code-signing.
- **D6-4 No trimming, no ReadyToRun (R2R) in this slice.** WPF is not officially trim-safe
  (XAML/reflection), and SQLitePCLRaw + `System.Text.Json` use reflection — trimming risks a
  runtime break that a publish exit code would not reveal. R2R is declined too (needs the
  crossgen2 pack and buys only startup time, not required by any AC). Both are documented
  Fork rejections, not silent omissions.
- **D6-5 Per-user, no elevation.** The packaged exe runs from any folder with no admin
  rights, no `requireAdministrator` manifest, and no registry writes; all state stays in the
  existing per-user `%LOCALAPPDATA%\GoldSignalAnalyzer` directory (unchanged from prior cycles).
- **D6-6 The package bundles no secret and phones home to nothing.** The published output
  contains no credential/token/key and no auto-updater; on launch it makes no network call
  (the loopback bridge seam is present but never invoked in this read-only build). "No secret
  in the package" is proven by scanning the actual publish folder, not asserted.
- **D6-7 Reproducible via a checked-in publish profile + one-command script.** Packaging is
  driven by a `.pubxml` publish profile under the Wpf project plus a thin `publish.ps1`
  wrapper, so the owner reproduces the exact artifact with a single command; no ad-hoc CLI
  flags to remember.

## Requirements (this slice)

| ID | Requirement | Acceptance criteria |
|----|-------------|---------------------|
| **FR-34** | Produce a self-contained, distributable Windows build of `GoldSignalAnalyzer.Wpf` that a non-developer can run without a .NET SDK/runtime install, driven by a reproducible, checked-in publish profile — introducing no new runtime behaviour, no live path, no secret, and no order/execution surface. | **AC-34.1** `dotnet publish` of the Wpf project with the packaging profile (self-contained, `win-x64`, Release) completes with **exit code 0** and writes the artifact to a known, documented output path. **AC-34.2** The output is **genuinely self-contained** — the artifact carries its own .NET/WPF runtime (CoreCLR + WPF assemblies) and the native SQLite library (`e_sqlite3`), verified by inspecting the **actual** file(s)/size, not inferred from the publish flags; it does **not** silently depend on a machine-wide shared framework. **AC-34.3** The **published** exe (not `bin/Debug`), launched from a **wiped** `%LOCALAPPDATA%\GoldSignalAnalyzer` profile, completes **disclaimer → wizard → dashboard** correctly, and a **relaunch** skips both gates (profile persists) — proven by driving the real exe via Windows UI Automation, per the standing lessons-learned rule. **AC-34.4** The packaged exe requires **no elevation** — no `requireAdministrator` manifest, no admin-only path, no registry writes; all state stays in per-user `%LOCALAPPDATA%`. **AC-34.5** **No secret/credential is bundled** in the published output (scan of the actual publish folder for `password/passwd/pwd/secret/investor/pin/token/apikey/otp` = 0 real hits). **AC-34.6** The packaged **Release** build enforces the **same safety envelope** as the tested build: `Mt5Live` still blocked with the exact C-3 reason, **no** order/execution surface, and **`GoldSignalAnalyzer.Testing.dll` absent** from the publish output (`deps.json` reference count = 0) — and this parity is structural (no `#if DEBUG`/`[Conditional]` divergence anywhere in the source). **AC-34.7** Packaging adds **no** new `PackageReference` and **no** installer toolchain; the normal `dotnet build` (Debug) dev path and the full `dotnet test` suite still pass unchanged (200/0). |
| **NFR-PKG-1** | No prerequisite runtime on the target machine. | The published build is self-contained; on a machine with **no** .NET SDK/runtime on `PATH` it still runs. Evidenced by the self-contained output containing its own `coreclr.dll` / `Microsoft.WindowsDesktop.App` assemblies (AC-34.2) and by the launch succeeding from a shell whose `PATH` does not surface a shared runtime. |
| **NFR-PKG-2** | No silent auto-update and no phone-home. | The package contains no updater component and initiates no outbound network call on launch (the loopback bridge client is never constructed in this read-only shell — the composition root builds only the sample-data analysis path). Grep of the publish output + composition root confirms no updater/HTTP-egress code path is triggered at startup. |
| **NFR-PKG-3** | No elevation / per-user only. | No `app.manifest` requesting elevation is added; no write outside `%LOCALAPPDATA%`; no registry key. The exe runs from a normal user folder (AC-34.4). |
| **NFR-PKG-4** | Safety parity between the tested (Debug) build and the shipped (Release/published) build. | There is **no** conditional compilation (`#if DEBUG` / `#elif` / `[Conditional]`) anywhere in the C# source (grep = 0), so the safety-relevant logic (Mt5Live block, no-order guards) compiles identically in every configuration. The `Testing` assembly is excluded by the **assembly graph** (Wpf does not reference it), which is configuration-independent. Re-verified on the published output (AC-34.6). |
| **NFR-PKG-5** | Zero new heavyweight dependency. | No installer framework (WiX/MSIX/Squirrel/Inno/ClickOnce) and no new `PackageReference` are added to any project; packaging uses only the .NET SDK's built-in `dotnet publish`. Verified by a csproj diff (only the Wpf packaging properties + a `.pubxml` + a `publish.ps1` are added; no `<PackageReference>` added anywhere). |
| **NFR-PKG-6** | Reproducible, one-command packaging. | A checked-in publish profile (`Properties/PublishProfiles/*.pubxml`) and a `publish.ps1` wrapper let the owner reproduce the exact artifact with a single command; the profile pins the RID, configuration, self-contained + single-file settings so the output is not dependent on remembered CLI flags. |

## Out of scope (this slice)
- Any live data path, live MT5 attach, or "test connection" against a real terminal
  (**C-3** — named-approver gate, untouched). Packaging ships the same read-only build.
- A real **installer** (MSI/MSIX/Setup bundle), Start-menu/Desktop shortcut creation, file
  associations, or auto-update (all deferred; D6-3). Owner distributes the exe directly (e.g. zipped).
- **Code-signing** / Authenticode certificate (the owner has no certificate in scope this
  cycle; documented as a Fork with a recommended default of *ship unsigned now, sign later
  if distribution widens* — SmartScreen implications noted in the architecture phase).
- **Trimming** and **ReadyToRun** (D6-4) — declined this slice for WPF trim-safety + no need.
- Multi-RID (`win-arm64`, `win-x86`) builds — `win-x64` only this slice.
- Secret/credential capture, in-app Settings editor, real-time auto-refresh, OS-level toast,
  any paper-execution action surface — all unchanged from Cycle 5's out-of-scope list.
- Any change to analysis, scoring, charting, journaling, or wizard **behaviour** — this slice
  is packaging only and ships the identical tested application.

## `[NEEDS CLARIFICATION]`
None blocking. FR-34 "packaging" is pinned concretely above (D6-1…D6-7) from the FR-34 line
in the Cycle-4/5 next-slice lists and the owner's explicit selection of packaging for this
cycle. The two genuine forks with real tradeoffs a human might otherwise weigh —
(a) **installer vs publish-ready exe** and (b) **code-signing now vs later** — are resolved
here toward the simplest safe default (publish-ready **unsigned** self-contained exe, no
installer toolchain) and documented as **Forks with recommended defaults** in
`phase-2-arch-cycle6.md`, since the owner has delegated the packaging-technology decision to
this engagement. If the owner later wants Start-menu integration, silent updates, or to clear
SmartScreen warnings for wide distribution, that is a follow-on slice (installer + code-signing)
called out in the CEO next-slice options.
