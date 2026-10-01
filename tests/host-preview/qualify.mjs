// Foreground desktop qualification driver for the Signal Garden host preview.
//
// Credential-free, Node standard library only. Launches a real (non-headless)
// Chrome or Edge window on the exact 1920x1080 reference render, drives the
// documented pointer path, and records the evidence SG-09 requires.
//
// This driver records observations. It deliberately does not decide PASS/FAIL:
// scripts/ci/ validators and owner qualification own those classifications.
//
// Usage:
//   $env:SG_BROWSER = "C:\Program Files\Google\Chrome\Application\chrome.exe"
//   node tests/host-preview/qualify.mjs --url=<url> --out=<path> [--label=<text>]
//
// Preconditions: `node tests/host-preview/serve.mjs --port <port>` is running
// and Builds/WebGL/Build holds the build under test.

import { spawn } from "node:child_process";
import { mkdtempSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { basename, join } from "node:path";

const args = {};
for (const token of process.argv.slice(2)) {
  const index = token.indexOf("=");
  if (index < 0) args[token.replace(/^--/, "")] = true;
  else args[token.slice(2, index)] = token.slice(index + 1);
}

const browserExe = process.env.SG_BROWSER || args.browser;
if (!browserExe) {
  console.error("Set SG_BROWSER to the Chrome or Edge executable path.");
  process.exit(2);
}

const targetUrl = args.url;
const outPath = args.out;
const label = args.label || browserExe;
const cdpPort = Number(args["cdp-port"] || 9222);
const readyTimeoutMs = Number(args["ready-timeout-ms"] || 180000);
const skipDrag = Boolean(args["skip-drag"]);
const invalidRouteOnly = Boolean(args["invalid-route"]);

if (!targetUrl || !outPath) {
  console.error("--url and --out are required.");
  process.exit(2);
}

// RouteRules.StandardTrail, in world X/Z, and the scene camera transform in
// Assets/Scenes/SignalGarden.unity (position 0,11.8,-13.4, orthographic size 4.7).
const STANDARD_TRAIL = [
  [-4.3, 2.2], [-2.9, 2.2], [-2.9, 1.0], [-1.4, 1.0], [-1.4, -0.3],
  [0.0, -0.3], [0.0, -1.7], [1.8, -1.7], [1.8, -2.6], [4.3, -2.6],
];
// RouteRules.DeadEndSpur, the blind branch used to exercise Recovery.
const DEAD_END_SPUR = [
  [-4.3, 2.2], [-2.9, 2.2], [-2.9, 1.0], [-2.05, 2.05], [-0.65, 2.05],
];

const ORTHOGRAPHIC_SIZE = 4.7;
const REFERENCE_WIDTH = 1920;
const REFERENCE_HEIGHT = 1080;
const ROUTE_DRAW_HEIGHT = 0.6;
// Camera basis expressed in world space from the serialized quaternion.
const CAMERA_UP_Y = 0.742191;
const CAMERA_UP_Z = 0.670164;
const CAMERA_POSITION_Y = 11.8;
const CAMERA_POSITION_Z = -13.4;

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const profile = mkdtempSync(join(tmpdir(), "sg-qualify-"));
const browser = spawn(
  browserExe,
  [
    "--remote-debugging-port=" + cdpPort,
    "--user-data-dir=" + profile,
    "--no-first-run",
    "--no-default-browser-check",
    "--disable-background-timer-throttling",
    "--disable-renderer-backgrounding",
    "--disable-backgrounding-occluded-windows",
    "--window-position=0,0",
    "--window-size=1920,1080",
    "--hide-crash-restore-bubble",
    "about:blank",
  ],
  { stdio: "ignore", detached: true }
);

async function findWebSocketUrl() {
  for (let attempt = 0; attempt < 160; attempt++) {
    try {
      const response = await fetch("http://127.0.0.1:" + cdpPort + "/json/version");
      const body = await response.json();
      if (body.webSocketDebuggerUrl) return body.webSocketDebuggerUrl;
    } catch {}
    await sleep(250);
  }
  throw new Error("CDP endpoint never became available on port " + cdpPort);
}

const socket = new WebSocket(await findWebSocketUrl());
await new Promise((resolve, reject) => {
  socket.addEventListener("open", resolve, { once: true });
  socket.addEventListener("error", reject, { once: true });
});

let nextId = 1;
const pending = new Map();
socket.addEventListener("message", (event) => {
  const message = JSON.parse(event.data);
  if (message.id && pending.has(message.id)) {
    const entry = pending.get(message.id);
    pending.delete(message.id);
    if (message.error) entry.reject(new Error(JSON.stringify(message.error)));
    else entry.resolve(message.result);
  }
});

function send(method, params = {}, sessionId) {
  const id = nextId++;
  const payload = { id, method, params };
  if (sessionId) payload.sessionId = sessionId;
  socket.send(JSON.stringify(payload));
  return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
}

const consoleMessages = [];
const pageErrors = [];
const failedRequests = [];

const { targetId } = await send("Target.createTarget", { url: "about:blank" });
const { sessionId } = await send("Target.attachToTarget", { targetId, flatten: true });

await send("Page.enable", {}, sessionId);
await send("Runtime.enable", {}, sessionId);
await send("Log.enable", {}, sessionId);
await send("Network.enable", {}, sessionId);
await send("Accessibility.enable", {}, sessionId);

socket.addEventListener("message", (event) => {
  const message = JSON.parse(event.data);
  if (message.method === "Runtime.consoleAPICalled") {
    consoleMessages.push({
      level: message.params.type,
      text: (message.params.args || [])
        .map((arg) => arg.value ?? arg.description ?? arg.type)
        .join(" "),
    });
  } else if (message.method === "Runtime.exceptionThrown") {
    pageErrors.push(message.params.exceptionDetails?.text || "exception");
  } else if (message.method === "Log.entryAdded") {
    consoleMessages.push({
      level: message.params.entry.level,
      text: message.params.entry.text,
      url: message.params.entry.url || null,
    });
  } else if (message.method === "Network.loadingFailed") {
    failedRequests.push({
      requestId: message.params.requestId,
      errorText: message.params.errorText,
    });
  } else if (message.method === "Network.responseReceived") {
    if (message.params.response.status >= 400) {
      failedRequests.push({
        url: message.params.response.url,
        status: message.params.response.status,
      });
    }
  }
});

async function evaluate(expression, awaitPromise = false) {
  const result = await send(
    "Runtime.evaluate",
    { expression, returnByValue: true, awaitPromise },
    sessionId
  );
  if (result.exceptionDetails) throw new Error(result.exceptionDetails.text);
  return result.result?.value;
}

const readStatusExpression =
  '(document.getElementById("signal-garden-accessible-status")||{}).textContent||""';

async function readStatus() {
  return evaluate(readStatusExpression);
}

const browserVersion = await send("Browser.getVersion");

await send("Page.navigate", { url: targetUrl }, sessionId);

// The harness refuses to sample unless the page is genuinely visible and
// focused, so bring the window forward before sampling starts. A run that
// never reaches this state is recorded as not-run rather than as a failure.
let windowState = "unknown";
let foregroundAttempts = 0;
for (let attempt = 0; attempt < 80; attempt++) {
  const state = await evaluate('document.visibilityState + "|" + document.hasFocus()');
  windowState = state;
  if (state.indexOf("visible|") === 0) break;
  if (state.indexOf("visible") === 0 && attempt >= 2) break;
  if (foregroundAttempts < 3) {
    foregroundAttempts++;
    bringWindowForward();
  }
  await sleep(500);
}

const navigationStartedAt = Date.now();
let readyStatus = null;
while (Date.now() - navigationStartedAt < readyTimeoutMs) {
  const status = await readStatus();
  if (status && status.indexOf("Ready.") >= 0) {
    readyStatus = status;
    break;
  }
  await sleep(250);
}

const report = {
  schemaVersion: 1,
  tool: "tests/host-preview/qualify.mjs",
  browser: {
    label,
    product: browserVersion.product,
    revision: browserVersion.revision,
    userAgent: browserVersion.userAgent,
  },
  target: targetUrl,
  capturedAt: new Date().toISOString(),
  windowState,
  readyStatus,
  readyAfterMilliseconds: Date.now() - navigationStartedAt,
};

if (!readyStatus) {
  report.status = "NOT_READY";
} else {
  report.status = "READY";
  report.canvas = await evaluate(`(() => {
    const canvas = document.getElementById("unity-canvas");
    const rect = canvas.getBoundingClientRect();
    return {
      bufferWidth: canvas.width,
      bufferHeight: canvas.height,
      exactReferenceRender: canvas.width === ${REFERENCE_WIDTH} && canvas.height === ${REFERENCE_HEIGHT},
      cssX: rect.left, cssY: rect.top, cssWidth: rect.width, cssHeight: rect.height,
      hasFocus: document.hasFocus(),
      visibilityState: document.visibilityState,
      devicePixelRatio: window.devicePixelRatio,
    };
  })()`);

  report.timeToInteractive = {
    signalGardenLoadMs: await evaluate("window.signalGardenLoadMs ?? null"),
    signalGardenInteractiveAt: await evaluate("window.signalGardenInteractiveAt ?? null"),
    budgetMilliseconds: 10000,
  };

  report.buildCacheKey = await evaluate(
    "document.getElementById('unity-canvas') ? (window.signalGardenUnity ? 'unity-instance-present' : 'no-instance') : 'no-canvas'"
  );

  report.webgl = {
    contextCreated: consoleMessages.some((m) => m.text.indexOf("Creating WebGL 2.0 context") >= 0),
    renderer: (consoleMessages.find((m) => m.text.indexOf("Renderer: ") >= 0) || {}).text || null,
    invalidOperation: consoleMessages.some((m) => m.text.indexOf("INVALID_OPERATION") >= 0),
    // WEBGL_lose_context appears in the GLES extension list, so only a real
    // loss message counts here.
    contextLost: consoleMessages.some((m) =>
      /webgl context lost|context lost at|glcontext.*lost/i.test(m.text)
    ),
  };

  // Drive the documented pointer path first so the harness smoke settles while
  // its own 120s window is open, then collect the performance sample.
  if (!skipDrag && !invalidRouteOnly) {
    report.route = await runRoute();
    // The harness polls its own smoke result every 100ms, so let it settle
    // before reading it rather than racing the observer.
    report.routeSmoke = await evaluate(`(async () => {
      for (let i = 0; i < 120; i++) {
        const result = window.signalGardenSmokeResult;
        if (result && (result.passed || result.error)) return result;
        await new Promise((resolve) => setTimeout(resolve, 100));
      }
      return window.signalGardenSmokeResult ?? null;
    })()`, true);
  }

  if (invalidRouteOnly) {
    report.recovery = await runInvalidRoute();
  }

// The harness starts its own sampler at load and only advances while the
// page is genuinely visible and focused. Do not reset its result; wait for
// it, and re-assert the foreground window if focus is stolen mid-run.
let performance = null;
const performanceRequested = targetUrl.indexOf("sg-stats=1") >= 0;
const performanceDeadline = Date.now() + (performanceRequested ? 420000 : 0);
let reassertions = 0;
let focusLostTicks = 0;
let focusTicks = 0;
while (Date.now() < performanceDeadline) {
  performance = await evaluate("window.signalGardenPerformanceResult ?? null");
  if (performance) break;
  const state = await evaluate('document.visibilityState + "|" + document.hasFocus()');
  focusTicks++;
  if (state.indexOf("visible|true") !== 0) {
    focusLostTicks++;
    if (reassertions < 40) {
      reassertions++;
      bringWindowForward();
    }
  }
  await sleep(500);
}
report.performance = performance;
report.performanceRequested = performanceRequested;
report.performanceReassertions = reassertions;
report.focusContinuity = {
  observedTicks: focusTicks,
  unfocusedTicks: focusLostTicks,
  continuouslyFocused: focusTicks > 0 && focusLostTicks === 0,
};
report.focusAtSampleEnd = await evaluate("document.hasFocus()");
if (!performance) {
  report.performanceNotRunReason = performanceRequested
    ? "Harness sampler never produced a result; the page was not continuously visible and focused."
    : "URL did not request stats sampling (sg-stats=1 absent).";
}

  report.accessibility = await collectAccessibility();
}

report.consoleErrors = consoleMessages.filter((m) => m.level === "error");
report.consoleWarnings = consoleMessages.filter((m) => m.level === "warning");
report.consoleLogs = consoleMessages.filter((m) => m.level === "log" || m.level === "info");
report.pageErrors = pageErrors;
report.failedRequests = failedRequests;

// The preview harness does not serve a favicon, so a favicon.ico 404 is host
// noise rather than a game defect. Everything else is reported as-is.
const isFaviconNoise = (entry) =>
  String(entry.url || "").indexOf("favicon.ico") >= 0 ||
  (String(entry.text || "").indexOf("Failed to load resource") >= 0 &&
    failedRequests.every(isFaviconNoise));
report.harnessNoise = {
  faviconRequests: report.failedRequests.filter(isFaviconNoise).length,
};
report.gameFailedRequests = failedRequests.filter((entry) => !isFaviconNoise(entry));
report.gameConsoleErrors = report.consoleErrors.filter((m) => !isFaviconNoise(m));

writeFileSync(outPath, JSON.stringify(report, null, 2));
socket.close();
try {
  spawn("taskkill", ["/PID", String(browser.pid), "/T", "/F"], { stdio: "ignore" });
} catch {}
process.exit(0);

// Best-effort Windows focus request. Chrome reports document.visibilityState
// "hidden" while its window is minimized or occluded, which makes foreground
// performance evidence impossible; request a restore and activation.
function bringWindowForward() {
  if (process.platform !== "win32") return;
  const profileNeedle = profile.split("\\").pop();
  const script =
    "$s='using System;using System.Runtime.InteropServices;" +
    "public class Q{[DllImport(\"user32.dll\")]public static extern bool ShowWindow(IntPtr h,int n);" +
    "[DllImport(\"user32.dll\")]public static extern bool SetForegroundWindow(IntPtr h);}" +
    "Add-Type -TypeDefinition $s;" +
    "$p=Get-CimInstance Win32_Process -Filter \"Name='" +
    basename(browserExe) +
    "'\"|Where-Object{$_.CommandLine -like '*" +
    profileNeedle +
    "*' -and $_.CommandLine -notlike '*--type=*'}|Select-Object -First 1;" +
    "if($p){$h=(Get-Process -Id $p.ProcessId).MainWindowHandle;" +
    "if($h -ne 0){[void][Q]::ShowWindow($h,9);[void][Q]::ShowWindow($h,5);[void][Q]::SetForegroundWindow($h)}}";
  try {
    spawn("powershell", ["-NoProfile", "-NonInteractive", "-Command", script], {
      stdio: "ignore",
    });
  } catch {}
}

// Re-projects an arbitrary world-space trail for the recovery probe.
function projectPointsExpression(points) {
  return `(() => {
    const canvas = document.getElementById("unity-canvas");
    const rect = canvas.getBoundingClientRect();
    const pixelsPerUnit = ${REFERENCE_HEIGHT} / (2 * ${ORTHOGRAPHIC_SIZE});
    const scaleX = rect.width / ${REFERENCE_WIDTH};
    const scaleY = rect.height / ${REFERENCE_HEIGHT};
    return ${JSON.stringify(points)}.map(([X, Z]) => {
      const viewX = X;
      const viewY = ${CAMERA_UP_Y} * (${ROUTE_DRAW_HEIGHT} - (${CAMERA_POSITION_Y}))
                 + ${CAMERA_UP_Z} * (Z - (${CAMERA_POSITION_Z}));
      const bufferX = ${REFERENCE_WIDTH / 2} + viewX * pixelsPerUnit;
      const bufferY = ${REFERENCE_HEIGHT / 2} - viewY * pixelsPerUnit;
      return { x: rect.left + bufferX * scaleX, y: rect.top + bufferY * scaleY };
    });
  })()`;
}

function projectTrailExpression() {
  return projectPointsExpression(STANDARD_TRAIL);
}

// Unity reports its frame rate from Update(), so a live, advancing value means
// the player loop is running and can consume pointer input. Dragging before
// that lands is silently ignored by the game.
async function waitForUnityRunning(timeoutMilliseconds = 60000) {
  const deadline = Date.now() + timeoutMilliseconds;
  let lastSeen = null;
  while (Date.now() < deadline) {
    const sample = await evaluate(`(() => {
      const fps = Number(window.signalGardenFrameRate);
      const updatedAt = Number(window.signalGardenFrameRateUpdatedAt || 0);
      const fresh = performance.now() - updatedAt <= 2500;
      return (Number.isFinite(fps) && updatedAt > 0 && fresh)
        ? JSON.stringify({ fps, updatedAt }) : null;
    })()`);
    if (sample) {
      if (lastSeen !== null && lastSeen !== sample) return JSON.parse(sample);
      lastSeen = sample;
    }
    await sleep(500);
  }
  return null;
}

async function dispatchPointer(type, x, y, buttons) {
  await send(
    "Input.dispatchMouseEvent",
    { type, x, y, button: "left", buttons, clickCount: 1 },
    sessionId
  );
}

async function runRoute() {
  const unityRunning = await waitForUnityRunning();
  const attempts = [];
  for (let attempt = 1; attempt <= 3; attempt++) {
    const points = await evaluate(projectTrailExpression());
    const result = {
      unityRunningBeforeDrag: unityRunning,
      attempt,
      projectionPoints: points,
      transitions: [],
    };

    await evaluate('document.getElementById("unity-canvas").focus({preventScroll:true})');
    await dispatchPointer("mouseMoved", points[0].x, points[0].y, 0);
    await dispatchPointer("mousePressed", points[0].x, points[0].y, 1);
    await sleep(150);
    result.transitions.push({ stage: "after-press", status: await readStatus() });

    for (let index = 1; index < points.length; index++) {
      const from = points[index - 1];
      const to = points[index];
      const subdivisions = 8;
      for (let step = 1; step <= subdivisions; step++) {
        const x = from.x + ((to.x - from.x) * step) / subdivisions;
        const y = from.y + ((to.y - from.y) * step) / subdivisions;
        await dispatchPointer("mouseMoved", x, y, 1);
        await sleep(45);
      }
      result.transitions.push({
        stage: "reached-waypoint-" + index,
        status: await readStatus(),
      });
    }

    await sleep(200);
    await dispatchPointer(
      "mouseReleased",
      points[points.length - 1].x,
      points[points.length - 1].y,
      0
    );

    for (let poll = 0; poll < 120; poll++) {
      const status = await readStatus();
      if (status.indexOf("Signal received") >= 0) {
        result.finalStatus = status;
        result.verified = true;
        attempts.push(result);
        return { unityRunningBeforeDrag: unityRunning, attempts, projectionPoints: points, transitions: result.transitions, finalStatus: status, verified: true };
      }
      if (
        status.indexOf("Try again") >= 0 ||
        status.indexOf("canceled") >= 0 ||
        status.indexOf("Focus changed") >= 0
      ) {
        result.finalStatus = status;
        result.verified = false;
        result.outcome =
          status.indexOf("Focus changed") >= 0 ? "focus-interrupted" : "route-rejected";
        attempts.push(result);
        break;
      }
      await sleep(250);
    }

    if (result.verified) return result;
    // A focus interruption is an environment artifact, not a game defect: the
    // game correctly cancelled. Restore the foreground window and retry.
    if (result.outcome === "focus-interrupted" && attempt < 3) {
      bringWindowForward();
      await evaluate('document.getElementById("unity-canvas").focus({preventScroll:true})');
      await sleep(1500);
      continue;
    }
    return { ...result, attempts };
  }
  return { unityRunningBeforeDrag: unityRunning, attempts, verified: false };
}

// Drives the route into the blind dead-end spur and records whether the game
// enters Recovery with an understandable failure trace and a retry instruction.
async function runInvalidRoute() {
  await waitForUnityRunning();
  const points = await evaluate(projectPointsExpression(DEAD_END_SPUR));
  const result = { path: "RouteRules.DeadEndSpur", transitions: [] };

  await evaluate('document.getElementById("unity-canvas").focus({preventScroll:true})');
  await dispatchPointer("mouseMoved", points[0].x, points[0].y, 0);
  await dispatchPointer("mousePressed", points[0].x, points[0].y, 1);
  await sleep(150);
  result.transitions.push({ stage: "after-press", status: await readStatus() });

  for (let index = 1; index < points.length; index++) {
    const from = points[index - 1];
    const to = points[index];
    for (let step = 1; step <= 8; step++) {
      await dispatchPointer(
        "mouseMoved",
        from.x + ((to.x - from.x) * step) / 8,
        from.y + ((to.y - from.y) * step) / 8,
        1
      );
      await sleep(45);
    }
  }

  result.statusBeforeRelease = await readStatus();
  await dispatchPointer(
    "mouseReleased",
    points[points.length - 1].x,
    points[points.length - 1].y,
    0
  );

  for (let poll = 0; poll < 120; poll++) {
    const status = await readStatus();
    result.transitions.push({ stage: "poll-" + poll, status });
    if (/spur|no receiver|again|Try again/i.test(status) && status !== result.statusBeforeRelease) {
      result.finalStatus = status;
      result.enteredRecovery = true;
      result.namesRetryAction = /again|Try again|coral source/i.test(status);
      return result;
    }
    await sleep(250);
  }

  result.finalStatus = await readStatus();
  result.enteredRecovery = false;
  return result;
}

async function collectAccessibility() {
  const dom = await evaluate(`(() => {
    const describe = (element) => ({
      tag: element.tagName.toLowerCase(),
      id: element.id || null,
      role: element.getAttribute("role"),
      ariaLabel: element.getAttribute("aria-label"),
      ariaLive: element.getAttribute("aria-live"),
      tabIndex: element.tabIndex,
      text: (element.textContent || "").trim().slice(0, 80),
    });
    const status = document.getElementById("signal-garden-accessible-status");
    const canvas = document.getElementById("unity-canvas");
    const focusable = Array.from(
      document.querySelectorAll('a[href], button, input, select, textarea, [tabindex]')
    )
      .filter((element) => element.offsetParent !== null || element.id === "unity-canvas")
      .map(describe);
    return {
      statusRegion: status ? describe(status) : null,
      canvas: canvas ? describe(canvas) : null,
      focusOrder: focusable,
      hasMainLandmark: !!document.querySelector("main"),
      langAttribute: document.documentElement.lang,
    };
  })()`);

  let axTree = null;
  try {
    const full = await send("Accessibility.getFullAXTree", {}, sessionId);
    axTree = (full.nodes || []).map((node) => ({
      role: node.role?.value,
      name: node.name?.value ?? null,
      ignored: node.ignored,
      focusable: node.properties?.some((p) => p.name === "focusable" && p.value?.value),
    }));
  } catch (error) {
    axTree = { error: String(error) };
  }

  // A DOM screen reader can only reach nodes present in this tree. The Unity
  // HUD is drawn inside the canvas, so record explicitly what is reachable.
  const roleCounts = {};
  for (const node of Array.isArray(axTree) ? axTree : []) {
    const role = (node && node.role) || "unknown";
    roleCounts[role] = (roleCounts[role] || 0) + 1;
  }
  const interactiveRoles = ["button", "slider", "checkbox", "switch", "link", "textbox"];
  const reachableInteractive = (Array.isArray(axTree) ? axTree : []).filter((node) =>
    interactiveRoles.includes(node.role)
  );

  return {
    dom,
    accessibilityTree: axTree,
    roleCounts,
    reachableInteractiveCount: reachableInteractive.length,
    reachableInteractive,
  };
}
