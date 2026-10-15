// Renders ad.html frame by frame in headless Chromium.
//   node capture.mjs <outDir>                 every frame (frame_00000.png …)
//   node capture.mjs <outDir> 1.2,4.5,9.9     only those times (t_<seconds>.png), for review
//   add --vertical to either form for the 1080×1920 (9:16) layout
import { createRequire } from 'node:module';
import { readFileSync, mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, '..', '..', '..');
const require = createRequire(join(repo, 'src', 'remote', 'package.json'));
const { chromium } = require('playwright');

const vertical = process.argv.includes('--vertical');
const [outDir, times_] = process.argv.slice(2).filter(a => a !== '--vertical');
const only = times_ ? times_.split(',').map(Number) : null;
if (!outDir) { console.error('usage: node capture.mjs <outDir> [t1,t2,…] [--vertical]'); process.exit(2); }
mkdirSync(outDir, { recursive: true });

const tl = JSON.parse(readFileSync(join(here, 'timeline.json'), 'utf8'));
if (vertical) [tl.width, tl.height] = [tl.height, tl.width];
const browser = await chromium.launch({ args: ['--allow-file-access-from-files', '--force-color-profile=srgb', '--font-render-hinting=none'] });
const page = await browser.newPage({ viewport: { width: tl.width, height: tl.height }, deviceScaleFactor: 1 });
page.on('console', m => { if (m.type() === 'error') console.error('page:', m.text()); });
page.on('pageerror', e => { console.error('pageerror:', e.message); process.exitCode = 1; });
await page.addInitScript(`window.TL = ${JSON.stringify(tl)}; window.__CAPTURE = true;`);
await page.goto(pathToFileURL(join(here, 'ad.html')).href);
await page.evaluate(() => window.ready);

const times = only ?? Array.from({ length: Math.round(tl.duration * tl.fps) }, (_, i) => i / tl.fps);
const started = Date.now();
for (let i = 0; i < times.length; i++) {
  const t = times[i];
  await page.evaluate(x => window.renderAt(x), t);
  const name = only ? `t_${t.toFixed(2)}.png` : `frame_${String(i).padStart(5, '0')}.png`;
  writeFileSync(join(outDir, name), await page.screenshot({ type: 'png' }));
  if (!only && i % 60 === 0) console.log(`frame ${i}/${times.length}  ${((Date.now() - started) / 1000).toFixed(1)} s`);
}
await browser.close();
console.log(`wrote ${times.length} frame(s) to ${outDir}`);
