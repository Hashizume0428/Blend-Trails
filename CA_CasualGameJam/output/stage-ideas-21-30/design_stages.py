"""Ten additional 5x4 designs, with symmetry-aware duplicate checks."""
from __future__ import annotations

import importlib.util
import json
import random
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PREVIOUS = ROOT.parent / 'stage-ideas-11-20'
spec = importlib.util.spec_from_file_location('earlier_stage_engine', PREVIOUS / 'design_stages.py')
engine = importlib.util.module_from_spec(spec)
spec.loader.exec_module(engine)
ADJ = engine.ADJ
validate = engine.validate

# Purple cells are intentionally different shapes from the earlier ten:
# an elbow, three islands, a wide block, displaced gates, a T, side gates,
# a staircase, paired elbows, a central corridor and paired 2x2 blocks.
THEMES = [
    (21, 'ひじ形の交差点', [(2,2),(2,3),(3,3)], 'ひじ形の紫3マスを共有し、残った外側を赤と青で埋め分ける。', 4, 5),
    (22, '三つの飛び石', [(1,3),(2,1),(3,4)], '離れた3つの紫マスを両色で回収し、端に空白を残さない。', 4, 5),
    (23, '横長の共有広場', [(2,2),(2,3),(2,4),(3,2),(3,3),(3,4)], '中央の2×3広場を両色で通り、外周の単色枠を守る。', 4, 6),
    (24, 'ずれた横ゲート', [(1,2),(1,3),(3,3),(3,4)], '上と下で位置がずれた横2マスのゲートをつなぐ。', 5, 6),
    (25, 'T字の受け渡し', [(1,3),(2,2),(2,3),(2,4)], 'T字の共有部分から抜ける位置を変え、単色の領域を確保する。', 4, 6),
    (26, '左右の小窓', [(2,1),(2,2),(3,4),(3,5)], '左右の端にある紫ゲートを、異なる回り道で結ぶ。', 5, 7),
    (27, '二段の階段橋', [(1,2),(2,2),(2,3),(3,3),(3,4),(4,4)], '斜めに伸びる階段状の共有路と外側の回収を両立する。', 5, 7),
    (28, 'ふたつのL字ゲート', [(1,2),(1,3),(2,2),(3,4),(4,3),(4,4)], '対角に離れたL字の紫領域を両色でつなぐ。', 5, 8),
    (29, '中央回廊と二つの入口', [(1,2),(2,2),(2,3),(2,4),(3,4),(3,3),(4,3)], '中央の広い共有部分と上下の細い入口を通り抜ける。', 5, 8),
    (30, 'ふたつの混色広場', [(1,2),(1,3),(2,2),(2,3),(3,3),(3,4),(4,3),(4,4)], '斜めにつながる2つの2×2広場を共有する総合問題。', 6, 9),
]


def canonical(stage, solution=False):
    """Treat left/right, up/down flips and red/blue swaps as duplicates."""
    variants = []
    for flip_r in (False, True):
        for flip_c in (False, True):
            def transform(rc):
                r, c = rc
                return (5-r if flip_r else r, 6-c if flip_c else c)
            for swap in (False, True):
                result = []
                for color in ('red','blue'):
                    source = ('blue' if color == 'red' else 'red') if swap else color
                    result.append(transform(stage['starts'][source]))
                    if solution:
                        result.append(tuple(transform(rc) for rc in stage['solution'][source]))
                    else:
                        result.append(tuple(sorted(transform(rc) for rc in stage['required_cells'][source])))
                result.append(tuple(sorted(transform(rc) for rc in stage['required_cells']['purple'])))
                variants.append(tuple(result))
    return min(variants)


def purple_signature(cells):
    variants = []
    for fr in (False, True):
        for fc in (False, True):
            variants.append(tuple(sorted((5-r if fr else r, 6-c if fc else c) for r,c in cells)))
    return min(variants)


def create_pair(number, purple, minimum_turns, target_turns):
    rng = random.Random(786433 * number)
    pm = engine.mask(purple)
    best, considered = None, 0
    deadline = time.monotonic() + 40
    while considered < 120 and time.monotonic() < deadline:
        length = rng.choice(range(10 + len(purple)//2, 15 + len(purple)//3))
        start = rng.choice([i for i in range(20) if i not in purple])
        red, used = [start], {start}
        while len(red) < length:
            choices = [j for j in ADJ[red[-1]] if j not in used]
            if not choices:
                break
            rng.shuffle(choices)
            nxt = min(choices, key=lambda j: rng.random()*3 - int(j in purple))
            red.append(nxt)
            used.add(nxt)
        if len(red) != length or engine.mask(red) & pm != pm or engine.turns(red) < minimum_turns:
            continue
        blue_nodes = (set(range(20)) - used) | purple
        if not 9 <= len(blue_nodes) <= 17:
            continue
        blue = engine.hamiltonian(blue_nodes, purple, rng, minimum_turns)
        if blue is None:
            continue
        considered += 1
        score = -4*(abs(engine.turns(red)-target_turns) + abs(engine.turns(blue)-target_turns))
        score -= .35*abs(len(red)-len(blue))
        if best is None or score > best[0]:
            best = (score, red, blue)
    if best is None:
        raise RuntimeError(f'Stage {number} needs a different shared-cell layout')
    return best[1], best[2], considered


def main():
    previous_data = json.loads((PREVIOUS / 'stages-11-20.json').read_text(encoding='utf-8'))
    previous = previous_data['stages']
    puzzle_seen = {canonical(s) for s in previous}
    solution_seen = {canonical(s, True) for s in previous}
    purple_seen = {purple_signature(s['shared_cells']) for s in previous}
    progress = ROOT / 'design-progress.json'
    cached = {s['id']: s for s in json.loads(progress.read_text(encoding='utf-8'))} if progress.exists() else {}
    stages = []
    for number, name, shared, concept, minimum, target in THEMES:
        assert abs(sum(1 if (r+c)%2 else -1 for r,c in shared)) <= 2
        assert purple_signature(shared) not in purple_seen, (number, 'repeated purple layout')
        if number in cached and cached[number]['shared_cells'] == sorted(map(list, shared)):
            stage = cached[number]
        else:
            purple = {engine.cell(rc) for rc in shared}
            red, blue, count = create_pair(number, purple, minimum, target)
            stage = {
                'id': number, 'name': name, 'concept': concept,
                'board': {'rows':4,'columns':5},
                'difficulty': '中級' if number<=24 else '中級〜上級' if number<=27 else '上級',
                'starts': {'red':engine.coord(red[0]),'blue':engine.coord(blue[0])},
                'required_cells': {
                    'red':[engine.coord(i) for i in engine.pick_frames(red,blue,len(purple))],
                    'blue':[engine.coord(i) for i in engine.pick_frames(blue,red,len(purple))],
                    'purple':sorted(map(list,shared))},
                'shared_cells':sorted(map(list,shared)),
                'solution': {'red':[engine.coord(i) for i in red], 'blue':[engine.coord(i) for i in blue]},
                'metrics': {'red_cells':len(red), 'blue_cells':len(blue), 'shared_cells':len(purple),
                            'red_turns':engine.turns(red), 'blue_turns':engine.turns(blue)},
            }
        validate(stage)
        assert canonical(stage) not in puzzle_seen
        assert canonical(stage, True) not in solution_seen
        puzzle_seen.add(canonical(stage))
        solution_seen.add(canonical(stage, True))
        purple_seen.add(purple_signature(stage['shared_cells']))
        stages.append(stage)
        progress.write_text(json.dumps(stages,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(number,name,stage['metrics'],flush=True)
    data = {**{k:v for k,v in previous_data.items() if k!='stages'}, 'stages':stages}
    (ROOT/'stages-21-30.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('Validated: 10 solvable designs; no mirrored or color-swapped duplicates of stages 11-20.',flush=True)


if __name__ == '__main__':
    main()
