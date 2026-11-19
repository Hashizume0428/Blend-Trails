import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const { createCanvas, loadImage, GlobalFonts } = require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const sharp = require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root = path.dirname(fileURLToPath(import.meta.url));
const kinds = [
  { id: '01-title', label: '01 タイトル', note: 'ロゴ・混色モチーフと、現在のステージへ進むボタン。' },
  { id: '02-gameplay', label: '02 通常プレイ', note: '開始点、指定色の四角い枠、混色ガイド、色別リセットを比較。' },
  { id: '04-incorrect', label: '04 不正解表示', note: '誤りマスの面と指定枠を透明度で点滅。画像は点滅中の一瞬。' },
  { id: '05-resume-color', label: '05 線の再開・色選択', note: '混色の終点から再開する色を選択。黄色の文字を濃紺にして読みやすくする。' },
  { id: '06-clear', label: '06 クリア', note: '小さくした完成盤面と次ステージボタン1つ。紙吹雪も配色に合わせる。' },
];
const palettes = [
  { id: 'red-blue', label: '赤＋青＝紫', colors: ['#ff3e43','#0068ff','#8a25f5'], stages: 'Stage 1–5', new: false },
  { id: 'red-yellow', label: '赤＋黄＝オレンジ', colors: ['#ff3e43','#ffd42a','#ff8c28'], stages: 'Stage 6–10', new: true },
  { id: 'blue-yellow', label: '青＋黄＝緑', colors: ['#0068ff','#ffd42a','#32bf69'], stages: 'Stage 11–15', new: true },
];

await fs.mkdir(path.join(root, 'red-blue'), { recursive: true });
await fs.mkdir(path.join(root, 'shared'), { recursive: true });
for (const kind of kinds) await fs.copyFile(path.join(root, '..', kind.id + '.png'), path.join(root, 'red-blue', kind.id + '.png'));
await fs.copyFile(path.join(root, '..', '03-tutorial.png'), path.join(root, 'shared', '03-tutorial.png'));
const entries = [];
for (const palette of palettes) for (const kind of kinds) {
  const file = `${palette.id}/${kind.id}.png`;
  const buffer = await fs.readFile(path.join(root, file)), meta = await sharp(buffer).metadata();
  if (Math.abs(meta.width / meta.height - 9 / 16) > .025) throw new Error(`Unexpected aspect: ${file}`);
  entries.push({ palette: palette.id, screen: kind.id, file, new: palette.new, width: meta.width, height: meta.height, bytes: buffer.length, sha256: crypto.createHash('sha256').update(buffer).digest('hex') });
}
const tutorial = await fs.readFile(path.join(root, 'shared/03-tutorial.png'));
entries.push({ palette: 'shared', screen: '03-tutorial', file: 'shared/03-tutorial.png', new: false, bytes: tutorial.length, sha256: crypto.createHash('sha256').update(tutorial).digest('hex') });
await fs.writeFile(path.join(root, 'manifest.json'), JSON.stringify({ date: '2026-10-25', mode: 'built-in image_gen', newImages: 10, comparisonImages: 5, sharedTutorialImages: 1, entries }, null, 2));

const escape = value => String(value).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
const swatches = palette => palette.colors.map((color, i) => `${i === 1 ? ' + ' : i === 2 ? ' = ' : ''}<i style="background:${color}"></i>`).join('');
const cards = kind => palettes.map(palette => `<article><header><h3>${escape(palette.label)}</h3><span>${palette.new ? '追加' : '前回'} · ${palette.stages}</span><div class="formula">${swatches(palette)}</div></header><button data-file="${palette.id}/${kind.id}.png" aria-label="${escape(palette.label + ' ' + kind.label)}を拡大"><img src="${palette.id}/${kind.id}.png" alt="${escape(palette.label + ' ' + kind.label)}" width="941" height="1672" loading="lazy"></button><a class="download" href="${palette.id}/${kind.id}.png" download>PNGを保存 ↓</a></article>`).join('');
const sections = kinds.map(kind => `<section id="${kind.id}"><h2>${kind.label}</h2><p>${escape(kind.note)}</p><div class="grid">${cards(kind)}</div></section>`).join('');
await fs.writeFile(path.join(root, 'index.html'), `<!DOCTYPE html><html lang="ja"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>B案・3配色の画面比較</title><style>
:root{--ink:#071b52;--cream:#fff6d8}*{box-sizing:border-box}html{scroll-behavior:smooth;scroll-padding-top:90px}body{margin:0;color:var(--ink);background:#eaf6fc;font-family:"Yu Gothic UI",Meiryo,sans-serif;line-height:1.7}main{max-width:1420px;margin:auto;padding:38px 28px 64px}h1{font-size:clamp(28px,3vw,42px);margin:8px 0 12px;line-height:1.3}.eyebrow{font-weight:800;font-size:12px;letter-spacing:.12em}p{margin:8px 0 18px}a{color:var(--ink);font-weight:700}.lead{max-width:1000px}.badge{display:inline-block;border:2px solid var(--ink);border-radius:99px;background:var(--cream);padding:3px 13px;font-weight:800;font-size:13px}.actions{display:flex;flex-wrap:wrap;gap:12px;margin:18px 0 20px}.actions a{background:white;border:2px solid var(--ink);border-radius:12px;padding:9px 14px;text-decoration:none}.actions a:first-child{background:var(--cream)}nav{position:sticky;top:0;z-index:2;display:flex;gap:9px;overflow:auto;background:#eaf6fcf2;border-bottom:2px solid #071b5240;padding:13px 0;backdrop-filter:blur(10px)}nav a{white-space:nowrap;text-decoration:none;font-size:14px;border:1px solid #071b5240;border-radius:99px;padding:5px 13px;background:white}section{margin-top:32px}h2{margin:0 0 6px;font-size:25px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:22px}article{border:2px solid var(--ink);border-radius:18px;background:white;overflow:hidden;box-shadow:0 4px 0 #071b5215}article header{padding:15px 17px 11px}h3{font-size:20px;margin:0}article header span{font-size:12px;color:#49627c}.formula{font-weight:800;margin-top:6px}.formula i{display:inline-block;width:22px;height:22px;border:2px solid var(--ink);border-radius:50%;vertical-align:middle}article button{display:block;border:0;border-top:2px solid var(--ink);padding:0;width:100%;cursor:zoom-in;background:#2eb9ef}article img{display:block;width:100%;height:auto}.download{display:block;padding:10px 16px;font-size:13px}.note{margin-top:30px;padding:20px 24px;border:2px solid var(--ink);border-radius:16px;background:var(--cream)}.tutorial{max-width:320px;display:block;margin-top:14px}.tutorial img{width:100%;height:auto;border:2px solid var(--ink);border-radius:13px}footer{border-top:2px solid #071b5240;margin-top:32px;padding-top:20px;font-size:13px}dialog{border:0;padding:0;background:transparent;max-width:95vw;max-height:96vh;overflow:visible}dialog::backdrop{background:#051334dd}dialog img{display:block;max-width:90vw;max-height:92vh;object-fit:contain}dialog button{position:absolute;right:5px;top:5px;width:44px;height:44px;border-radius:99px;background:var(--cream);border:2px solid var(--ink);font-size:25px;cursor:pointer}a:focus-visible,button:focus-visible{outline:4px solid #8a25f5;outline-offset:3px}@media(max-width:800px){main{padding:24px 15px}.grid{grid-template-columns:1fr;max-width:470px;margin:auto}h3{font-size:20px}nav{margin:0 -15px;padding:10px 15px}}
</style></head><body><main><div class="eyebrow">COLOR TRACE / 2D B · TOON BOLD / 2026.10.25</div><h1>「赤＋黄」「青＋黄」を追加</h1><p><span class="badge">新規10枚・内蔵画像生成</span>　認識合わせ用 / Unity未反映</p><p class="lead">B案の青空・クリーム・濃紺と画面構成を継承し、ゲームに使う色を切り替えました。前回の「赤＋青」と横並びで、開始点・指定枠・混色・リセット・色選択を確認できます。</p><div class="actions"><a href="palette-variants.zip" download>画像・確認資料一式を保存 ↓</a><a href="gameplay-comparison.png">通常プレイ3配色の一覧</a><a href="overview.png">追加2配色の主要画面一覧</a><a href="generation-specs.json">生成プロンプト</a></div><nav aria-label="画面の種類">${kinds.map(kind => `<a href="#${kind.id}">${kind.label}</a>`).join('')}<a href="#tutorial">共通チュートリアル</a></nav>${sections}
<section id="tutorial" class="note"><h2>03 チュートリアルは共通</h2><p>指ガイドはStage1専用のため、前回の「赤＋青」を使用します。黄色を使うStage1の案内や、後続ステージへのチュートリアル追加は行っていません。</p><a class="tutorial" href="shared/03-tutorial.png"><img src="shared/03-tutorial.png" width="941" height="1672" alt="Stage1専用の赤青チュートリアル" loading="lazy"></a></section>
<div class="note"><p><strong>表示例について：</strong>配色だけを比較しやすいよう、盤面配置・経路は前回のStage3の例で統一しています。見出しのSTAGE 6 / STAGE 11は配色区間の表示例で、実際の問題6・11の盤面を示すものではありません。次ステージはそれぞれSTAGE 7 / STAGE 12です。</p><p>黄色のリセット矢印・選択ボタン文字は濃紺にしています。青空背景・HOME・次へ進む青いボタンは共通のUI色として維持します。</p></div><footer><a href="../index.html">前回の6画面へ</a> ／ <a href="README.md">確認メモ</a> ／ <a href="manifest.json">画像の寸法・ハッシュ</a><p>ZIPには新規10枚、比較用の既存5枚、共通チュートリアル1枚と資料を収録。画像は未承認の提案です。</p></footer></main><dialog id="viewer"><img id="large" alt="画面の拡大表示"><button id="close" aria-label="閉じる">×</button></dialog><script>const viewer=document.querySelector('#viewer');document.querySelectorAll('[data-file]').forEach(button=>button.addEventListener('click',()=>{document.querySelector('#large').src=button.dataset.file;document.querySelector('#large').alt=button.getAttribute('aria-label');viewer.showModal()}));document.querySelector('#close').addEventListener('click',()=>viewer.close());viewer.addEventListener('click',event=>{if(event.target===viewer)viewer.close()});</script></body></html>`);

GlobalFonts.registerFromPath('C:/Windows/Fonts/meiryo.ttc', 'Meiryo');
const navy='#071b52', bg='#eaf6fc';
async function comparisonSheet(filename, columns, rows, title, subtitle) {
  const cw=470, gap=24, margin=38, top=150, ch=925;
  const canvas=createCanvas(margin*2 + columns.length*cw + (columns.length-1)*gap, top+rows.length*ch+25), ctx=canvas.getContext('2d');
  ctx.fillStyle=bg;ctx.fillRect(0,0,canvas.width,canvas.height);
  ctx.fillStyle=navy;ctx.font='bold 34px Meiryo';ctx.fillText(title,margin,55);
  ctx.font='18px Meiryo';ctx.fillText(subtitle,margin,92);
  for(let row=0;row<rows.length;row++)for(let col=0;col<columns.length;col++){
    const item=rows[row][col], x=margin+col*(cw+gap), y=top+row*ch;
    ctx.fillStyle='#ffffff';ctx.beginPath();ctx.roundRect(x,y,cw,ch-20,14);ctx.fill();
    ctx.fillStyle=navy;ctx.font='bold 23px Meiryo';ctx.fillText(item.label,x+15,y+35);
    const img=await loadImage(path.join(root,item.file));
    ctx.drawImage(img,x+10,y+55,cw-20,(cw-20)*img.height/img.width);
  }
  await fs.writeFile(path.join(root,filename),canvas.toBuffer('image/png'));
}
await comparisonSheet('gameplay-comparison.png',palettes,[palettes.map(p => ({file:`${p.id}/02-gameplay.png`,label:p.label}))], 'B案｜通常プレイ・3配色の比較', '左：前回の赤＋青　 中：赤＋黄＝オレンジ　 右：青＋黄＝緑　 / Unity未反映');
const selectedKinds=kinds.filter(k => ['02-gameplay','05-resume-color','06-clear'].includes(k.id));
await comparisonSheet('overview.png',selectedKinds,palettes.filter(p=>p.new).map(p=>selectedKinds.map(k=>({file:`${p.id}/${k.id}.png`,label:`${p.label} / ${k.label.split(' ')[1]}`}))), 'B案｜追加2配色・主要画面', '上：赤＋黄＝オレンジ　 下：青＋黄＝緑　 / 各5画面・計10枚を生成 / Unity未反映');
const teaser='<aside class="review"><strong>追加画像：</strong><a href="palette-variants/index.html">「赤＋黄」「青＋黄」を含む3配色の画面比較へ →</a></aside>\n';
for (const file of ['index.html','build-review.mjs']) {
  const target=path.join(root,'..',file), original=await fs.readFile(target,'utf8');
  if (!original.includes('palette-variants/index.html')) {
    const next=original.replace('<h2>基本の3画面</h2>',teaser+'<h2>基本の3画面</h2>');
    if (next===original) throw new Error(`Missing root gallery anchor: ${file}`);
    await fs.writeFile(target,next);
  }
}
console.log(JSON.stringify({newImages:10,comparisonImages:5,sharedTutorialImages:1,gallery:'index.html',sheets:['gameplay-comparison.png','overview.png']},null,2));
