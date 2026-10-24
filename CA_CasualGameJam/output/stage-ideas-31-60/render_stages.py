"""Render exact coordinate diagrams and a local review gallery. Requires Pillow."""
from __future__ import annotations

import html
import json
import zipfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from validate_stages import verify

ROOT = Path(__file__).resolve().parent
WIDTH, HEIGHT = 1400, 1280
OX, OY, CELL = 140, 240, 224
INK = '#171923'
COLORS = {'red': '#ED2330', 'blue': '#087BF5', 'purple': '#8D2FA8'}
LATIN = 'C:/Windows/Fonts/arialbd.ttf'
JP = 'C:/Windows/Fonts/YuGothM.ttc'
JP_BOLD = 'C:/Windows/Fonts/YuGothB.ttc'


def point(rc, color):
    r,c = rc
    offset = -9 if color == 'red' else 9
    return OX+(c-.5)*CELL+offset, OY+(r-.5)*CELL+offset


def draw_stage(stage, solution):
    scale = 2
    image = Image.new('RGB', (WIDTH*scale, HEIGHT*scale), 'white')
    d = ImageDraw.Draw(image)

    def box(values):
        return tuple(round(x*scale) for x in values)

    def text(x,y,value,size=28,color=INK,font=JP,anchor=None):
        d.text((x*scale,y*scale),value,fill=color,font=ImageFont.truetype(font,size*scale),anchor=anchor)

    def line(points, color, width):
        points = [box(p) for p in points]
        d.line(points,fill=color,width=width*scale,joint='curve')
        radius = width*scale/2
        for x,y in points:
            d.ellipse((x-radius,y-radius,x+radius,y+radius),fill=color)

    text(WIDTH/2,90,f"STAGE {stage['id']}",62,font=LATIN,anchor='mm')
    text(WIDTH/2,157,stage['name'],32,font=JP_BOLD,anchor='mm')
    text(1256,43,'解答例' if solution else '問題図',23,color='#626B7B',anchor='ra')
    for col in range(1,6):
        text(OX+(col-.5)*CELL,OY-30,str(col),23,color='#78808D',font=LATIN,anchor='mm')
    for row in range(1,5):
        text(OX-32,OY+(row-.5)*CELL,str(row),23,color='#78808D',font=LATIN,anchor='mm')
    for col in range(6):
        x = OX+col*CELL
        line([(x,OY),(x,OY+4*CELL)],INK,12 if col in (0,5) else 7)
    for row in range(5):
        y = OY+row*CELL
        line([(OX,y),(OX+5*CELL,y)],INK,12 if row in (0,4) else 7)
    for color,cells in stage['required_cells'].items():
        for r,c in cells:
            x,y = OX+(c-1)*CELL+17, OY+(r-1)*CELL+17
            d.rectangle(box((x,y,x+CELL-34,y+CELL-34)),outline=COLORS[color],width=8*scale)
    if solution:
        for color in ('red','blue'):
            points = [point(rc,color) for rc in stage['solution'][color]]
            line(points,'white',12)
            line(points,COLORS[color],7)
            x,y = points[-1]
            d.ellipse(box((x-8,y-8,x+8,y+8)),fill=COLORS[color])
    for color in ('red','blue'):
        x,y = point(stage['starts'][color],color)
        d.ellipse(box((x-27,y-27,x+27,y+27)),fill=COLORS[color])
    text(WIDTH/2,1190,'通常マス＝単色のみ　｜　紫枠＝赤＋青　｜　全20マスを埋める',25,color='#626B7B',anchor='mm')
    text(WIDTH/2,1230,'大きい丸：開始点　／　細い線：解答例' if solution else '大きい丸：開始点　／　色枠：完成色の指定',22,color='#78808D',anchor='mm')
    return image.resize((WIDTH,HEIGHT),Image.Resampling.LANCZOS)


def svg_stage(stage, solution):
    chunks = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">',
              '<rect width="100%" height="100%" fill="white"/>',
              f'<title>STAGE {stage["id"]} — {html.escape(stage["name"])}</title>',
              '<g font-family="Yu Gothic,Arial,sans-serif" text-anchor="middle">',
              f'<text x="700" y="112" font-size="62" font-weight="700" fill="{INK}">STAGE {stage["id"]}</text>',
              f'<text x="700" y="168" font-size="32" fill="{INK}">{html.escape(stage["name"])}</text>',
              f'<text x="1256" y="62" text-anchor="end" font-size="23" fill="#626B7B">{"解答例" if solution else "問題図"}</text>']
    for c in range(1,6):
        chunks.append(f'<text x="{OX+(c-.5)*CELL}" y="218" font-size="23" fill="#78808D">{c}</text>')
    for r in range(1,5):
        chunks.append(f'<text x="108" y="{OY+(r-.5)*CELL+8}" font-size="23" fill="#78808D">{r}</text>')
    chunks.append('</g>')
    for c in range(6):
        x = OX+c*CELL
        chunks.append(f'<path d="M{x},{OY}V{OY+4*CELL}" stroke="{INK}" stroke-width="{12 if c in (0,5) else 7}"/>')
    for r in range(5):
        y = OY+r*CELL
        chunks.append(f'<path d="M{OX},{y}H{OX+5*CELL}" stroke="{INK}" stroke-width="{12 if r in (0,4) else 7}"/>')
    for color,cells in stage['required_cells'].items():
        for r,c in cells:
            x,y = OX+(c-1)*CELL+17,OY+(r-1)*CELL+17
            chunks.append(f'<rect x="{x}" y="{y}" width="190" height="190" fill="none" stroke="{COLORS[color]}" stroke-width="8"/>')
    if solution:
        chunks.append('<g id="solution" fill="none" stroke-linecap="round" stroke-linejoin="round">')
        for color in ('red','blue'):
            points = ' '.join(f'{x:g},{y:g}' for x,y in [point(rc,color) for rc in stage['solution'][color]])
            chunks.append(f'<polyline points="{points}" stroke="white" stroke-width="12"/>')
            chunks.append(f'<polyline points="{points}" stroke="{COLORS[color]}" stroke-width="7"/>')
        chunks.append('</g>')
    for color in ('red','blue'):
        x,y = point(stage['starts'][color],color)
        chunks.append(f'<circle cx="{x:g}" cy="{y:g}" r="27" fill="{COLORS[color]}"/>')
    chunks.append('<text x="700" y="1200" text-anchor="middle" font-family="Yu Gothic,sans-serif" font-size="25" fill="#626B7B">通常マス＝単色のみ ｜ 紫枠＝赤＋青 ｜ 全20マスを埋める</text>')
    chunks.append('</svg>')
    return '\n'.join(chunks)+'\n'


def overview(stages, images, full=False):
    first,last = stages[0]['id'],stages[-1]['id']
    columns = 3 if full else 2
    card_w,card_h,gap = (480,505,18) if full else (720,730,20)
    width = columns*card_w+(columns+1)*gap
    rows = (len(stages)+columns-1)//columns
    sheet = Image.new('RGB',(width,150+rows*card_h+gap),'#EEF1F6')
    d = ImageDraw.Draw(sheet)
    d.text((30,25),f'追加ステージ案 {first}–{last}',fill=INK,font=ImageFont.truetype(JP_BOLD,38))
    d.text((30,87),'5列×4行 ／ 通常マスは単色のみ ／ すべて解答例あり',fill='#626B7B',font=ImageFont.truetype(JP,22))
    for k,stage in enumerate(stages):
        x,y = gap+(k%columns)*(card_w+gap),140+(k//columns)*card_h
        d.rounded_rectangle((x,y,x+card_w,y+card_h-16),radius=12,fill='white')
        thumb_w = card_w-20
        thumb_h = round(thumb_w*HEIGHT/WIDTH)
        thumb = images[stage['id']].resize((thumb_w,thumb_h),Image.Resampling.LANCZOS)
        sheet.paste(thumb,(x+10,y+4))
        d.text((x+18,y+thumb_h+9),f"{stage['difficulty']}目安 · 紫{stage['metrics']['shared_cells']}マス",fill='#626B7B',font=ImageFont.truetype(JP,18 if full else 23))
    path = ROOT/f'stages-{first}-{last}-overview.png'
    sheet.save(path)


def coordinates(points, ordered=True):
    return (' → ' if ordered else '、').join(f'({r},{c})' for r,c in points) or 'なし'


def write_readme(stages, implemented=False):
    status = ('問題31〜60としてゲームに実装済みです。Stage1〜60を順番に出題し、Stage61以降は問題5〜60を再利用するエクストラです。'
              if implemented else '案ID31〜60は資料内の管理番号です。現在のゲームのStage31以降は既存問題を再利用するエクストラであり、今回の30案はまだゲームへ追加していません。')
    lines = ['# 追加ステージ案31〜60（30案）','',
             '既存と同じ5列×4行の配置資料です。'+status,'',
             '## 閲覧・受け渡し','',
             '- `index.html`：全30案。問題図／解答図の切り替えと10案ごとの絞り込み。',
             '- `stage-XX-puzzle.png`：問題図。開始点・完成色の枠のみ。',
             '- `stage-XX-clean.png`：解答例入りの清書図。',
             '- `stage-XX-puzzle.svg` / `stage-XX-clean.svg`：編集可能なベクター図。',
             '- `stages-31-60.json`：AIや実装担当に渡せる全30案の配置・解答データ。',
             '- `stages-31-40-overview.png`、`stages-41-50-overview.png`、`stages-51-60-overview.png`：10案ずつの一覧。',
             '- `stages-31-60-overview.png`：全30案の一覧。',
             '- `validation.json`：全30案の自動検証結果。','',
             '座標は **[行,列]**、左上が **[1,1]**。行は下へ、列は右へ増えます。UnityのCoordinateは **(列−1, 行−1)** です。','',
             '## ルール','',
             '各色の開始点から、上下左右に隣り合うマスを一筆ずつ進み、全20マスを埋めます。同じ色で同じマスを再訪せず、他方の開始点を通りません。通常マスと色指定のない開始点は単色のみ正解です。赤枠・青枠はその単色、紫枠は両色が必要です。禁止マスはありません。','',
             '図とJSONは既存資料に合わせて赤・青・紫に統一しています。赤は第1色、青は第2色、紫はその混色の役割を表すため、実装時は既存のStageColorPaletteの5ステージごとの色替えに対応できます。','',
             '掲載解答について、通常マスの混色ゼロ・20マスの被覆・隣接性・自己再訪なし・開始点・指定枠の一致を生成処理と独立した検証器で確認しています。既存問題1〜30と新規30案の間で、上下反転・左右反転・180度回転・2色の入れ替えだけの重複を除外しました。さらに混色指定の位置自体も、反転だけの重複を避けています。','',
             '難易度は共有マスの配置と解答の曲がり数に基づく設計上の目安です。唯一解や実機での難易度は未検証です。','',
             '| 案ID | テーマ | 難易度目安 | 紫マス | 共有領域数 | 赤／青の曲がり数 |',
             '|---|---|---|---|---|---|']
    for s in stages:
        m = s['metrics']
        lines.append(f"| {s['id']} | {s['name']} | {s['difficulty']} | {m['shared_cells']} | {m['purple_regions']} | {m['red_turns']} / {m['blue_turns']} |")
    for s in stages:
        n = s['id']
        lines.extend(['',f"## 案{n}：{s['name']}",'',s['concept'],'',
                      f'[問題図](stage-{n}-puzzle.png) · [解答図](stage-{n}-clean.png)','',
                      f"- 赤開始：{coordinates([s['starts']['red']])}",
                      f"- 青開始：{coordinates([s['starts']['blue']])}"])
        for color,label in (('red','赤枠'),('blue','青枠'),('purple','紫枠')):
            lines.append(f"- {label}：{coordinates(s['required_cells'][color],False)}")
        lines.extend(['','赤の解答例：','',coordinates(s['solution']['red']),'',
                      '青の解答例：','',coordinates(s['solution']['blue'])])
    lines.extend(['','## 再生成','',
                  'Python 3とPillowで、`python design_stages.py` → `python validate_stages.py` → `python render_stages.py` の順に実行できます。描画用の既定フォントはWindowsのArial / 游ゴシックです。',
                  '比較元の配置は `comparison-stages-01-30.json` に保存しています。Unityプロジェクト内で生成器を実行した場合は、現在のStageTestReferenceから比較元を読み直します。'])
    (ROOT/'README.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')


def write_gallery(stages, implemented=False):
    cards = []
    for s in stages:
        n = s['id']
        group = (n-31)//10
        cards.append(f'''<article data-group="{group}"><a class="image-link" href="stage-{n}-puzzle.png"><img src="stage-{n}-puzzle.png" data-stage="{n}" alt="案{n} {html.escape(s['name'])}の問題図" loading="lazy" width="1400" height="1280"></a>
<div class="description"><span class="eyebrow">PROPOSAL {n}</span><h2>{html.escape(s['name'])}</h2><p>{html.escape(s['concept'])}</p><p class="meta">{s['difficulty']}目安 · 紫{s['metrics']['shared_cells']}マス · 曲がり数 {s['metrics']['red_turns']} / {s['metrics']['blue_turns']}</p>
<div class="links"><a href="stage-{n}-puzzle.png" download>問題PNG</a><a href="stage-{n}-clean.png" download>解答PNG</a><a href="stage-{n}-puzzle.svg" download>問題SVG</a><a href="stage-{n}-clean.svg" download>解答SVG</a></div></div></article>''')
    page = '''<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>追加ステージ案31〜60</title><style>
*{box-sizing:border-box}body{margin:0;background:#eef1f6;color:#171923;font-family:system-ui,"Yu Gothic",sans-serif}main{max-width:1540px;margin:auto;padding:42px 24px 72px}h1{font-size:34px;letter-spacing:.02em;margin:0 0 16px}p{line-height:1.8}a{color:#195caa;text-underline-offset:3px}.intro{max-width:1000px}.meta{color:#626b7b;font-size:14px}.downloads{display:flex;gap:20px;flex-wrap:wrap;margin:24px 0}.toolbar{position:sticky;top:0;z-index:1;background:#eef1f6f5;border-bottom:1px solid #d2d8e3;padding:16px 0;display:flex;gap:20px;flex-wrap:wrap;align-items:center}.group{display:flex;gap:8px;align-items:center;flex-wrap:wrap}.group-label{font-size:14px;color:#626b7b;margin-right:4px}button{font:inherit;font-size:14px;padding:9px 15px;border:1px solid #b5bdca;border-radius:7px;background:white;cursor:pointer}button[aria-pressed=true]{background:#1b2435;color:white;border-color:#1b2435}button:focus-visible,a:focus-visible{outline:3px solid #087bf5;outline-offset:4px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:24px;margin-top:24px}article{border:1px solid #dde2ea;border-radius:12px;overflow:hidden;background:white}article[hidden]{display:none}.image-link{display:block}img{display:block;width:100%;height:auto}.description{padding:0 22px 24px}h2{font-size:20px;margin:6px 0 10px}.eyebrow{font-size:12px;color:#626b7b;letter-spacing:.12em}.links{display:flex;gap:12px;flex-wrap:wrap;font-size:14px}.foot{margin-top:32px;color:#626b7b;font-size:14px}.status{font-size:14px;color:#626b7b;margin-left:auto}@media(max-width:1100px){.grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:650px){main{padding:24px 12px 48px}h1{font-size:27px}.grid{grid-template-columns:1fr}.toolbar{gap:12px}button{padding:9px 11px}.status{margin-left:0}}
</style><main><h1>追加ステージ案31〜60</h1>
<p class="intro">5列×4行の新規30案です。通常マスは単色のみ、混色できるのは紫枠だけ。全案に解答例を用意し、20マスの被覆・色条件・経路と重複を自動検証しています。</p>
<p class="meta">案IDは資料の管理番号です。ゲームへの組み込みは未実施です。図は赤・青の役割で統一し、既存の色替え仕様にも対応できるデータを添えています。</p>
<nav class="downloads" aria-label="資料"><a href="stages-31-60.json" download>全30案のJSON</a><a href="README.md">設計メモ</a><a href="stages-31-40-overview.png">一覧31〜40</a><a href="stages-41-50-overview.png">一覧41〜50</a><a href="stages-51-60-overview.png">一覧51〜60</a></nav>
<div class="toolbar"><div class="group" role="group" aria-label="図の表示"><span class="group-label">表示</span><button data-mode="puzzle" aria-pressed="true">問題図</button><button data-mode="clean" aria-pressed="false">解答例</button></div><div class="group" role="group" aria-label="案の範囲"><span class="group-label">範囲</span><button data-group-filter="all" aria-pressed="true">全30案</button><button data-group-filter="0" aria-pressed="false">31〜40</button><button data-group-filter="1" aria-pressed="false">41〜50</button><button data-group-filter="2" aria-pressed="false">51〜60</button></div><span class="status" aria-live="polite">30案を表示</span></div>
<section class="grid" aria-label="ステージ案">'''+'\n'.join(cards)+'''</section>
<p class="foot">禁止マスなし。既存問題1〜30と新規30案の間で、反転や2色の入れ替えだけの重複はありません。難易度は設計上の目安で、唯一解・実機での難易度は未検証です。</p>
</main><script>
for(const button of document.querySelectorAll('[data-mode]'))button.addEventListener('click',()=>{
 for(const other of document.querySelectorAll('[data-mode]'))other.setAttribute('aria-pressed',String(other===button));
 for(const img of document.querySelectorAll('img[data-stage]')){const src=`stage-${img.dataset.stage}-${button.dataset.mode}.png`;img.src=src;img.closest('a').href=src;img.alt=`案${img.dataset.stage}の${button.dataset.mode==='clean'?'解答例':'問題図'}`;}
});
for(const button of document.querySelectorAll('[data-group-filter]'))button.addEventListener('click',()=>{
 for(const other of document.querySelectorAll('[data-group-filter]'))other.setAttribute('aria-pressed',String(other===button));
 let count=0;for(const card of document.querySelectorAll('article[data-group]')){card.hidden=button.dataset.groupFilter!=='all'&&card.dataset.group!==button.dataset.groupFilter;if(!card.hidden)count++;}
 document.querySelector('.status').textContent=`${count}案を表示`;
});
</script></html>'''
    if implemented:
        page = page.replace('案IDは資料の管理番号です。ゲームへの組み込みは未実施です。',
                            '問題31〜60としてゲームに実装済みです。Stage61以降は問題5〜60を再利用します。')
    (ROOT/'index.html').write_text(page,encoding='utf-8')


def main():
    report = verify()
    data = json.loads((ROOT/'stages-31-60.json').read_text(encoding='utf-8'))
    stages = data['stages']
    images = {}
    for stage in stages:
        n = stage['id']
        images[n] = draw_stage(stage,True)
        images[n].save(ROOT/f'stage-{n}-clean.png')
        draw_stage(stage,False).save(ROOT/f'stage-{n}-puzzle.png')
        for solution in (False,True):
            mode = 'clean' if solution else 'puzzle'
            (ROOT/f'stage-{n}-{mode}.svg').write_text(svg_stage(stage,solution),encoding='utf-8')
        print(f'Rendered {n}',flush=True)
    for start in (0,10,20):
        overview(stages[start:start+10],images)
    overview(stages,images,full=True)
    write_readme(stages, data.get('implemented_in_game',False))
    write_gallery(stages, data.get('implemented_in_game',False))
    files = [p for p in ROOT.iterdir() if p.suffix in ('.png','.svg','.html','.md','.py') or
             p.name in ('stages-31-60.json','comparison-stages-01-30.json','validation.json')]
    package = ROOT.parent/'stage-ideas-31-60.zip'
    with zipfile.ZipFile(package,'w',compression=zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(files):
            archive.write(path,path.name)
    with zipfile.ZipFile(package) as archive:
        assert archive.testzip() is None
        assert len([n for n in archive.namelist() if n.endswith('-clean.png')]) == 30
        assert len([n for n in archive.namelist() if n.endswith('-puzzle.png')]) == 30
        assert len([n for n in archive.namelist() if n.endswith('.svg')]) == 60
    print(f'Validated {report["stages_checked"]} proposals. Package: {package} ({package.stat().st_size:,} bytes)',flush=True)


if __name__ == '__main__':
    main()
