# Gold Signal Analyzer — Installation & User Guide

**What this app is:** an advisory-only gold (XAU/USD) signal dashboard with a paper-trading
journal. It can run on sample data, a historical CSV, or a **live, read-only** feed from your
own MetaTrader 5 terminal. It **never places, modifies, or closes a real order** — there is no
order/execution code path anywhere in the app (this is a permanent, structural guarantee, not a
setting).

---

## 1. What you need

| Component | Required for | Notes |
|---|---|---|
| **GoldSignalAnalyzer.Wpf.exe** | Always | The app itself. Self-contained — no .NET install needed. |
| **MetaTrader 5 terminal** | Live MT5 mode only | Installed, open, and logged into a broker account (demo recommended first). |
| **Python 3.9+** | Live MT5 mode only | To run the local bridge script that reads your MT5 terminal. |
| **`MetaTrader5` Python package** | Live MT5 mode only | `pip install MetaTrader5` (Windows only). |

If you only want to try the app with **sample data**, you need nothing but the `.exe` — skip
straight to Section 3.

---

## 2. Installing the app

There is no installer (no MSI/setup wizard) — the app is a single self-extracting `.exe`,
by design (no admin rights needed, nothing written outside your own user profile):

1. Copy `GoldSignalAnalyzer.Wpf.exe` anywhere on your laptop (Desktop, a folder — anywhere you like).
2. Double-click it to run.
3. Windows SmartScreen may show an "unrecognized app" warning the first time (the exe isn't
   code-signed yet) — click **More info → Run anyway**.

The app stores all its data under `%LOCALAPPDATA%\GoldSignalAnalyzer\` (disclaimer
acknowledgement, your setup profile, the paper-trading journal database, logs, and — in live
mode — an audit trail of every live signal shown). Nothing is written anywhere else; no
elevation, no registry changes.

**To uninstall:** delete the `.exe` and, if you want to remove your saved settings/journal too,
delete `%LOCALAPPDATA%\GoldSignalAnalyzer\`.

---

## 3. First run — disclaimer and setup wizard

### Step 1 — Disclaimer
On first launch you'll see a disclaimer window. Read it and click **"I have read and
understood — Continue."** This only appears once; it's remembered for future launches.

### Step 2 — Setup wizard (6 steps)
This also only runs once (delete `setup.json` from the data folder above to redo it).

1. **Welcome** — click Next.
2. **Data Source** — pick one:
   - **Sample / demo data** — instant, no MT5 needed. Good for a first look.
   - **Historical CSV file** — point it at a CSV of candle data.
   - **Live MT5 terminal (read-only)** — see Section 4 below; needs the Python bridge running
     *before* you finish this wizard, or the dashboard will just show "not ready" until it is.
3. **Gold Symbol** — pick your broker's gold instrument from the list. **Important:** many
   brokers add a suffix (e.g. Exness uses `XAUUSDm`, not plain `XAUUSD`). If you're using live
   mode, check the exact name in your MT5 **Market Watch** panel and pick the matching entry —
   a mismatch here is the #1 cause of "DATA STALE" in live mode (see Section 6).
4. **Connection** (only meaningful for live mode):
   - Leave **Mode A / attach existing session** unchecked (recommended) — this uses your
     already-logged-in terminal and needs no login/password.
   - **Port: enter `9001`** — this must match the bridge's port (Section 4 uses the default,
     9001). The wizard's own default here is `8080`, which will **not** match the bridge unless
     you change it — always set this to `9001` unless you deliberately changed the bridge's port too.
   - Leave Host as `127.0.0.1` (loopback only — the app will refuse anything else).
   - **Security note:** the "Credential key name" field is for a *label*, not a password — do
     not type a real broker password into it. It is **not required for either mode** (Mode A
     needs nothing here at all; Mode B still never transmits this field anywhere — it's stored
     locally as plain text for your own reference only).
5. **Account** — enter your account balance (used only to size the paper-trading risk plan;
   never sent anywhere).
6. **Review** — check the summary, click **Finish**.

The dashboard opens after Finish.

---

## 4. Setting up live MT5 mode (the local bridge)

Live mode works by running a small local Python program (the "bridge") that reads prices from
your MT5 terminal and serves them to the app over `localhost` only — nothing ever leaves your
machine, and the bridge itself has **no ability to place trades** (it only reads).

### One-time setup
```powershell
pip install MetaTrader5
```

### Every time you want live data
1. **Open MetaTrader 5 and log into your broker account** (demo account recommended for
   testing). Leave it running.
2. Open PowerShell in the app's `src/bridge` folder and set a token (any strong random string —
   this is a local password that stops other programs on your PC from reading your MT5 data
   through the bridge; it is never a broker password):
   ```powershell
   $env:GSA_BRIDGE_TOKEN = "choose-a-long-random-string-here"
   python run_bridge.py --live
   ```
   Leave this window open — it's now listening on `http://127.0.0.1:9001` (loopback only).
3. **In the same PowerShell session** (so it inherits the same token), launch the app:
   ```powershell
   & "C:\path\to\GoldSignalAnalyzer.Wpf.exe"
   ```
   The app reads `GSA_BRIDGE_TOKEN` from its environment at startup — if you launch it from a
   *different* window/shortcut that doesn't have this variable set, live mode will show
   "GSA_BRIDGE_TOKEN is not set" and pause.
4. In the dashboard's setup (or if you already finished setup, it connects automatically),
   confirm Data Source = Live MT5, port = `9001`, symbol = your broker's exact gold symbol name.

To make the token persist across PowerShell sessions instead of retyping it every time:
```powershell
[System.Environment]::SetEnvironmentVariable("GSA_BRIDGE_TOKEN", "your-token", "User")
```
(open a new PowerShell window afterward for it to take effect for both the bridge and the app).

---

## 5. Using the dashboard

- **Live feed banner** (live mode only) — shows one of:
  - `LIVE — feed connected and fresh` — everything's working; a real signal may be showing.
  - `CONNECTION NOT READY` — bridge isn't reachable (not started, wrong port, or terminal not logged in).
  - `DATA STALE` — connected, but the symbol name likely doesn't match your broker's real
    symbol, or MT5 hasn't produced a fresh tick recently.
  - The app **never invents a price or a signal** — if it can't confirm the data is real and
    fresh, it shows nothing rather than guessing.
- **Current Signal panel** — Direction (Buy/Sell/Neutral), regime, buy/sell scores (0–100,
  never a probability), confidence, and a proposed (paper-only) risk plan with the deterministic
  reasoning behind it.
- **Price Chart** — candles with EMA overlays.
- **Signal Notifications** — pops up when a signal becomes actionable.
- **Paper-Trading Journal** — every simulated fill (entry/exit, size, R:R, P&L). This is
  **never a real trade** — there is no code path anywhere in the app that can place, modify, or
  close a real broker order, live mode or not.
- **Live signal audit log** (live mode only) — every refresh, allowed or suppressed, is
  appended to `%LOCALAPPDATA%\GoldSignalAnalyzer\live-signal-audit.jsonl` — a durable record of
  what the app showed you and when.

---

## 6. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| "GSA_BRIDGE_TOKEN is not set" | App launched from a shell/shortcut without the env var | Set the token in the same session/shortcut that launches the exe (Section 4, step 3). |
| "CONNECTION NOT READY" | Bridge not running, wrong port, or MT5 not logged in | Confirm the bridge PowerShell window is still open with no errors; confirm MT5 is open and logged in; confirm the wizard's port is `9001`. |
| "DATA STALE" | Symbol name doesn't match your broker's real symbol | Open MT5's Market Watch, find the exact gold symbol name (check for a suffix like `m`, `.pro`, etc.), redo the wizard's Symbol step with the matching entry (delete `setup.json` to re-run the wizard). |
| Windows SmartScreen warning on launch | Exe isn't code-signed | Click "More info → Run anyway" — this is expected until the app is signed. |
| Want to start over completely | — | Close the app, delete `%LOCALAPPDATA%\GoldSignalAnalyzer\`, relaunch. |

---

## 7. What this app will never do

- Place, modify, or close a real order on your broker account (no code path exists for this —
  it's a permanent design decision, not a setting you could accidentally enable).
- Store your broker password or send any credential over the network (Mode A needs none at
  all; the "credential key" field, if you use it, stays local and unused by the connection logic).
- Fabricate a price or a signal when it can't verify the data is real and fresh — it pauses
  and tells you why instead.
