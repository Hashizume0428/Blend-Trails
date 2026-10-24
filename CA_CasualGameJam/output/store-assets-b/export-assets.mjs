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
const manifest = [];
const MAX_ICON_BYTES = 1000000;

async function writeExport(job, source, width, height, relative, rgba) {
  const sourceMeta = await sharp(source).metadata();
  const difference = Math.abs((sourceMeta.width / sourceMeta.height) / (width / height) - 1);
  if (difference > 0.025) throw new Error(`${job.id}: unexpected source aspect ratio ${sourceMeta.width}x${sourceMeta.height}; review before cropping`);
  let pipeline = sharp(source).rotate().resize(width, height, { fit: 'cover', position: 'centre', kernel: 'lanczos3' }).toColourspace('srgb');
  pipeline = rgba ? pipeline.ensureAlpha(1) : pipeline.flatten({ background: '#2EB9EF' }).removeAlpha();
  const raw = await pipeline.raw().toBuffer({ resolveWithObject: true });
  const encode = input => sharp(input.data, { raw: input.info }).png({ compressionLevel: 9, adaptiveFiltering: true, palette: false }).toBuffer();
  let output = await encode(raw);
  let quantized = false;
  if (rgba && output.length > MAX_ICON_BYTES) {
    for (const colours of [256, 192, 128, 96, 64]) {
      const palette = await sharp(raw.data, { raw: raw.info }).png({ palette: true, colours, dither: 0, effort: 10 }).toBuffer();
      const reduced = await sharp(palette).ensureAlpha(1).raw().toBuffer({ resolveWithObject: true });
      output = await encode(reduced);
      quantized = true;
      if (output.length <= MAX_ICON_BYTES) break;
    }
  }
  if (rgba && output.length > MAX_ICON_BYTES) throw new Error(`${relative}: icon exceeds 1 MB`);
  const destination = path.join(root, relative);
  await fs.mkdir(path.dirname(destination), { recursive: true });
  await fs.writeFile(destination, output);
  const metadata = await sharp(output).metadata();
  if (metadata.width !== width || metadata.height !== height || metadata.format !== 'png') throw new Error(`Invalid export: ${relative}`);
  if (metadata.hasAlpha !== rgba || metadata.channels !== (rgba ? 4 : 3)) throw new Error(`Invalid PNG channel layout: ${relative}`);
  const record = {
    concept: job.id.slice(0, 2), name: job.name, type: job.type, file: relative.replaceAll('\\', '/'),
    width, height, bytes: output.length, format: metadata.format, colourSpace: metadata.space,
    channels: metadata.channels, alpha: metadata.hasAlpha, bitDepth: metadata.bitsPerSample,
    sha256: createHash('sha256').update(output).digest('hex'), colourReduction: quantized,
    sourceDimensions: [sourceMeta.width, sourceMeta.height]
  };
  manifest.push(record);
  console.log(JSON.stringify(record));
}

await fs.mkdir(path.join(root, 'originals'), { recursive: true });
for (const job of specs.jobs) {
  const source = sources[job.id];
  if (!source) throw new Error(`Missing source: ${job.id}`);
  await fs.copyFile(source, path.join(root, 'originals', `${job.id}.png`));
  const stem = job.id.replace(/-(icon|feature)$/, '');
  if (job.type === 'icon') {
    await writeExport(job, source, 1024, 1024, `icons/${stem}-1024.png`, true);
    await writeExport(job, source, 512, 512, `google-play/icons/${stem}-512.png`, true);
  } else {
    await writeExport(job, source, 1024, 500, `feature-graphics/${stem}-1024x500.png`, false);
  }
}
await fs.writeFile(path.join(root, 'manifest.json'), JSON.stringify({ date: specs.date, method: specs.method, selectedDesign: specs.selectedDesign, exports: manifest }, null, 2));
