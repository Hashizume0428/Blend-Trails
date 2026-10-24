import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createCanvas, loadImage} from 'file:///C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas/index.js';

const root = path.dirname(fileURLToPath(import.meta.url));
const items = [
  ['title', 'タイトル', '正式名のロゴへ差し替え。赤＋黄＝オレンジのモチーフを継承。'],
  ['gameplay', '盤面の余白', '外枠を広げ、四隅のタイルにも余白を確保。'],
  ['clear', 'ステージクリア', 'クリーム色のカード、混色チェック、完成盤面の縮小と次のStage番号。HOMEなし。'],
  ['reset-arrows', 'リセット矢印', '円弧と矢じりがつながる形へ修正。'],
  ['clear-tall', '縦長画面でのクリア', '941×2039の実画面。カードと操作ボタンをSafe Area内へ配置。'],
  ['stage6', '赤＋黄', '既存のステージ配色を維持。'],
  ['stage11', '青＋黄', '既存のステージ配色を維持。'],
];
const canvas = createCanvas(1278, 748);
const ctx = canvas.getContext('2d');
ctx.fillStyle = '#071b52'; ctx.fillRect(0, 0, canvas.width, canvas.height);
for (let i = 0; i < 3; i++) {
  const bitmap = await loadImage(path.join(root, items[i][0] + '.png'));
  ctx.drawImage(bitmap, 24 + i * 418, 24, 394, 700);
}
await fs.writeFile(path.join(root, 'overview.png'), canvas.toBuffer('image/png'));
const cards = items.map(([file, title, description]) => `<article><h2>${title}</h2><p>${description}</p><a href="${file}.png" target="_blank"><img src="${file}.png" alt="${title}" loading="lazy"></a></article>`).join('\n');
await fs.writeFile(path.join(root, 'index.html'), `<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Blend Trails: Color Puzzle — UI修正</title><style>*{box-sizing:border-box}body{margin:0;background:#071b52;color:#fff6d8;font:16px/1.65 system-ui,sans-serif}header{padding:32px max(24px,5vw)}h1{font-size:clamp(24px,4vw,42px);line-height:1.2;margin:0 0 12px}header p{margin:0;color:#c1dcff}main{padding:0 max(24px,5vw) 48px;display:grid;grid-template-columns:repeat(auto-fit,minmax(270px,1fr));gap:24px}article{background:#112b63;border:1px solid #36558a;border-radius:20px;padding:18px}h2{margin:0 0 6px;font-size:21px}p{margin:0 0 16px;min-height:3.3em}img{display:block;width:100%;height:auto;border-radius:12px}a{color:#fff6d8}</style><header><h1>Blend Trails: Color Puzzle</h1><p>UI修正後のUnity実画面 · 2026-10-25</p></header><main>${cards}</main></html>`);
console.log('Wrote overview.png and index.html');
