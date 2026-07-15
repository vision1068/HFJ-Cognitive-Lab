# Company Memory — Lessons Learned

Append-only. After every engagement (Phase 7 retrospective) the
orchestrator adds an entry here; at the start of every new engagement
it reads this file first. Pattern from metaswarm's post-merge
reflection (github.com/dsifry/metaswarm) and rohitg00/pro-workflow's
compounding memory.

Entry format:

### YYYY-MM-DD — <project> — <one-line title>
- **What happened:**
- **Lesson:**
- **Rule going forward:**

---

### 2026-06-22 — doh-flight-checker — .gitignore missed .env exclusion
- **What happened:** The project scaffold didn't exclude `.env`; caught later during the Market Compass security pass rather than at creation time.
- **Lesson:** Scaffolding steps silently skip security defaults unless checked explicitly.
- **Rule going forward:** Every new project's first commit must include a `.gitignore` that covers `.env`/`.env.local`/credentials — verified, not assumed (now in secure-coding.md checklist).

### 2026-07-02 — market-compass — GitHub Pages first deploy required manual settings
- **What happened:** Deploy failed 3× at `deployment_queued`: Pages source and Actions write-permissions had to be set manually in repo Settings before any workflow-based deploy could succeed; `enablement: true` was rejected ("Resource not accessible by integration").
- **Lesson:** First-ever Pages deployment on a repo is an account-settings problem, not a workflow problem — retrying the pipeline cannot fix it.
- **Rule going forward:** Before the first Pages deploy on any repo, confirm with the user: Settings → Pages → Source = GitHub Actions, and Settings → Actions → Workflow permissions = Read and write. Escalate to the user after 2 failed identical attempts (codex-rescuer rule 5).

### 2026-07-10 — market-compass — parallel sessions on one branch cause push rejections
- **What happened:** Multiple sessions pushed to `claude/jolly-newton-5uctey` simultaneously; pushes were rejected and required fetch+merge before every push, and one rerun failed on a stale duplicate artifact.
- **Lesson:** Shared-branch parallel work needs fetch-merge-push discipline every time, and reruns of partially-failed workflows can collide with stale artifacts — a fresh run is safer.
- **Rule going forward:** Always `git fetch` + merge before push; prefer a new triggering commit over rerunning a failed workflow when artifacts may be stale.

### 2026-07-10 — auto-captured — PSX API rejects requests without a browser User-Agent header
- **Lesson:** PSX API rejects requests without a browser User-Agent header — always set one on server-side fetches.
- **Source:** [LEARN] block auto-captured by learn-capture hook

### 2026-07-15 — market-compass — Predic panel: the data-source gate is the whole engagement, not a detail
- **What happened:** A "build a 17-section equity-research report panel" feature-add turned entirely on one reconnaissance finding — the app is PSX-Data-Portal-only and no free source publishes PSX financial statements, so ~11 of 17 sections have NO real data and could only be filled by fabricating figures (Constitution-forbidden). Routed as a scoped feature-add (not fresh Pattern A): architect data-source gate → adversarial plan-review (QA + auditor, both PASS-WITH-CONDITIONS) → user resolved the fork (PSX-only, honest-and-thin, keep verdict + add disclaimer) → backend engine + frontend UI (sequenced on one branch) → QA verify. Shipped a fork-agnostic engine whose `SourcedFigure` discriminated union + `guard()` makes fabrication structurally impossible; unavailable sections render honest N/A. 38 Predic tests green, tsc 0, build 0. Auditor caught that a numeric BUY/SELL verdict crosses into regulated-investment-advice territory and that `VITE_`-prefixed keys leak into the client bundle.
- **Lesson:** For any "generate an analyst/report/insight" feature, verify the real data supply BEFORE any UI/spec work — the honest answer may be "this cannot be built as asked," and surfacing that fork is the deliverable. Enforce no-invented-data in the TYPE SYSTEM (a value that structurally cannot exist without {source, asOf}), not in review. A rendered verdict/score on real securities is a governance surface (disclaimer adjacent to the verdict, keys server-side only), independent of the data gap. Also: full-suite runs surface pre-existing red tests — always separate "did we regress" (check `git diff --name-only` on the failing file) from "was it already broken."
- **Rule going forward:** Feature requests that produce numbers/verdicts must (1) pass a data-source gate first with an explicit sourceable-vs-blocked matrix, (2) route every figure through a guard constructor so fabrication is a compile error, never a placeholder, and (3) get an auditor pass whenever output could read as advice or a record. `VITE_`-prefixed env vars are client-public by construction — never a secret. Pre-existing test failures on an untouched file are not a regression; prove it with the diff before claiming or denying a green suite.

### 2026-07-10 — catch-the-falling-stars — conditional module.exports makes browser games unit-testable
- **What happened:** Full 6-phase engagement on a zero-dependency canvas game. Structuring game.js as pure-logic → engine → DOM adapter, with `if (typeof module !== 'undefined') module.exports = {...}`, let Node run 21 real assertions (collision, difficulty, scoring, storage sanitization, end-of-round precedence) against the exact shipped file — no browser needed for the QA gate. The adversarial Plan Review Gate caught 8 timing/state edge cases (pause-drift, timer-vs-lives race, blur-from-non-PLAYING) before a line of code existed.
- **Lesson:** Layer browser code so its pure logic is requireable, and run the plan through adversarial review first — both turn "should work" into command-output evidence cheaply. Also: security greps need manual triage (comments and `content=` attributes false-positive on innerHTML/on*= patterns).
- **Rule going forward:** Every browser-JS deliverable exposes its pure logic via conditional module.exports and ships with a Node harness executed in Phase 4; grep-based security scans must show the matched lines, not just counts.
