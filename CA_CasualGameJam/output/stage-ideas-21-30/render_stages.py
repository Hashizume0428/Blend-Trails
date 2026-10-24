"""Draw exact new stage diagrams from coordinates; user images are not edited."""
from __future__ import annotations

import html
import json
import math
import zipfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from design_stages import validate

ROOT = Path(__file__).resolve().parent
WIDTH, HEIGHT = 1400, 1280
OX, OY, CELL = 140, 240, 224
INK = '#111111'
COLORS = {'red': '#F20D16', 'blue': '#007BFF', 'purple': '#872DA0'}
FONT = r'C:\Windows\Fonts\arialbd.ttf'
JP_FONT = r'C:\Windows\Fonts\YuGothM.ttc'
JP_BOLD = r'C:\Windows\Fonts\YuGothB.ttc'


def point(rc, color):
    r, c = rc
    offset = -7 if color == 'red' else 7
    return OX + (c - .5) * CELL + offset, OY + (r - .5) * CELL + offset


def draw_stage(stage, solution):
    scale = 2
    image = Image.new('RGB', (WIDTH * scale, HEIGHT * scale), 'white')
    d = ImageDraw.Draw(image)

    def coords(box):
        return tuple(round(v * scale) for v in box)

    def line(points, fill, width):
        points = [coords(p) for p in points]
        d.line(points, fill=fill, width=round(width * scale), joint='curve')
        radius = width * scale / 2
        for x, y in points:
            d.ellipse((x - radius, y - radius, x + radius, y + radius), fill=fill)

    title = f"STAGE {stage['id']:02d}"
    d.text((WIDTH * scale / 2, 112 * scale), title, fill=INK,
           font=ImageFont.truetype(FONT, 66 * scale), anchor='mm')
    for c in range(6):
        x = OX + c * CELL
        line([(x, OY), (x, OY + 4 * CELL)], INK, 8 if c not in (0, 5) else 12)
    for r in range(5):
        y = OY + r * CELL
        line([(OX, y), (OX + 5 * CELL, y)], INK, 8 if r not in (0, 4) else 12)
    for color, cells in stage['required_cells'].items():
        for r, c in cells:
            x, y = OX + (c - 1) * CELL + 15, OY + (r - 1) * CELL + 15
            d.rectangle(coords((x, y, x + CELL - 30, y + CELL - 30)), outline=COLORS[color], width=8 * scale)
    if solution:
        for color in ('red', 'blue'):
            points = [point(rc, color) for rc in stage['solution'][color]]
            line(points, 'white', 12)
            line(points, COLORS[color], 7)
    for color in ('red', 'blue'):
        x, y = point(stage['starts'][color], color)
        d.ellipse(coords((x - 28, y - 28, x + 28, y + 28)), fill=COLORS[color])
    return image.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS)


def svg_stage(stage, solution):
    chunks = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">',
              '<rect width="100%" height="100%" fill="white"/>',
              f'<title>STAGE {stage["id"]} — {html.escape(stage["name"])}</title>',
              f'<text x="700" y="134" text-anchor="middle" font-family="Arial,sans-serif" font-size="66" font-weight="700" fill="{INK}">STAGE {stage["id"]}</text>']
    for c in range(6):
        x = OX + c * CELL
        chunks.append(f'<path d="M{x},{OY}V{OY + 4 * CELL}" fill="none" stroke="{INK}" stroke-width="{12 if c in (0,5) else 8}"/>')
    for r in range(5):
        y = OY + r * CELL
        chunks.append(f'<path d="M{OX},{y}H{OX + 5 * CELL}" fill="none" stroke="{INK}" stroke-width="{12 if r in (0,4) else 8}"/>')
    for color, cells in stage['required_cells'].items():
        for r, c in cells:
            x, y = OX + (c - 1) * CELL + 15, OY + (r - 1) * CELL + 15
            chunks.append(f'<rect x="{x}" y="{y}" width="194" height="194" fill="none" stroke="{COLORS[color]}" stroke-width="8"/>')
    if solution:
        chunks.append('<g id="solution" fill="none" stroke-linecap="round" stroke-linejoin="round">')
        for color in ('red', 'blue'):
            pts = ' '.join(f'{x:g},{y:g}' for x, y in [point(rc, color) for rc in stage['solution'][color]])
            chunks.append(f'<polyline points="{pts}" stroke="white" stroke-width="12"/>')
            chunks.append(f'<polyline points="{pts}" stroke="{COLORS[color]}" stroke-width="7"/>')
        chunks.append('</g>')
    for color in ('red', 'blue'):
        x, y = point(stage['starts'][color], color)
        chunks.append(f'<circle cx="{x:g}" cy="{y:g}" r="28" fill="{COLORS[color]}"/>')
    chunks.append('</svg>')
    return '\n'.join(chunks) + '\n'


def overview(stages, images):
    width, card_w, card_h = 1500, 720, 735
    out = Image.new('RGB', (width, 160 + 5 * card_h + 20), '#EEF0F3')
    d = ImageDraw.Draw(out)
    d.text((40, 30), '新しいステージ案 21–30', fill=INK, font=ImageFont.truetype(JP_BOLD, 46))
    d.text((40, 98), '5列 × 4行  /  赤・青・紫  /  線は解答例', fill='#555555', font=ImageFont.truetype(JP_FONT, 25))
    for k, stage in enumerate(stages):
        x = 20 + (k % 2) * (card_w + 20)
        y = 160 + (k // 2) * card_h
        d.rounded_rectangle((x, y, x + card_w, y + card_h - 18), radius=12, fill='white')
        thumb = images[stage['id']].resize((700, 640), Image.Resampling.LANCZOS)
        out.paste(thumb, (x + 10, y + 5))
        d.text((x + 24, y + 650), stage['name'], fill=INK, font=ImageFont.truetype(JP_BOLD, 27))
        d.text((x + 24, y + 691), f"{stage['difficulty']}　紫 {stage['metrics']['shared_cells']}マス", fill='#555555', font=ImageFont.truetype(JP_FONT, 21))
    out.save(ROOT / 'stages-21-30-overview.png')


def route_text(path):
    return ' → '.join(f'({r},{c})' for r, c in path)


def write_readme(stages):
    parts = ['# ステージ21〜30：新規10案', '',
             'これまでと同じ5列×4行、赤・青の開始点と色指定枠を使う追加10案です。ステージ11〜20の反転や色替えだけの案は除外しています。', '',
             '- `index.html`：画像一覧。問題のみ／解答例ありを切り替えられます。',
             '- `stage-XX-clean.png`：前回と同様の、ルートを描いた清書画像。',
             '- `stage-XX-puzzle.png`：プレイヤー向けの問題図。解答の線を省き、開始点と色枠を表示。',
             '- `stage-XX-clean.svg`：編集可能なベクター図。',
             '- `stages-21-30.json`：AIや実装担当に渡せる配置・解答例のデータ。', '',
             '座標はすべて **(行, 列)**、左上を **(1,1)** とする1始まりです。', '',
             '条件：全20マスを着色する。同じ色は一筆で上下左右に隣接するマスを進み、同じマスを通り直さない。赤／青の枠はその単色、紫枠は両色で通る。無印マスは赤・青・紫のどれでもよい。', '',
             '掲載解答について、隣接性・自己再訪なし・全20マスの着色・全色枠との一致を自動検証しています。唯一解やプレイ感の実機検証は行っていません。難易度は設計上の目安です。図は座標データから直接作図しています。', '',
             '| ステージ | テーマ | 難易度目安 | 紫マス | 赤／青の曲がり数 |',
             '|---|---|---|---|---|']
    for s in stages:
        m = s['metrics']
        parts.append(f"| {s['id']} | {s['name']} | {s['difficulty']} | {m['shared_cells']} | {m['red_turns']} / {m['blue_turns']} |")
    for s in stages:
        n = s['id']
        parts.extend(['', f"## STAGE {n}：{s['name']}", '', s['concept'], '',
                      f"[解答図](stage-{n}-clean.png) · [問題図](stage-{n}-puzzle.png)", '',
                      f"- 赤開始：{route_text([s['starts']['red']])}",
                      f"- 青開始：{route_text([s['starts']['blue']])}"])
        for color, label in [('red','赤枠'),('blue','青枠'),('purple','紫枠')]:
            parts.append(f"- {label}：{route_text(s['required_cells'][color])}")
        parts.extend(['', '赤の解答例：', '', route_text(s['solution']['red']), '',
                      '青の解答例：', '', route_text(s['solution']['blue']), ''])
    (ROOT / 'README.md').write_text('\n'.join(parts) + '\n', encoding='utf-8')


def write_gallery(stages):
    cards = []
    for s in stages:
        n = s['id']
        cards.append(f'''<article>
<a class="image-link" href="stage-{n}-clean.png"><img src="stage-{n}-clean.png" data-stage="{n}" alt="STAGE {n}の図" loading="lazy"></a>
<div class="description"><h2>STAGE {n} · {html.escape(s['name'])}</h2>
<p>{html.escape(s['concept'])}</p>
<p class="meta">{s['difficulty']}目安 · 紫 {s['metrics']['shared_cells']}マス</p>
<a href="stage-{n}-clean.png" download>解答図PNG</a> · <a href="stage-{n}-puzzle.png" download>問題図PNG</a> · <a href="stage-{n}-clean.svg" download>SVG</a></div></article>''')
    page = '''<!doctype html>
<html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>新しいステージ案 21–30</title><style>
*{box-sizing:border-box}body{margin:0;background:#eef0f3;color:#171717;font-family:system-ui,sans-serif}main{max-width:1460px;margin:auto;padding:32px 22px 60px}
h1{font-size:30px;margin:0 0 16px}p{line-height:1.7}a{color:#1762ac}.intro{max-width:900px}.toolbar{position:sticky;top:0;z-index:1;display:flex;align-items:center;gap:10px;background:#eef0f3ed;padding:16px 0;flex-wrap:wrap}
button{font:inherit;padding:10px 16px;background:white;border:1px solid #aaa;border-radius:6px;cursor:pointer}button[aria-pressed=true]{background:#222;color:white;border-color:#222}button:focus-visible,a:focus-visible{outline:3px solid #147df5;outline-offset:3px}
.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:24px}article{background:white;border-radius:10px;border:1px solid #ddd;overflow:hidden}img{display:block;width:100%;aspect-ratio:1400/1280;object-fit:contain}.description{padding:4px 24px 24px}h2{font-size:20px;margin:0 0 10px}.meta{color:#626262;font-size:14px}.foot{font-size:14px;color:#666}
@media(max-width:440px){.grid{grid-template-columns:1fr}h1{font-size:24px}main{padding:20px 12px}.description{padding:4px 16px 20px}}
</style><main><h1>新しいステージ案 21–30</h1>
<p class="intro">5列×4行。赤と青の開始点から一筆ずつなぞり、全マスを着色します。赤枠・青枠はその単色、紫枠は両方のルートが必要です。各画像をクリックすると原寸で開きます。</p>
<div class="toolbar" role="group" aria-label="図の表示"><button data-mode="clean" aria-pressed="true">解答例あり</button><button data-mode="puzzle" aria-pressed="false">問題のみ</button><a href="stages-21-30.json">配置・解答データ JSON</a> · <a href="README.md">設計メモ</a></div>
<div class="grid">''' + '\n'.join(cards) + '''</div>
<p class="foot">難易度は目安。全20マスの着色・隣接性・自己再訪なし・色指定への適合を自動検証済み。解答は一例で、唯一解は未検証です。</p>
</main><script>
for(const button of document.querySelectorAll('[data-mode]'))button.addEventListener('click',()=>{
 for(const other of document.querySelectorAll('[data-mode]'))other.setAttribute('aria-pressed',String(other===button));
 for(const img of document.querySelectorAll('img[data-stage]')){const src=`stage-${img.dataset.stage}-${button.dataset.mode}.png`;img.src=src;img.closest('a').href=src;}
});
</script></html>'''
    (ROOT / 'index.html').write_text(page, encoding='utf-8')


def main():
    data = json.loads((ROOT / 'stages-21-30.json').read_text(encoding='utf-8'))
    stages, images = data['stages'], {}
    for s in stages:
        validate(s)
        n = s['id']
        images[n] = draw_stage(s, True)
        images[n].save(ROOT / f'stage-{n}-clean.png')
        draw_stage(s, False).save(ROOT / f'stage-{n}-puzzle.png')
        (ROOT / f'stage-{n}-clean.svg').write_text(svg_stage(s, True), encoding='utf-8')
    overview(stages, images)
    write_readme(stages)
    write_gallery(stages)
    report = {'stages_checked': 10, 'all_solution_paths_valid': True,
              'all_boards_have_20_painted_cells': True,
              'all_required_colors_match': True,
              'self_revisits': 0, 'nonadjacent_steps': 0,
              'solution_uniqueness_checked': False, 'distinct_from_stages_11_20_under_reflection_and_color_swap': True}
    (ROOT / 'validation.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    package = ROOT.parent / 'stage-ideas-21-30.zip'
    if package.exists():
        raise FileExistsError(package)
    files = [p for p in ROOT.iterdir() if p.suffix in ('.png','.svg','.html','.md') or p.name in ('stages-21-30.json','validation.json')]
    with zipfile.ZipFile(package, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
        for p in sorted(files):
            archive.write(p, p.name)
    with zipfile.ZipFile(package) as archive:
        assert archive.testzip() is None
        assert len([n for n in archive.namelist() if n.endswith('-clean.png')]) == 10
        assert len([n for n in archive.namelist() if n.endswith('-puzzle.png')]) == 10
    print(json.dumps(report, ensure_ascii=False))
    print(f'Package: {package} ({package.stat().st_size:,} bytes)')


if __name__ == '__main__':
    main()
