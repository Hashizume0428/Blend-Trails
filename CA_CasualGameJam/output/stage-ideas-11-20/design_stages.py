"""Create new, exact 5x4 puzzle designs and reference diagrams; no Unity edits."""
from __future__ import annotations

import json
import random
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent
ROWS, COLS = 4, 5
ALL = (1 << 20) - 1
ADJ = [[j for j in range(20) if abs(i // 5 - j // 5) + abs(i % 5 - j % 5) == 1] for i in range(20)]

THEMES = [
    (11, "中央の三連橋", [(2, 2), (2, 3), (2, 4)], "横に並ぶ3つの紫マスを、赤と青の両方でつなぐ。", 4),
    (12, "斜めの合流点", [(1, 2), (2, 3), (3, 4), (4, 4)], "斜めに離れた紫マスと下端の縦ゲートをつなぐ。", 4),
    (13, "中央ブロック", [(2, 2), (2, 3), (3, 2), (3, 3)], "中央の2×2を両色で通り、周囲を単色に塗り分ける。", 4),
    (14, "ふたつの混色ゲート", [(1, 4), (2, 4), (3, 2), (4, 2)], "上下に離れた縦2マスの紫ゲートを通る順番が鍵。", 4),
    (15, "階段の共有路", [(1, 3), (2, 3), (2, 4), (3, 4), (3, 5)], "階段状の紫マスをたどりながら、空いた側を埋める。", 4),
    (16, "縦の共有通路", [(1, 3), (2, 3), (3, 3), (4, 3)], "中央の縦一列を共有し、左右へ折り返す位置を考える。", 4),
    (17, "二点交差の回り道", [(2, 2), (3, 4)], "紫は2マスだけ。長い単色ルートで盤面の取り残しを防ぐ。", 5),
    (18, "L字の引き継ぎ", [(1, 2), (2, 2), (3, 2), (3, 3), (3, 4)], "L字の混色通路と外側の折り返しを組み合わせる。", 5),
    (19, "上下のすれ違い", [(1, 2), (1, 3), (2, 3), (3, 3), (4, 3), (4, 4)], "長い共有通路を使いつつ、上下で別方向に抜ける。", 5),
    (20, "二重ゲートの総合問題", [(1, 4), (2, 2), (2, 4), (3, 2), (3, 3), (4, 3)], "離れた縦ゲートと曲がる共有路をつなぐ総合問題。", 5),
]


def cell(rc):
    r, c = rc
    return (r - 1) * COLS + c - 1


def coord(i):
    return [i // COLS + 1, i % COLS + 1]


def mask(items):
    return sum(1 << i for i in set(items))


def turns(path):
    delta = [b - a for a, b in zip(path, path[1:])]
    return sum(a != b for a, b in zip(delta, delta[1:]))


def connected(nodes):
    if not nodes:
        return True
    seen = {next(iter(nodes))}
    stack = list(seen)
    while stack:
        for j in ADJ[stack.pop()]:
            if j in nodes and j not in seen:
                seen.add(j)
                stack.append(j)
    return len(seen) == len(nodes)


def hamiltonian(nodes, shared, rng, minimum_turns):
    """Find a simple path covering this color's exact required region."""
    if not connected(nodes):
        return None
    degree = {i: len(set(ADJ[i]) & nodes) for i in nodes}
    leaves = [i for i in nodes if degree[i] == 1]
    if len(leaves) > 2 or any(degree[i] == 0 for i in nodes):
        return None
    starts = [i for i in (leaves or list(nodes)) if i not in shared]
    rng.shuffle(starts)
    budget = [14000]

    def walk(path, remaining):
        budget[0] -= 1
        if budget[0] <= 0:
            return None
        if not remaining:
            return path if turns(path) >= minimum_turns else None
        choices = [j for j in ADJ[path[-1]] if j in remaining]
        rng.shuffle(choices)
        choices.sort(key=lambda j: len(set(ADJ[j]) & remaining))
        for j in choices:
            rest = remaining - {j}
            if rest and not connected(rest):
                continue
            # Every residual degree-one vertex must be either the next
            # vertex's continuation or the eventual free endpoint.
            if sum(len(set(ADJ[k]) & (rest | {j})) == 1 for k in rest) > 1:
                continue
            found = walk(path + [j], rest)
            if found:
                return found
        return None

    for start in starts:
        found = walk([start], nodes - {start})
        if found:
            return found
    return None


def create_pair(number, purple, minimum_turns):
    rng = random.Random(104729 * number)
    pm = mask(purple)
    deadline = time.monotonic() + 45
    best = None
    considered = 0
    # Red is a randomized simple walk; blue must visit every remaining
    # cell AND all purple cells. This guarantees exact shared positions.
    for attempt in range(1000000):
        if time.monotonic() > deadline:
            break
        length = rng.choice(range(10 + len(purple) // 2, 15 + len(purple) // 3))
        start = rng.choice([i for i in range(20) if i not in purple])
        red = [start]
        used = {start}
        while len(red) < length:
            choices = [j for j in ADJ[red[-1]] if j not in used]
            if not choices:
                break
            # Bias toward yet-unvisited shared cells without forcing
            # the same route through every theme.
            rng.shuffle(choices)
            next_cell = min(choices, key=lambda j: rng.random() * 3 - (1 if j in purple else 0))
            red.append(next_cell)
            used.add(next_cell)
        if len(red) != length or not pm & mask(red) == pm or turns(red) < minimum_turns:
            continue
        blue_nodes = (set(range(20)) - used) | purple
        if not 9 <= len(blue_nodes) <= 17:
            continue
        blue = hamiltonian(blue_nodes, purple, rng, minimum_turns)
        if blue is None or blue[0] == red[0]:
            continue
        considered += 1
        # Prefer balanced paths and turns, nontrivial red-only/blue-only
        # regions, and endpoints separated from their own starts.
        target_turns = 4 + (number - 11) // 2
        score = -(abs(turns(red) - target_turns) + abs(turns(blue) - target_turns)) * 4
        score -= abs(len(red) - len(blue)) * 0.35
        if best is None or score > best[0]:
            best = (score, red, blue)
        if considered >= 80:
            break
    if best is None:
        raise RuntimeError(f'No route pair for stage {number}; theme needs revision')
    return best[1], best[2], considered


def pick_frames(path, other, purple_count):
    exclusive = set(path) - set(other) - {path[0]}
    target = 2 if purple_count <= 4 else 1
    ranked = []
    for k, i in enumerate(path):
        if i not in exclusive:
            continue
        at_turn = 0 < k < len(path) - 1 and path[k] - path[k - 1] != path[k + 1] - path[k]
        ranked.append((k / len(path) + 0.55 * at_turn, i))
    ranked.sort(reverse=True)
    chosen = []
    for _, i in ranked:
        if len(chosen) >= target:
            break
        if not chosen or abs(i // 5 - chosen[0] // 5) + abs(i % 5 - chosen[0] % 5) >= 2:
            chosen.append(i)
    for _, i in ranked:
        if len(chosen) >= target:
            break
        if i not in chosen:
            chosen.append(i)
    return sorted(chosen)


def validate(stage):
    red, blue = stage['solution']['red'], stage['solution']['blue']
    sets = {}
    for color, path in [('red', red), ('blue', blue)]:
        assert len(path) == len(set(map(tuple, path))), (stage['id'], 'self repeat')
        assert all(1 <= r <= 4 and 1 <= c <= 5 for r, c in path)
        assert all(abs(a[0] - b[0]) + abs(a[1] - b[1]) == 1 for a, b in zip(path, path[1:]))
        assert path[0] == stage['starts'][color]
        sets[color] = set(map(tuple, path))
    assert len(sets['red'] | sets['blue']) == 20
    assert sets['red'] & sets['blue'] == set(map(tuple, stage['shared_cells']))
    for color, cells in stage['required_cells'].items():
        for rc in cells:
            actual = ('purple' if tuple(rc) in sets['red'] & sets['blue']
                      else 'red' if tuple(rc) in sets['red'] else 'blue')
            assert actual == color, (stage['id'], rc, color, actual)
    assert stage['starts']['red'] != stage['starts']['blue']
    return True


def main():
    ROOT.mkdir(parents=True, exist_ok=True)
    stages = []
    progress_path = ROOT / 'design-progress.json'
    previous = {s['id']: s for s in json.loads(progress_path.read_text(encoding='utf-8'))} if progress_path.exists() else {}
    for number, title, shared_coords, description, min_turns in THEMES:
        if number in previous and previous[number].get('design_revision') == 2 and previous[number]['shared_cells'] == sorted(map(list, shared_coords)):
            validate(previous[number])
            stages.append(previous[number])
            print(number, 'reused validated design', flush=True)
            continue
        purple = set(map(cell, shared_coords))
        red, blue, considered = create_pair(number, purple, min_turns)
        frames = {
            'red': [coord(i) for i in pick_frames(red, blue, len(purple))],
            'blue': [coord(i) for i in pick_frames(blue, red, len(purple))],
            'purple': sorted([coord(i) for i in purple]),
        }
        stage = {
            'id': number, 'name': title, 'concept': description, 'design_revision': 2,
            'board': {'rows': 4, 'columns': 5},
            'difficulty': '中級' if number <= 14 else '中級〜上級' if number <= 17 else '上級',
            'starts': {'red': coord(red[0]), 'blue': coord(blue[0])},
            'required_cells': frames,
            'shared_cells': sorted([coord(i) for i in purple]),
            'solution': {'red': [coord(i) for i in red], 'blue': [coord(i) for i in blue]},
            'metrics': {'red_cells': len(red), 'blue_cells': len(blue), 'shared_cells': len(purple),
                        'red_turns': turns(red), 'blue_turns': turns(blue)},
        }
        validate(stage)
        stages.append(stage)
        progress_path.write_text(json.dumps(stages, ensure_ascii=False, indent=2), encoding='utf-8')
        print(number, title, stage['metrics'], f'candidates={considered}', flush=True)
    data = {
        'schema_version': 1,
        'coordinate_system': 'All coordinates are [row, column], 1-based, from the top-left.',
        'rules': ['Cover every cell with red, blue or both.',
                  'Each color is one non-self-revisiting orthogonal path from its start.',
                  'Required red/blue cells must remain that single color; purple requires both.',
                  'Unmarked cells may finish as any supported color.',
                  'Solutions are examples; uniqueness is not claimed.'],
        'stages': stages,
    }
    (ROOT / 'stages-11-20.json').write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
