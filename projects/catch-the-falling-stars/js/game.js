/* Catch the Falling Stars
 * Layering: (1) pure logic — exported for Node tests; (2) engine; (3) DOM/canvas adapter.
 * No frameworks, no network, no eval, no innerHTML with dynamic data.
 */
"use strict";

/* ========== 1. PURE LOGIC (unit-testable, no DOM) ========== */

/** AABB overlap test. Rects are {x, y, w, h}. */
function rectsOverlap(a, b) {
  return a.x < b.x + b.w && a.x + a.w > b.x &&
         a.y < b.y + b.h && a.y + a.h > b.y;
}

/** Difficulty curve (FR-7): linear, capped. px/second. */
function fallSpeedForScore(score) {
  return Math.min(140 + 9 * score, 460);
}

/** Spawn interval in ms, tightens with score. */
function spawnIntervalForScore(score) {
  return Math.max(1100 - 18 * score, 450);
}

/** Scoring/penalty for catching an entity (FR-4, FR-10, FR-11 / D-1). */
function applyCatch(kind, score, lives) {
  if (kind === "star")   return { score: score + 1, lives: lives };
  if (kind === "golden") return { score: score + 5, lives: lives };
  if (kind === "bomb")   return { score: score, lives: lives - 1 };
  return { score: score, lives: lives };
}

/** Penalty for an entity leaving the bottom uncaught (FR-5; bombs are free to miss). */
function applyMiss(kind, lives) {
  return kind === "bomb" ? lives : lives - 1;
}

/** Keep basket inside the field. */
function clampBasketX(x, basketW, fieldW) {
  return Math.max(0, Math.min(x, fieldW - basketW));
}

/** Validate a raw localStorage value into a safe non-negative integer (NFR-5). */
function sanitizeHighScore(raw) {
  const n = parseInt(raw, 10);
  return Number.isFinite(n) && n >= 0 ? n : 0;
}

/** End-of-round check (D-2): timer first, then lives (F-2). */
function roundOverReason(timeLeft, lives) {
  if (timeLeft <= 0) return "time";
  if (lives <= 0) return "lives";
  return null;
}

if (typeof module !== "undefined") {
  module.exports = {
    rectsOverlap, fallSpeedForScore, spawnIntervalForScore,
    applyCatch, applyMiss, clampBasketX, sanitizeHighScore, roundOverReason
  };
}

/* ========== 2+3. ENGINE + ADAPTER (browser only) ========== */
if (typeof document !== "undefined") {
(function () {

  const FIELD_W = 560, FIELD_H = 520;
  const BASKET_W = 84, BASKET_H = 30, BASKET_Y = FIELD_H - 42;
  const ENTITY_SIZE = 30, HITBOX_PAD = 4; // forgiving art, honest hitbox
  const ROUND_SECONDS = 60, START_LIVES = 3;
  const KEY_SPEED = 420;           // px/s for arrow keys
  const MAX_DT = 50;               // ms clamp — tunnelling/pause guard (F-1)
  const GOLDEN_P = 0.12, BOMB_P = 0.18;
  const STORAGE_KEY = "ctfs.highScore";

  const STATE = { START: "start", PLAYING: "playing", PAUSED: "paused", GAMEOVER: "gameover" };

  /* --- storage adapter with in-memory fallback (F-4, NFR-5) --- */
  const storage = (function () {
    let memory = 0, persistent = true;
    function read() {
      try { return sanitizeHighScore(localStorage.getItem(STORAGE_KEY)); }
      catch (e) { persistent = false; return memory; }
    }
    function write(v) {
      memory = v;
      try { localStorage.setItem(STORAGE_KEY, String(v)); }
      catch (e) { persistent = false; }
    }
    return { read, write, isPersistent: () => persistent };
  })();

  /* --- DOM refs --- */
  const canvas = document.getElementById("game-canvas");
  const ctx = canvas.getContext("2d");
  const el = (id) => document.getElementById(id);
  const hud = { score: el("hud-score"), lives: el("hud-lives"), timer: el("hud-timer"), high: el("hud-high") };
  const overlays = { start: el("overlay-start"), pause: el("overlay-pause"), gameover: el("overlay-gameover") };
  const btn = {
    start: el("btn-start"), resume: el("btn-resume"), playagain: el("btn-playagain"),
    pause: el("btn-pause"), restart: el("btn-restart")
  };

  /* --- game state --- */
  let state = STATE.START;
  let score = 0, lives = START_LIVES, timeLeft = ROUND_SECONDS;
  let highScore = storage.read();
  let entities = [];               // {kind, x, y}
  let basketX = (FIELD_W - BASKET_W) / 2;
  let spawnTimer = 0;
  let lastFrame = null;
  let keys = { left: false, right: false };
  let activeInput = "keys";        // 'keys' | 'mouse' (F-3 / D-3)
  let mouseTargetX = basketX;
  let stars = [];                  // static background stars

  for (let i = 0; i < 60; i++) {
    stars.push({ x: Math.random() * FIELD_W, y: Math.random() * FIELD_H, r: Math.random() * 1.4 + 0.4, a: Math.random() * 0.5 + 0.3 });
  }

  /* --- state machine --- */
  function setState(next) {
    state = next;
    overlays.start.classList.toggle("hidden", next !== STATE.START);
    overlays.pause.classList.toggle("hidden", next !== STATE.PAUSED);
    overlays.gameover.classList.toggle("hidden", next !== STATE.GAMEOVER);
    btn.pause.disabled = next !== STATE.PLAYING;
    btn.restart.disabled = next === STATE.START;
    // Focus management (F-8)
    if (next === STATE.START) btn.start.focus();
    else if (next === STATE.PAUSED) btn.resume.focus();
    else if (next === STATE.GAMEOVER) btn.playagain.focus();
  }

  /* --- single reset path (F-7) --- */
  function resetRound() {
    score = 0; lives = START_LIVES; timeLeft = ROUND_SECONDS;
    entities = []; spawnTimer = 0;
    basketX = (FIELD_W - BASKET_W) / 2;
    mouseTargetX = basketX;
    keys.left = keys.right = false;
    lastFrame = null;
    updateHud();
  }

  function startRound() { resetRound(); setState(STATE.PLAYING); }

  function endRound(reason) {
    if (state === STATE.GAMEOVER) return; // idempotent (F-2)
    setState(STATE.GAMEOVER);
    el("gameover-reason").textContent =
      reason === "time" ? "⏱ Time's up!" : "♥ Out of lives!";
    el("gameover-score").textContent = "⭐ Final score: " + score;
    const isRecord = score > highScore;
    if (isRecord) { highScore = score; storage.write(highScore); }
    el("gameover-record").classList.toggle("hidden", !isRecord);
    updateHud();
  }

  /* --- HUD --- */
  function updateHud() {
    hud.score.textContent = "⭐ Score: " + score;
    hud.lives.textContent = "♥ Lives: " + lives;
    hud.timer.textContent = "⏱ Time: " + Math.ceil(timeLeft) + "s";
    hud.high.textContent = "🏆 Best: " + highScore;
    el("storage-note").classList.toggle("hidden", storage.isPersistent());
  }

  /* --- spawning --- */
  function spawnEntity() {
    const r = Math.random();
    const kind = r < BOMB_P ? "bomb" : r < BOMB_P + GOLDEN_P ? "golden" : "star";
    entities.push({ kind, x: Math.random() * (FIELD_W - ENTITY_SIZE), y: -ENTITY_SIZE });
  }

  /* --- per-frame update (PLAYING only) --- */
  function update(dt) {
    const dtS = dt / 1000;

    // Timer first (F-2)
    timeLeft -= dtS;
    let reason = roundOverReason(timeLeft, lives);
    if (reason) { timeLeft = Math.max(timeLeft, 0); endRound(reason); return; }

    // Basket movement (D-3): only active input source applies
    if (activeInput === "keys") {
      const dir = (keys.right ? 1 : 0) - (keys.left ? 1 : 0);
      basketX += dir * KEY_SPEED * dtS;
    } else {
      const target = mouseTargetX - BASKET_W / 2;
      basketX += (target - basketX) * Math.min(1, dtS * 14);
    }
    basketX = clampBasketX(basketX, BASKET_W, FIELD_W);

    // Spawning
    spawnTimer += dt;
    if (spawnTimer >= spawnIntervalForScore(score)) { spawnTimer = 0; spawnEntity(); }

    // Entities
    const speed = fallSpeedForScore(score);
    const basketBox = { x: basketX, y: BASKET_Y, w: BASKET_W, h: BASKET_H };
    const kept = [];
    for (const ent of entities) {
      ent.y += speed * dtS;
      const box = { x: ent.x + HITBOX_PAD, y: ent.y + HITBOX_PAD, w: ENTITY_SIZE - 2 * HITBOX_PAD, h: ENTITY_SIZE - 2 * HITBOX_PAD };
      if (rectsOverlap(box, basketBox)) {
        const res = applyCatch(ent.kind, score, lives);
        score = res.score; lives = res.lives;
      } else if (ent.y > FIELD_H) {
        lives = applyMiss(ent.kind, lives);
      } else {
        kept.push(ent);
      }
    }
    entities = kept;

    reason = roundOverReason(timeLeft, lives);
    if (reason) { endRound(reason); return; }
    updateHud();
  }

  /* --- rendering --- */
  function drawStarShape(cx, cy, outer, inner, color) {
    ctx.beginPath();
    for (let i = 0; i < 10; i++) {
      const r = i % 2 === 0 ? outer : inner;
      const a = (Math.PI / 5) * i - Math.PI / 2;
      const x = cx + r * Math.cos(a), y = cy + r * Math.sin(a);
      if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
    }
    ctx.closePath();
    ctx.fillStyle = color;
    ctx.fill();
  }

  function render() {
    ctx.clearRect(0, 0, FIELD_W, FIELD_H);
    // twinkling background
    for (const s of stars) {
      ctx.globalAlpha = s.a;
      ctx.fillStyle = "#cfd9ff";
      ctx.fillRect(s.x, s.y, s.r, s.r);
    }
    ctx.globalAlpha = 1;

    // entities
    for (const ent of entities) {
      const cx = ent.x + ENTITY_SIZE / 2, cy = ent.y + ENTITY_SIZE / 2;
      if (ent.kind === "bomb") {
        ctx.beginPath();
        ctx.arc(cx, cy + 2, 11, 0, Math.PI * 2);
        ctx.fillStyle = "#20242e";
        ctx.fill();
        ctx.strokeStyle = "#ff7861"; ctx.lineWidth = 2; ctx.stroke();
        ctx.beginPath(); // fuse
        ctx.moveTo(cx, cy - 9); ctx.quadraticCurveTo(cx + 6, cy - 15, cx + 9, cy - 12);
        ctx.strokeStyle = "#c9a15a"; ctx.stroke();
        ctx.fillStyle = "#ffd75e"; // spark
        ctx.fillRect(cx + 8, cy - 14, 3, 3);
      } else if (ent.kind === "golden") {
        ctx.shadowColor = "#ffd75e"; ctx.shadowBlur = 14;
        drawStarShape(cx, cy, 14, 6, "#ffd75e");
        ctx.shadowBlur = 0;
      } else {
        drawStarShape(cx, cy, 12, 5, "#eaf0ff");
      }
    }

    // basket
    ctx.fillStyle = "#a8703d";
    ctx.beginPath();
    ctx.moveTo(basketX, BASKET_Y);
    ctx.lineTo(basketX + BASKET_W, BASKET_Y);
    ctx.lineTo(basketX + BASKET_W - 12, BASKET_Y + BASKET_H);
    ctx.lineTo(basketX + 12, BASKET_Y + BASKET_H);
    ctx.closePath();
    ctx.fill();
    ctx.strokeStyle = "#d9a266"; ctx.lineWidth = 2; ctx.stroke();
    ctx.beginPath(); // rim
    ctx.moveTo(basketX, BASKET_Y); ctx.lineTo(basketX + BASKET_W, BASKET_Y);
    ctx.strokeStyle = "#f0c188"; ctx.lineWidth = 4; ctx.stroke();
  }

  /* --- rAF loop --- */
  function frame(now) {
    if (state === STATE.PLAYING) {
      if (lastFrame === null) lastFrame = now;
      const dt = Math.min(now - lastFrame, MAX_DT); // F-1 clamp
      lastFrame = now;
      update(dt);
    } else {
      lastFrame = null; // F-1: no giant delta after resume
    }
    render();
    requestAnimationFrame(frame);
  }

  /* --- input --- */
  document.addEventListener("keydown", (e) => {
    if (e.key === "ArrowLeft")  { keys.left = true;  activeInput = "keys"; e.preventDefault(); }
    if (e.key === "ArrowRight") { keys.right = true; activeInput = "keys"; e.preventDefault(); }
    if (e.key === "p" || e.key === "P") {
      if (state === STATE.PLAYING) setState(STATE.PAUSED);
      else if (state === STATE.PAUSED) setState(STATE.PLAYING);
    }
  });
  document.addEventListener("keyup", (e) => {
    if (e.key === "ArrowLeft") keys.left = false;
    if (e.key === "ArrowRight") keys.right = false;
  });
  canvas.addEventListener("mousemove", (e) => {
    const rect = canvas.getBoundingClientRect();
    mouseTargetX = (e.clientX - rect.left) * (FIELD_W / rect.width);
    activeInput = "mouse";
  });
  canvas.addEventListener("touchmove", (e) => {
    const rect = canvas.getBoundingClientRect();
    mouseTargetX = (e.touches[0].clientX - rect.left) * (FIELD_W / rect.width);
    activeInput = "mouse";
    e.preventDefault();
  }, { passive: false });

  /* --- buttons (no inline handlers, NFR-1) --- */
  btn.start.addEventListener("click", startRound);
  btn.resume.addEventListener("click", () => setState(STATE.PLAYING));
  btn.playagain.addEventListener("click", startRound);
  btn.pause.addEventListener("click", () => { if (state === STATE.PLAYING) setState(STATE.PAUSED); });
  btn.restart.addEventListener("click", startRound);

  /* --- auto-pause on blur, only from PLAYING (F-6, NFR-4) --- */
  window.addEventListener("blur", () => { if (state === STATE.PLAYING) setState(STATE.PAUSED); });
  document.addEventListener("visibilitychange", () => {
    if (document.hidden && state === STATE.PLAYING) setState(STATE.PAUSED);
    lastFrame = null; // F-1
  });

  /* --- boot --- */
  updateHud();
  setState(STATE.START);
  requestAnimationFrame(frame);
})();
}
