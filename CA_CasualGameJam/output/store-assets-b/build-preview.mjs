import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
const { createCanvas, loadImage } = require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const root = path.dirname(fileURLToPath(import.meta.url));
const { exports: items } = JSON.parse(await fs.readFile(path.join(root, 'manifest.json'), 'utf8'));
const canvas = createCanvas(1440, 960), ctx = canvas.getContext('2d');
ctx.fillStyle = '#eff7fc'; ctx.fillRect(0, 0, 1440, 960);
ctx.fillStyle = '#102451'; ctx.font = 'bold 36px Inter, Arial'; ctx.fillText('COLOR TRACE', 42, 60);
ctx.fillStyle = '#52647d'; ctx.font = '20px Inter, Arial'; ctx.fillText('B / 2D TOON BOLD     •     STORE ASSET CANDIDATES', 42, 96);
for (const [index, label] of ['MIX SYMBOL', 'TILE BOARD', 'BOLD PATHS'].entries()) {
  const concept = String(index + 1).padStart(2, '0'), x = 32 + index * 468;
  ctx.fillStyle = '#ffffff'; ctx.beginPath(); ctx.roundRect(x, 124, 440, 794, 18); ctx.fill();
  ctx.fillStyle = '#102451'; ctx.font = 'bold 22px Inter, Arial'; ctx.fillText(`${concept}  ${label}`, x + 22, 163);
  const icon = items.find(item => item.concept === concept && item.type === 'icon' && item.width === 1024);
  const feature = items.find(item => item.concept === concept && item.type === 'feature');
  ctx.drawImage(await loadImage(path.join(root, icon.file)), x + 60, 188, 320, 320);
  ctx.fillStyle = '#52647d'; ctx.font = '16px Inter, Arial'; ctx.fillText(`ICON  1024 × 1024  /  ${Math.round(icon.bytes / 1000)} KB`, x + 40, 542);
  for (const [i, size] of [48, 64, 96].entries()) ctx.drawImage(await loadImage(path.join(root, icon.file)), x + 88 + [0, 68, 152][i], 571 + (96 - size) / 2, size, size);
  ctx.drawImage(await loadImage(path.join(root, feature.file)), x + 16, 700, 408, 408 * 500 / 1024);
  ctx.fillStyle = '#52647d'; ctx.font = '14px Inter, Arial'; ctx.fillText('FEATURE  1024 × 500  /  PNG', x + 22, 688);
}
await fs.writeFile(path.join(root, 'comparison.png'), canvas.toBuffer('image/png'));
console.log('comparison.png: 1440 × 960');
