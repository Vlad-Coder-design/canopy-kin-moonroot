// Run with a Playwright-enabled Node.js:
//   node QA/webgl-movement-acceptance.mjs https://example.github.io/canopy-kin-moonroot/
//
// This is an automated regression gate for canvas focus and movement-state
// recovery. It deliberately complements, rather than replaces, the manual
// visual playtest recorded in QA/Screenshots.
import { chromium } from 'playwright';

const target = process.argv[2];
if (!target) throw new Error('Pass the deployed WebGL URL as the first argument.');

const url = new URL(target);
url.searchParams.set('movementDiagnostics', '1');
url.searchParams.set('acceptance', String(Date.now()));

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
const consoleLines = [];
page.on('console', message => consoleLines.push(message.text()));

try {
  await page.goto(url.href, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.waitForFunction(() => Boolean(window.game), null, { timeout: 180_000 });

  const canvas = page.locator('#unity-canvas');
  await canvas.click({ position: { x: 720, y: 450 } });
  await page.keyboard.down('w');
  await page.waitForTimeout(900);
  await page.keyboard.up('w');
  await page.waitForTimeout(500);

  const beforeRecovery = await page.evaluate(() => window.getWebglInputAcceptance());
  if (beforeRecovery.activeElement !== 'canvas')
    throw new Error(`Canvas did not retain focus: ${JSON.stringify(beforeRecovery)}`);
  if (beforeRecovery.focusedMovementKeydowns < 1)
    throw new Error(`Focused W input was not observed: ${JSON.stringify(beforeRecovery)}`);

  await page.locator('#full').click();
  await page.waitForTimeout(500);
  await page.keyboard.press('Escape');
  await page.waitForTimeout(500);
  await canvas.click({ position: { x: 720, y: 450 } });
  await page.keyboard.press('a');
  await page.waitForTimeout(700);

  const afterRecovery = await page.evaluate(() => window.getWebglInputAcceptance());
  if (afterRecovery.fullscreenRecoveries < 1)
    throw new Error(`Fullscreen focus recovery did not run: ${JSON.stringify(afterRecovery)}`);
  if (afterRecovery.activeElement !== 'canvas')
    throw new Error(`Canvas focus was not restored: ${JSON.stringify(afterRecovery)}`);
  if (afterRecovery.focusedMovementKeydowns < 2)
    throw new Error(`Movement input did not recover: ${JSON.stringify(afterRecovery)}`);

  const movementSamples = consoleLines.filter(line =>
    line.includes('MOONROOT_MOVEMENT_SAMPLE'));
  if (!movementSamples.some(line => !line.includes('raw=(0.00, 0.00)')))
    throw new Error('Unity did not report a non-zero raw movement sample.');
  if (!movementSamples.some(line => !line.includes('velocity=(0.00, 0.00, 0.00)')))
    throw new Error('Unity did not report physical movement after keyboard input.');

  console.log(JSON.stringify({
    result: 'PASS',
    url: url.href,
    beforeRecovery,
    afterRecovery,
    movementSamples: movementSamples.slice(-6)
  }, null, 2));
} finally {
  await browser.close();
}
