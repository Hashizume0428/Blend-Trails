import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import crypto from 'node:crypto';

const require = createRequire(import.meta.url);
const { createCanvas, loadImage, GlobalFonts } = require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const sharp = require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root = path.dirname(fileURLToPath(import.meta.url));
const entries = [
  { id: '01-title', file: '01-title.png', title: '01 タイトル', short: 'TITLE', group: 'main', note: 'COLOR TRACEのロゴと混色モチーフ。現在のステージへ進むボタンを1つ配置。STAGE 3は表示例。' },
  { id: '02-gameplay', file: '02-gameplay.png', title: '02 通常プレイ', short: 'GAMEPLAY', group: 'main', note: '5列×4行の盤面。指定色は四角い枠で表示し、混色ガイドとALL・青・赤のリセットを配置。' },
  { id: '06-clear', file: '06-clear.png', title: '06 クリア', short: 'CLEAR', group: 'main', note: '完成盤面を60%へ縮小し、紙吹雪で達成を表現。中央のSTAGE 4ボタン1つで次へ進む。HOMEは配置しない。' },
  { id: '03-tutorial', file: '03-tutorial.png', title: '03 チュートリアル', short: 'TUTORIAL', group: 'state', note: 'Stage1だけに指と点線のガイドを重ねる。説明文を追加せず、操作方向を視覚で伝える。赤の案内時の表示例。' },
  { id: '04-incorrect', file: '04-incorrect.png', title: '04 不正解表示', short: 'INCORRECT', group: 'state', note: '誤りのあるマスだけ、現在色と指定枠の透明度を2秒間点滅。線と着色を保持して修正を続ける。画像は点滅途中の静止画。SEと振動は画像には含まれない。' },
  { id: '05-resume-color', file: '05-resume-color.png', title: '05 線の再開・色選択', short: 'SELECT COLOR', group: 'state', note: '2色の終点が同じマスにあるとき、再開する色を選ぶ小さなカードを表示。色を選んだ後、終点から改めてドラッグする。' },
];

for (const item of entries) {
  const buffer = await fs.readFile(path.join(root, item.file));
  const meta = await sharp(buffer).metadata();
  item.width = meta.width;
  item.height = meta.height;
  item.bytes = buffer.length;
  item.sha256 = crypto.createHash('sha256').update(buffer).digest('hex');
  if (Math.abs(item.width / item.height - 9 / 16) > .025) throw new Error(`Unexpected portrait aspect: ${item.file}`);
}
await fs.writeFile(path.join(root, 'manifest.json'), JSON.stringify({ date: '2026-10-25', timezone: 'Asia/Tokyo', purpose: 'Review concepts; not implemented in Unity', entries }, null, 2));

const escape = value => String(value).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const cards = group => entries.filter(item => item.group === group).map(item => `<article><div class="caption"><h3>${escape(item.title)}</h3><a href="${item.file}" download aria-label="${escape(item.title)}のPNGを保存">PNG ↓</a></div><button class="image" data-file="${item.file}" aria-label="${escape(item.title)}を拡大"><img src="${item.file}" alt="${escape(item.title)} B案の画面イメージ" width="${item.width}" height="${item.height}"></button><p>${escape(item.note)}</p></article>`).join('');

await fs.writeFile(path.join(root, 'index.html'), `<!DOCTYPE html>
<html lang="ja"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>B案 UI刷新 / 画面イメージ確認</title><style>
:root{color-scheme:light;--navy:#071b52;--sky:#2eb9ef;--cream:#fff6d8}*{box-sizing:border-box}body{margin:0;background:#eaf6fc;color:var(--navy);font-family:"Yu Gothic UI","Meiryo",sans-serif;line-height:1.7}main{max-width:1420px;margin:auto;padding:42px 32px 64px}header{border-bottom:3px solid var(--navy);padding-bottom:26px;margin-bottom:32px}.eyebrow{letter-spacing:.13em;font-size:12px;font-weight:800}h1{font-size:clamp(25px,3vw,42px);line-height:1.3;margin:12px 0 14px}h2{font-size:24px;margin:34px 0 16px}h3{font-size:19px;margin:0}p{margin:8px 0}a{color:var(--navy);font-weight:700}.status{display:inline-block;border:2px solid var(--navy);border-radius:100px;padding:4px 14px;background:var(--cream);font-size:13px;font-weight:800;margin-right:10px}.intro{max-width:940px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:22px}article{background:white;border:2px solid var(--navy);border-radius:20px;overflow:hidden;box-shadow:0 5px 0 #071b5215}.caption{display:flex;align-items:center;justify-content:space-between;gap:10px;padding:15px 18px}.caption a{font-size:13px}.image{display:block;width:100%;border:0;border-top:2px solid var(--navy);padding:0;margin:0;background:var(--sky);cursor:zoom-in}.image img{display:block;width:100%;height:auto}article p{padding:12px 18px 18px;margin:0;font-size:14px;min-height:110px}.review{padding:22px 26px;border:2px solid var(--navy);background:var(--cream);border-radius:18px;margin-top:32px}.review h2{margin:0 0 10px}.review ul{margin:0;padding-left:22px}.palettes{display:flex;flex-wrap:wrap;gap:14px;margin:16px 0}.palette{background:#fff;border:1px solid #c0d9e9;padding:10px 14px;border-radius:12px;font-size:13px}.dot{display:inline-block;width:19px;height:19px;border-radius:50%;vertical-align:middle;border:2px solid var(--navy);margin:0 4px}.red{background:#ff3e43}.blue{background:#0068ff}.purple{background:#8a25f5}.yellow{background:#ffd12b}.orange{background:#ff8c26}.green{background:#38c764}footer{font-size:13px;margin-top:30px}dialog{border:0;padding:0;background:transparent;max-width:95vw;max-height:96vh;overflow:visible}dialog::backdrop{background:#051334dd}dialog img{display:block;max-width:90vw;max-height:92vh;object-fit:contain}dialog button{position:absolute;right:4px;top:4px;background:var(--cream);border:2px solid var(--navy);border-radius:99px;width:44px;height:44px;font-size:25px;cursor:pointer}button:focus-visible,a:focus-visible{outline:4px solid #a31ef5;outline-offset:3px}@media(max-width:780px){main{padding:24px 16px}.grid{grid-template-columns:1fr;max-width:480px;margin:auto}article p{min-height:0}h2{font-size:21px}}
</style></head><body><main><header><div class="eyebrow">COLOR TRACE / 2D B · TOON BOLD / 2026.10.25</div><h1>B案で揃えた、6つの画面イメージ</h1><p><span class="status">認識合わせ・確認用</span>Unityへの反映はこれから</p><p class="intro">決定済みの青空背景・クリーム色・太い濃紺の輪郭線を継承。タイトル、プレイ、クリアの3画面と、プレイ中の3つの状態を並べています。画像をクリックすると拡大できます。</p></header>
<h2>基本の3画面</h2><section class="grid">${cards('main')}</section>
<h2>プレイ中の表示と操作</h2><section class="grid">${cards('state')}</section>
<section class="review"><h2>確認したいポイント</h2><ul><li>全画面の色・文字・輪郭線が、決定したB案の印象に合っているか。</li><li>盤面・ガイド・リセットの大きさと余白が適切か。</li><li>指の案内・不正解マスの表示・色選択カードが分かりやすいか。</li><li>クリア後の縮小盤面と、中央の次ステージボタン1つの構成でよいか。</li></ul></section>
<h2>既存の色ローテーション</h2><p>今回の画面は赤・青・紫の表示例です。実装時は5ステージごとの組み合わせを継承し、開始点・指定枠・混色ガイド・色別リセット・色選択を連動させます。</p><div class="palettes"><div class="palette">Stage 1–5 <span class="dot red"></span> + <span class="dot blue"></span> = <span class="dot purple"></span></div><div class="palette">Stage 6–10 <span class="dot red"></span> + <span class="dot yellow"></span> = <span class="dot orange"></span></div><div class="palette">Stage 11–15 <span class="dot blue"></span> + <span class="dot yellow"></span> = <span class="dot green"></span></div></div><p>以降も15ステージ周期で繰り返します。STAGE 3 / STAGE 4は進行番号の表示例。アプリ名は現行のCOLOR TRACEで仮置きしています。</p>
<footer><a href="ui-refresh-b-review.zip" download>6枚のPNGと確認資料をZIPで保存</a> ／ <a href="overview.png">6枚の一覧画像</a> ／ <a href="README.md">仕様・確認メモ</a> ／ <a href="generation-specs.json">内蔵画像生成の全プロンプト</a><p>参照：<a href="../stage-ui-concepts/SELECTED.md">決定済みB案</a>。この資料は画像による提案です。点滅・指の移動・紙吹雪・SE・端末振動の動作確認は実装時に行います。</p></footer>
</main><dialog id="viewer"><img id="large" alt="拡大した画面イメージ"><button id="close" aria-label="閉じる">×</button></dialog><script>const viewer=document.querySelector('#viewer');document.querySelectorAll('[data-file]').forEach(button=>button.addEventListener('click',()=>{document.querySelector('#large').src=button.dataset.file;document.querySelector('#large').alt=button.getAttribute('aria-label');viewer.showModal()}));document.querySelector('#close').addEventListener('click',()=>viewer.close());viewer.addEventListener('click',event=>{if(event.target===viewer)viewer.close()});</script></body></html>`);

GlobalFonts.registerFromPath('C:/Windows/Fonts/meiryo.ttc', 'Meiryo');
const W = 1620, H = 2110, margin = 46, gap = 26, cw = 492;
const canvas = createCanvas(W, H), ctx = canvas.getContext('2d');
ctx.fillStyle = '#eaf6fc'; ctx.fillRect(0, 0, W, H);
ctx.fillStyle = '#071b52'; ctx.font = 'bold 42px Meiryo'; ctx.fillText('B案 UI刷新｜画面イメージ確認', margin, 64);
ctx.font = '20px Meiryo'; ctx.fillText('2D B / Toon Bold  •  2026.10.25  •  確認用イメージ / Unity未反映', margin, 105);
for (const [i, item] of entries.entries()) {
  const row = Math.floor(i / 3), col = i % 3;
  const x = margin + col * (cw + gap), y = 148 + row * 963;
  ctx.fillStyle = '#ffffff'; ctx.beginPath(); ctx.roundRect(x, y, cw, 934, 15); ctx.fill();
  ctx.fillStyle = '#071b52'; ctx.font = 'bold 24px Meiryo'; ctx.fillText(item.title, x + 18, y + 39);
  const img = await loadImage(path.join(root, item.file));
  const iw = cw - 24, ih = iw * item.height / item.width;
  ctx.drawImage(img, x + 12, y + 58, iw, ih);
}
await fs.writeFile(path.join(root, 'overview.png'), canvas.toBuffer('image/png'));
console.log(JSON.stringify({ images: entries.map(({file,width,height,bytes})=>({file,width,height,bytes})), gallery: 'index.html', overview: 'overview.png' }, null, 2));
