"""Design thirty exact 5x4 puzzles. No Unity assets or game settings are changed."""
from __future__ import annotations

import json
import random
import re
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent
ALL = (1 << 20) - 1
ADJ = [[j for j in range(20) if abs(i // 5 - j // 5) + abs(i % 5 - j % 5) == 1] for i in range(20)]
NEIGHBORS = [sum(1 << j for j in row) for row in ADJ]

# Number, theme, purple cells, connected purple regions, target turns per path.
# These are design targets, not measured player difficulty.
THEMES = [
    (31, '二つの待ち合わせ', 2, 2, 5, '離れた二つの合流点を結び、間の通常マスを単色で埋め分ける。'),
    (32, '小さな連絡橋', 3, 2, 5, '短い共有路と独立した合流点を回収し、端の取り残しを防ぐ。'),
    (33, '二組の通り道', 4, 2, 5, '二つに分かれた共有部分を、異なる単色の回り道でつなぐ。'),
    (34, '曲がり角の受け渡し', 3, 1, 6, 'つながった三つの混色マスを使い、折り返す側を考える。'),
    (35, 'ずれた合流地点', 4, 2, 6, '共有部分の前後で進む向きを変え、単色の領域を確保する。'),
    (36, '三つの合流点', 3, 3, 6, '独立した三つの混色マスを両色で訪ね、空白を残さずつなぐ。'),
    (37, '四マスの連絡路', 4, 1, 6, 'ひと続きの共有路を使い、入口と出口を分担する。'),
    (38, 'ふたつの中継所', 4, 2, 6, '二つの共有領域の間を、重ならない単色の線で結ぶ。'),
    (39, '飛び石を巡る道', 4, 3, 7, '三つに分かれた混色の目印を回り、通常マスへの重なりを避ける。'),
    (40, '五つの共有マス', 5, 2, 7, '二つの共有領域と外側の単色マスを、折り返しで回収する。'),
    (41, '長い合流路', 5, 1, 7, '五つの共有マスを両色でたどり、単色の脇道を埋める。'),
    (42, '三方向の受け渡し', 5, 3, 7, '三つの共有領域を訪ねる順番と、単色で抜ける方向を考える。'),
    (43, '二つの共有ブロック', 6, 2, 7, '二つに分かれた混色部分を共有し、残りの領域を塗り分ける。'),
    (44, '四点を巡る折り返し', 4, 3, 7, '三つに分かれた四つの共有マスを、一筆ずつの線で巡る。'),
    (45, '折り返しの回廊', 6, 1, 8, 'ひと続きの共有領域を通り、外周の単色マスも回収する。'),
    (46, '三つの中継領域', 6, 3, 8, '三つに分かれた混色領域の間で、単色の担当を切り替える。'),
    (47, '五点の受け渡し', 5, 3, 8, '小さな共有領域を順にたどり、つなぐ線の重複を避ける。'),
    (48, '広がる共有路', 7, 2, 8, '二つの共有領域を使いつつ、狭い単色部分を取り残さない。'),
    (49, '分かれた連絡路', 6, 2, 8, '共有路を抜ける向きを変え、両方の線に折り返しを作る。'),
    (50, '合流と離脱', 7, 3, 8, '三つの共有領域で合流と離脱を繰り返し、盤面を埋め切る。'),
    (51, '大きな共有回廊', 8, 1, 8, '広い共有領域と残る単色領域を、一筆ずつで両立させる。'),
    (52, '六点の連携', 6, 3, 8, '三つの共有領域を通る順番と、線の折り返しを組み合わせる。'),
    (53, 'ふたつの混色ループ', 8, 2, 9, '二つの混色領域を巡り、単色の線で別々の回り道を作る。'),
    (54, '散らばる七つの目印', 7, 3, 9, '分散した七つの共有マスを回収し、細い単色部分を守る。'),
    (55, '三領域の大回り', 8, 3, 9, '三つの混色領域をつなぎ、盤面の端まで大きく折り返す。'),
    (56, '交互の受け渡し', 7, 2, 9, '二つの共有領域を何度も曲がりながら通り、単色枠を守る。'),
    (57, '細道と共有広場', 8, 2, 9, '広い混色部分と細い単色の通り道を、両方の線で使い分ける。'),
    (58, '八点の共有ゲート', 8, 3, 9, '三つの共有領域をつなぐ単色の経路を、最後まで確保する。'),
    (59, '長い共有の折り返し', 8, 1, 10, 'つながった八つの共有マスと単色の脇道を、折り返して埋める。'),
    (60, '三十問目の総合路', 8, 3, 10, '三つの共有領域・単色枠・外周の回収を組み合わせる総合問題。'),
]


def coord(i):
    return [i // 5 + 1, i % 5 + 1]


def cell(rc):
    r, c = rc
    return (r - 1) * 5 + c - 1


def mask(nodes):
    return sum(1 << i for i in set(nodes))


def members(bits):
    while bits:
        bit = bits & -bits
        yield bit.bit_length() - 1
        bits ^= bit


def component_count(bits):
    count = 0
    while bits:
        count += 1
        reached = front = bits & -bits
        while front:
            following = 0
            for i in members(front):
                following |= NEIGHBORS[i]
            front = following & bits & ~reached
            reached |= front
        bits &= ~reached
    return count


def turns(path):
    directions = [b - a for a, b in zip(path, path[1:])]
    return sum(a != b for a, b in zip(directions, directions[1:]))


def purple_signature(cells):
    variants = []
    for fr in (False, True):
        for fc in (False, True):
            variants.append(tuple(sorted((5-r if fr else r, 6-c if fc else c) for r, c in cells)))
    return min(variants)


def canonical(stage, solution=False):
    variants = []
    for fr in (False, True):
        for fc in (False, True):
            def transform(rc):
                r, c = rc
                return (5-r if fr else r, 6-c if fc else c)
            for swap in (False, True):
                value = []
                for color in ('red', 'blue'):
                    source = ('blue' if color == 'red' else 'red') if swap else color
                    value.append(transform(stage['starts'][source]))
                    coordinates = stage['solution'][source] if solution else stage['required_cells'][source]
                    value.append(tuple(transform(rc) for rc in coordinates) if solution else
                                 tuple(sorted(transform(rc) for rc in coordinates)))
                value.append(tuple(sorted(transform(rc) for rc in stage['required_cells']['purple'])))
                variants.append(tuple(value))
    return min(variants)


def find_blue(nodes, shared, rng, minimum_turns):
    """Find a path visiting exactly nodes, starting outside the red path."""
    if component_count(nodes) != 1:
        return None
    degrees = {i: (NEIGHBORS[i] & nodes).bit_count() for i in members(nodes)}
    leaves = [i for i, degree in degrees.items() if degree == 1]
    if len(leaves) > 2 or min(degrees.values()) == 0:
        return None
    starts = [i for i in (leaves or list(degrees)) if not (shared >> i) & 1]
    rng.shuffle(starts)
    budget = 3500

    def visit(path, rest):
        nonlocal budget
        budget -= 1
        if budget <= 0:
            return None
        if rest == 0:
            return list(path) if turns(path) >= minimum_turns else None
        options = list(members(NEIGHBORS[path[-1]] & rest))
        rng.shuffle(options)
        options.sort(key=lambda i: (NEIGHBORS[i] & rest).bit_count())
        for nxt in options:
            remaining = rest ^ (1 << nxt)
            if remaining and component_count(remaining) != 1:
                continue
            if sum((NEIGHBORS[i] & (remaining | (1 << nxt))).bit_count() == 1
                   for i in members(remaining)) > 1:
                continue
            path.append(nxt)
            found = visit(path, remaining)
            path.pop()
            if found is not None:
                return found
        return None

    for start in starts:
        found = visit([start], nodes ^ (1 << start))
        if found is not None:
            return found
    return None


def pick_frames(path, other, count):
    available = set(path) - set(other) - {path[0]}
    ranked = []
    for k, i in enumerate(path):
        if i in available:
            corner = 0 < k < len(path)-1 and path[k]-path[k-1] != path[k+1]-path[k]
            ranked.append((k / len(path) + .5 * corner, i))
    ranked.sort(reverse=True)
    selected = []
    for _, i in ranked:
        if all(abs(i//5-j//5) + abs(i%5-j%5) >= 2 for j in selected):
            selected.append(i)
        if len(selected) == count:
            break
    for _, i in ranked:
        if len(selected) >= count:
            break
        if i not in selected:
            selected.append(i)
    return sorted(selected)


def validate(stage):
    n = stage['id']
    assert stage['board'] == {'rows': 4, 'columns': 5}, n
    paths = stage['solution']
    visited = {}
    for color in ('red', 'blue'):
        route = paths[color]
        assert route[0] == stage['starts'][color], (n, 'start')
        assert all(1 <= r <= 4 and 1 <= c <= 5 for r, c in route), (n, 'bounds')
        assert len(route) == len(set(map(tuple, route))), (n, 'self revisit')
        assert all(abs(a[0]-b[0]) + abs(a[1]-b[1]) == 1 for a, b in zip(route, route[1:])), (n, 'step')
        visited[color] = set(map(tuple, route))
    required = {key: set(map(tuple, values)) for key, values in stage['required_cells'].items()}
    assert not (required['red'] & required['blue'] or required['red'] & required['purple'] or
                required['blue'] & required['purple']), (n, 'conflicting frames')
    assert visited['red'] | visited['blue'] == {(r, c) for r in range(1, 5) for c in range(1, 6)}, (n, 'coverage')
    assert visited['red'] & visited['blue'] == required['purple'], (n, 'purple only on required cells')
    assert set(map(tuple, stage['shared_cells'])) == required['purple'], n
    assert required['red'] <= visited['red'] - visited['blue'], (n, 'red frame')
    assert required['blue'] <= visited['blue'] - visited['red'], (n, 'blue frame')
    assert tuple(stage['starts']['red']) not in visited['blue'], (n, 'other start')
    assert tuple(stage['starts']['blue']) not in visited['red'], (n, 'other start')
    assert not stage.get('forbidden_cells'), (n, 'forbidden cell')
    return True


def read_existing():
    """Snapshot the current game geometry, including revised puzzles 5,6,9,10."""
    source = ROOT.parents[1] / 'Assets/Tests/PlayMode/StageTestReference.cs'
    snapshot = ROOT / 'comparison-stages-01-30.json'
    if not source.exists():
        return json.loads(snapshot.read_text(encoding='utf-8'))['stages']
    rows = re.findall(r'new Reference\("([0-9 ]*)", "([0-9 ]*)", "([0-9 ]*)", "([0-9 ]*)", "([0-9 ]*)"\)',
                      source.read_text(encoding='utf-8'))
    assert len(rows) >= 30
    rows = rows[:30]

    def coordinates(text):
        return [[int(code[0]), int(code[1])] for code in text.split()]

    def expand(text):
        corners = coordinates(text)
        result = [corners[0]]
        for r, c in corners[1:]:
            while result[-1] != [r, c]:
                y, x = result[-1]
                assert y == r or x == c
                result.append([y + (r > y) - (r < y), x + (c > x) - (c < x)])
        return result

    stages = []
    for n, row in enumerate(rows, 1):
        red, blue = expand(row[0]), expand(row[1])
        stage = {'id': n, 'board': {'rows': 4, 'columns': 5},
                 'starts': {'red': red[0], 'blue': blue[0]},
                 'required_cells': dict(zip(('red','blue','purple'), map(coordinates, row[2:]))),
                 'shared_cells': coordinates(row[4]), 'solution': {'red': red, 'blue': blue}}
        validate(stage)
        stages.append(stage)
    snapshot.write_text(json.dumps({'source': 'Assets/Tests/PlayMode/StageTestReference.cs', 'stages': stages},
                                   ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    return stages


def create_stage(theme, purple_seen, starts_used):
    number, name, count, regions, target, concept = theme
    rng = random.Random(20261025 * 97 + number * 65537)
    considered = 0
    best = None
    begun = time.monotonic()
    for attempt in range(200000):
        if attempt % 1000 == 0 and time.monotonic() - begun > 45:
            break
        red_length = rng.choice(range(max(9, (20+count)//2-2), min(17, (20+count)//2+3)))
        red = [rng.randrange(20)]
        used = {red[0]}
        while len(red) < red_length:
            options = [j for j in ADJ[red[-1]] if j not in used]
            if not options:
                break
            nxt = rng.choice(options)
            red.append(nxt)
            used.add(nxt)
        if len(red) != red_length or turns(red) < max(4, target-2):
            continue
        # Sampling shared cells after the first route makes coverage exact by construction.
        purple = set(rng.sample(red[1:], count))
        shared_mask = mask(purple)
        if component_count(shared_mask) != regions:
            continue
        shared_coords = sorted(coord(i) for i in purple)
        if purple_signature(shared_coords) in purple_seen:
            continue
        if abs(sum(1 if (i//5+i%5)%2 else -1 for i in purple)) > 2:
            continue
        blue_nodes = (ALL ^ mask(red)) | shared_mask
        if not 9 <= blue_nodes.bit_count() <= 17:
            continue
        blue = find_blue(blue_nodes, shared_mask, rng, max(4, target-2))
        if blue is None:
            continue
        considered += 1
        score = -4 * (abs(turns(red)-target) + abs(turns(blue)-target))
        score -= .45 * abs(len(red)-len(blue))
        score -= .7 * (starts_used.get(red[0], 0) + starts_used.get(blue[0], 0))
        if best is None or score > best[0]:
            best = (score, red, blue, shared_coords)
        if considered >= 14 and best[0] > -5:
            break
        if considered >= 65:
            break
    if best is None:
        raise RuntimeError(f'No design for {number}: purple={count}, regions={regions}. Revise the theme.')
    _, red, blue, shared = best
    frame_count = 2 if count <= 6 else 1
    return {'id': number, 'name': name, 'concept': concept,
            'board': {'rows': 4, 'columns': 5},
            'difficulty': '中級' if number <= 40 else '中級〜上級' if number <= 50 else '上級',
            'starts': {'red': coord(red[0]), 'blue': coord(blue[0])},
            'required_cells': {'red': [coord(i) for i in pick_frames(red, blue, frame_count)],
                               'blue': [coord(i) for i in pick_frames(blue, red, frame_count)],
                               'purple': shared},
            'shared_cells': shared, 'forbidden_cells': [],
            'solution': {'red': [coord(i) for i in red], 'blue': [coord(i) for i in blue]},
            'metrics': {'red_cells': len(red), 'blue_cells': len(blue), 'shared_cells': count,
                        'purple_regions': regions, 'red_turns': turns(red), 'blue_turns': turns(blue)},
            'design_target': {'turns_per_color': target, 'feasible_pairs_considered': considered}}


def main():
    existing = read_existing()
    checkpoint = ROOT / 'design-progress.json'
    cached = {s['id']: s for s in json.loads(checkpoint.read_text(encoding='utf-8'))} if checkpoint.exists() else {}
    purple_seen = {purple_signature(s['shared_cells']) for s in existing}
    puzzle_seen = {canonical(s) for s in existing}
    route_seen = {canonical(s, True) for s in existing}
    starts_used = {}
    stages = []
    for theme in THEMES:
        number = theme[0]
        stage = cached.get(number) or create_stage(theme, purple_seen, starts_used)
        validate(stage)
        assert canonical(stage) not in puzzle_seen
        assert canonical(stage, True) not in route_seen
        assert purple_signature(stage['shared_cells']) not in purple_seen
        puzzle_seen.add(canonical(stage))
        route_seen.add(canonical(stage, True))
        purple_seen.add(purple_signature(stage['shared_cells']))
        for rc in stage['starts'].values():
            i = cell(rc)
            starts_used[i] = starts_used.get(i, 0) + 1
        stages.append(stage)
        checkpoint.write_text(json.dumps(stages, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
        print(number, stage['name'], stage['metrics'], flush=True)
    data = {'schema_version': 2, 'kind': 'stage_proposals', 'implemented_in_game': False,
            'proposal_ids': '31-60 (proposal IDs, not existing endless-stage display numbers)',
            'coordinate_system': 'All coordinates are [row, column], 1-based, from the top-left.',
            'palette_roles': 'red=first authored color; blue=second authored color; purple=their mix. Map roles through StageColorPalette when implementing.',
            'rules': ['Cover all 20 cells.', 'Each color is one non-self-revisiting orthogonal path from its own start.',
                      'Normal cells, including unmarked starts, must be exactly one base color.',
                      'Required red/blue cells must be exactly that single color; purple cells require both.',
                      'The intersection of the two paths must equal the purple required cells exactly.',
                      'Do not enter the other color start. No forbidden cells are used.',
                      'A verified example solution is supplied; uniqueness is not claimed.'],
            'stages': stages}
    (ROOT / 'stages-31-60.json').write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print('Created 30 valid new proposals; no reflected or color-swapped duplicates of existing puzzles 1-30.', flush=True)


if __name__ == '__main__':
    main()
