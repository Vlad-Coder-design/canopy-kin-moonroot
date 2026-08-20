#!/usr/bin/env node

const { chromium } = require("playwright");
const path = require("path");

const url = process.argv[2];
const screenshot = process.argv[3];
if (!url) {
  console.error("Usage: node verify_webgl.cjs <url> [screenshot.png]");
  process.exit(2);
}

(async () => {
  const browser = await chromium.launch({
    headless: true,
    executablePath: process.env.PLAYWRIGHT_EXECUTABLE_PATH || chromium.executablePath(),
    args: ["--enable-webgl", "--ignore-gpu-blocklist"]
  });
  const context = await browser.newContext({
    viewport: { width: 1600, height: 900 },
    serviceWorkers: "block"
  });
  const page = await context.newPage();
  const consoleErrors = [];
  const failedRequests = [];
  const pushUnique = (items, value) => {
    if (items.length < 50 && !items.includes(value)) items.push(value);
  };

  page.on("console", message => {
    if (message.type() === "error") pushUnique(consoleErrors, message.text());
  });
  page.on("pageerror", error => pushUnique(consoleErrors, error.message));
  page.on("requestfailed", request => pushUnique(failedRequests,
    `${request.url()} :: ${request.failure()?.errorText || "request failed"}`));
  page.on("response", response => {
    if (response.status() >= 400)
      pushUnique(failedRequests, `${response.url()} :: HTTP ${response.status()}`);
  });

  try {
    await page.goto(url, { waitUntil: "domcontentloaded", timeout: 30000 });
    await page.waitForFunction(() => Boolean(window.game), null, { timeout: 180000 });
    // The Unity promise resolves before the first-mission presentation and
    // runtime-built world have necessarily painted their first stable frame.
    await page.waitForTimeout(10000);
    await page.locator("#unity-canvas").click({ position: { x: 800, y: 450 } });
    await page.keyboard.down("KeyW");
    await page.waitForTimeout(800);
    await page.keyboard.up("KeyW");
    await page.waitForTimeout(1000);

    const result = await page.evaluate(() => ({
      title: document.title,
      gameReady: Boolean(window.game),
      shellDisplay: getComputedStyle(document.querySelector("#shell")).display,
      canvasDisplay: getComputedStyle(document.querySelector("#unity-canvas")).display,
      canvasWidth: document.querySelector("#unity-canvas").width,
      canvasHeight: document.querySelector("#unity-canvas").height,
      input: window.getWebglInputAcceptance?.(),
      diagnostics: window.webglDiagnostics || []
    }));

    if (screenshot)
      await page.screenshot({ path: path.resolve(screenshot), fullPage: true });

    result.consoleErrors = consoleErrors;
    result.failedRequests = failedRequests;
    console.log(JSON.stringify(result, null, 2));

    if (!result.gameReady || result.canvasDisplay === "none" ||
        consoleErrors.length || failedRequests.length)
      process.exitCode = 1;
  } finally {
    await browser.close();
  }
})().catch(error => {
  console.error(error.stack || error);
  process.exit(1);
});
