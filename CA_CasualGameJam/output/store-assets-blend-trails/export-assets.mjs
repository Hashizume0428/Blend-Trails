import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import { createHash } from 'node:crypto';

const require = createRequire(import.meta.url);
const sharp = require('C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp/dist/index.cjs');
const root = path.dirname(fileURLToPath(import.meta.url));
const specs = JSON.parse(await fs.readFile(path.join(root, 'generation-specs.json'), 'utf8'));
const sources = JSON.parse(await fs.readFile(path.join(root, 'generation-sources.json'), 'utf8'));
const records = [];

// Technical exports only: scale to the specified dimensions and compress the AI artwork.
async function writeExport(job, source, width, height, relative, alpha) {
  const original = await sharp(source).metadata();
  const ratioError = Math.abs((original.width / original.height) / (width / height) - 1);
  if (ratioError > 0.025) throw new Error(`${job.id}: source aspect ratio needs visual review`);
  let pipeline = sharp(source).rotate().resize(width, height, { fit: 'cover', position: 'centre', kernel: 'lanczos3' }).toColourspace('srgb');
  pipeline = alpha ? pipeline.ensureAlpha(1) : pipeline.flatten({ background: '#28B3E8' }).removeAlpha();
  const raw = await pipeline.raw().toBuffer({ resolveWithObject: true });
  const encode = data => sharp(data.data, { raw: data.info }).png({ compressionLevel: 9, adaptiveFiltering: true, palette: false }).toBuffer();
  let bytes = await encode(raw);
  let reducedColours = null;
  if (alpha && bytes.length > 1000000) {
    for (const colours of [256, 192, 128, 96, 64]) {
      const compact = await sharp(raw.data, { raw: raw.info }).png({ palette: true, colours, dither: 0, effort: 10 }).toBuffer();
      const decoded = await sharp(compact).ensureAlpha(1).raw().toBuffer({ resolveWithObject: true });
      bytes = await encode(decoded);
      reducedColours = colours;
      if (bytes.length <= 1000000) break;
    }
  }
  if (alpha && bytes.length > 1000000) throw new Error(`${relative}: over 1 MB`);
  const destination = path.join(root, relative);
  await fs.mkdir(path.dirname(destination), { recursive: true });
  await fs.writeFile(destination, bytes);
  const info = await sharp(bytes).metadata();
  if (info.width !== width || info.height !== height || info.format !== 'png' || info.channels !== (alpha ? 4 : 3) || info.hasAlpha !== alpha || info.bitsPerSample !== 8) throw new Error(`Invalid export ${relative}`);
  const record = { concept: job.id.slice(0, 2), name: job.name, type: job.type, file: relative, width, height, bytes: bytes.length, format: 'png', channels: info.channels, alpha: info.hasAlpha, bitDepth: info.bitsPerSample, colourSpace: info.space, reducedColours, sourceDimensions: [original.width, original.height], sha256: createHash('sha256').update(bytes).digest('hex') };
  records.push(record);
  console.log(JSON.stringify(record));
}

await fs.mkdir(path.join(root, 'originals'), { recursive: true });
for (const job of specs.jobs) {
  const source = sources[job.id];
  if (!source) throw new Error(`Missing source ${job.id}`);
  await fs.copyFile(source, path.join(root, 'originals', `${job.id}.png`));
  const stem = job.id.replace(/-(icon|feature)$/, '');
  if (job.type === 'icon') {
    await writeExport(job, source, 1024, 1024, `icons/${stem}-1024.png`, true);
    await writeExport(job, source, 512, 512, `google-play/icons/${stem}-512.png`, true);
  } else {
    await writeExport(job, source, 1024, 500, `feature-graphics/${stem}-1024x500.png`, false);
  }
}
await fs.writeFile(path.join(root, 'manifest.json'), JSON.stringify({ date: specs.date, game: specs.game, method: specs.method, exports: records }, null, 2));

const concepts = [
  { id: '01', letter: 'A', title: 'Sunshine Mix', ja: 'タイトルと揃える混色シンボル', detail: '赤＋黄→オレンジ。タイトルと共通のモチーフで、ブランドの覚えやすさを重視。推奨案。' },
  { id: '02', letter: 'B', title: 'Puzzle Cross', ja: '盤面で遊び方を伝える', detail: '赤＋青→紫。アイコンは盤面の一部を簡略化、横長画像はStage3の5列×4行の経路を表現。' },
  { id: '03', letter: 'C', title: 'Fresh Trails', ja: '太い経路と鮮やかな混色', detail: '青＋黄→緑。アイコンでは共有する経路、横長画像では中央ロゴと交差する経路を強調。' }
];
const asset = (id, type, width) => records.find(x => x.concept === id && x.type === type && (!width || x.width === width));
const kb = x => `${(x.bytes / 1000).toFixed(0)} KB`;
const cards = concepts.map(c => {
  const icon = asset(c.id, 'icon', 1024), play = asset(c.id, 'icon', 512), feature = asset(c.id, 'feature');
  return `<article><header><span>${c.letter}</span><div><h2>${c.title}</h2><p>${c.ja}</p></div></header><a class="art icon" href="${icon.file}" target="_blank"><img src="${icon.file}" alt="${c.title}のアイコン" width="1024" height="1024"></a><p class="meta">1024×1024 / PNG / ${kb(icon)}</p><div class="links"><a href="${icon.file}" download>1024px PNG</a><a href="${play.file}" download>入稿用512px PNG</a></div><div class="small"><img src="${icon.file}" width="48" height="48" alt="48px表示"><img src="${icon.file}" width="64" height="64" alt="64px表示"><img src="${icon.file}" width="96" height="96" alt="96px表示"><p>48 / 64 / 96px</p></div><a class="art feature" href="${feature.file}" target="_blank"><img src="${feature.file}" width="1024" height="500" alt="${c.title}のフィーチャーグラフィック"></a><p class="meta">1024×500 / 24-bit PNG / ${kb(feature)}</p><div class="links"><a href="${feature.file}" download>フィーチャーグラフィック PNG</a></div><p class="detail">${c.detail}</p></article>`;
}).join('');
const html = `<!doctype html><html lang="ja"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blend Trails — Store Creative</title><style>*{box-sizing:border-box}body{margin:0;background:#edf5fa;color:#0b2047;font-family:Arial,'Yu Gothic UI',sans-serif}main{max-width:1500px;margin:auto;padding:36px 26px}h1{font-size:36px;letter-spacing:-.025em;margin:12px 0}p{line-height:1.7}a{color:#125aab}.eyebrow{font-weight:800;letter-spacing:.12em;font-size:11px;color:#1575c8}.intro{color:#4b607b;font-size:14px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:22px;margin:30px 0}article{background:white;padding:22px;border:1px solid #d9e5ee;border-radius:18px}article header{display:flex;gap:14px;align-items:center;margin-bottom:24px}article header span{font-weight:900;font-size:34px;color:#1684d1}h2{font-size:21px;margin:0}header p{font-size:12px;margin:4px 0 0;color:#4b607b}.art img{display:block;width:100%;height:auto}.icon img{width:260px;max-width:100%;margin:auto}.meta{font-size:12px;text-align:center;color:#526881;margin:12px 0}.links{display:flex;gap:8px;justify-content:center;flex-wrap:wrap}.links a{border:1px solid #c7d9e8;border-radius:7px;padding:8px 10px;text-decoration:none;font-size:12px}.small{display:flex;gap:12px;align-items:center;justify-content:center;margin:24px 0;padding-top:20px;border-top:1px solid #e5eef5}.small img{border-radius:23%}.small p{font-size:10px;color:#526881}.detail{font-size:12px;color:#526881;border-top:1px solid #e5eef5;margin-top:20px;padding-top:14px}.button{display:inline-block;background:#102957;color:white;padding:12px 18px;text-decoration:none;border-radius:9px;font-size:14px}footer{font-size:12px;color:#526881;line-height:1.9}@media(max-width:1100px){.grid{grid-template-columns:1fr}article{max-width:760px;width:100%;margin:auto}h1{font-size:28px}}@media print{.links,.button{display:none}.grid{grid-template-columns:repeat(3,1fr)}.small{display:none}main{padding:0}}</style></head><body><main><div class="eyebrow">BLEND TRAILS: COLOR PUZZLE / STORE CREATIVE / 2026.10.25</div><h1>アイコン & フィーチャーグラフィック 3案</h1><p class="intro">現在のToon Boldデザイン・2色の経路と混色をもとに制作。画像をクリックすると原寸で開きます。<br>アイコン：1024×1024 PNG・各1,000,000バイト以下 ／ フィーチャー：1024×500・透過なしPNG</p><a class="button" href="blend-trails-store-assets.zip" download>素材一式 ZIP</a><div class="grid">${cards}</div><footer>Google Play入稿用：512×512・32-bit PNGのアイコンを別途同梱。<br>参考：<a href="https://play.google.com/store/apps/details/Flow_Free?hl=en&id=com.bigduckgames.flow&gl=US">Flow Free</a> / <a href="https://play.google.com/store/apps/details?id=com.GybeGames.ColorBlockJam&hl=en_US&gl=US">Color Block Jam</a> / <a href="https://play.google.com/store/apps/details?id=net.peakgames.toonblast&hl=en_US&gl=US">Toon Blast</a> / <a href="https://support.google.com/googleplay/android-developer/answer/9866151?hl=en">Google Play公式素材規格</a><br>制作方式：内蔵image_gen。<a href="generation-specs.json">全6点の生成プロンプト</a> / <a href="manifest.json">寸法・容量の検証記録</a> / <a href="README.md">構成意図・参考資料</a></footer></main></body></html>`;
await fs.writeFile(path.join(root, 'index.html'), html);

// Comparison board: presentation labels and scaled previews, not additional game artwork.
const W = 1680, H = 1170, col = 520, gap = 24, left = 36;
let svg = `<svg width="${W}" height="${H}" xmlns="http://www.w3.org/2000/svg"><rect width="100%" height="100%" fill="#edf5fa"/><g font-family="Arial,sans-serif" fill="#10254b"><text x="36" y="51" font-size="16" font-weight="700" letter-spacing="2">BLEND TRAILS: COLOR PUZZLE</text><text x="36" y="96" font-size="34" font-weight="700">Store creative / 3 directions</text><text x="1644" y="51" text-anchor="end" font-size="14" fill="#526881">2026.10.25</text>`;
for (let i = 0; i < concepts.length; i++) {
  const c = concepts[i], x = left + i * (col + gap), icon = asset(c.id, 'icon', 1024), feat = asset(c.id, 'feature');
  svg += `<rect x="${x}" y="128" width="${col}" height="990" rx="18" fill="white" stroke="#d4e3ef"/><text x="${x+24}" y="175" font-size="29" font-weight="700" fill="#0878c1">${c.letter}</text><text x="${x+66}" y="175" font-size="25" font-weight="700">${c.title}</text><text x="${x+col/2}" y="565" text-anchor="middle" font-size="14" fill="#526881">ICON 1024 x 1024 PNG / ${kb(icon)}</text><text x="${x+col/2}" y="730" text-anchor="middle" font-size="13" fill="#526881">48 / 64 / 96px small-size preview</text><text x="${x+col/2}" y="1040" text-anchor="middle" font-size="14" fill="#526881">FEATURE 1024 x 500 PNG / ${kb(feat)}</text><text x="${x+col/2}" y="1080" text-anchor="middle" font-size="14">${i===0?'RED + YELLOW = ORANGE':i===1?'RED + BLUE = PURPLE':'BLUE + YELLOW = GREEN'}</text>`;
}
svg += '<text x="36" y="1150" font-size="13" fill="#526881">Requested 1024px masters + Google Play 512px icons included. Built-in image_gen artwork; dimensions and file sizes verified.</text></g></svg>';
const layers = [];
for (let i=0; i<concepts.length; i++) {
  const c=concepts[i], x=left+i*(col+gap), icon=path.join(root,asset(c.id,'icon',1024).file), feat=path.join(root,asset(c.id,'feature').file);
  layers.push({ input: await sharp(icon).resize(340,340).toBuffer(), left:x+90, top:200 });
  let sx=x+112;
  for(const size of [48,64,96]) { layers.push({input:await sharp(icon).resize(size,size).toBuffer(),left:sx,top:600+Math.round((96-size)/2)});sx+=size+22; }
  layers.push({input:await sharp(feat).resize(480,234).toBuffer(),left:x+20,top:774});
}
await sharp(Buffer.from(svg)).composite(layers).png().toFile(path.join(root,'comparison.png'));
console.log('Exported 9 verified PNG files, comparison.png, and index.html.');
