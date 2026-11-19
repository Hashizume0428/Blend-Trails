"""Independent checks of the proposed puzzles, without importing the design engine."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def identity(stage, route=False):
    variants = []
    for vertical in (False, True):
        for horizontal in (False, True):
            def move(rc):
                return (5-rc[0] if vertical else rc[0], 6-rc[1] if horizontal else rc[1])
            for colors in (('red','blue'), ('blue','red')):
                key = []
                for color in colors:
                    key.append(move(stage['starts'][color]))
                    points = [move(rc) for rc in stage['solution' if route else 'required_cells'][color]]
                    key.append(tuple(points if route else sorted(points)))
                key.append(tuple(sorted(move(rc) for rc in stage['required_cells']['purple'])))
                variants.append(tuple(key))
    return min(variants)


def verify():
    data = json.loads((ROOT/'stages-31-60.json').read_text(encoding='utf-8'))
    stages = data['stages']
    old = json.loads((ROOT/'comparison-stages-01-30.json').read_text(encoding='utf-8'))['stages']
    assert [s['id'] for s in stages] == list(range(31, 61))
    puzzle_keys = {identity(s) for s in old}
    route_keys = {identity(s, True) for s in old}
    rows = []
    for stage in stages:
        n = stage['id']
        assert stage['board'] == {'rows': 4, 'columns': 5}, n
        actual = {(r,c): set() for r in range(1,5) for c in range(1,6)}
        path_sets = {}
        for color in ('red','blue'):
            points = [tuple(rc) for rc in stage['solution'][color]]
            assert points[0] == tuple(stage['starts'][color]), n
            assert len(points) == len(set(points)), (n, 'revisit')
            assert all(abs(a[0]-b[0])+abs(a[1]-b[1]) == 1 for a,b in zip(points, points[1:])), (n, 'adjacency')
            for rc in points:
                assert rc in actual, (n, 'bounds')
                actual[rc].add(color)
            path_sets[color] = set(points)
        for color, other in (('red','blue'),('blue','red')):
            assert tuple(stage['starts'][color]) not in path_sets[other], (n, 'other start')
        conditions = {}
        for color, cells in stage['required_cells'].items():
            for rc in cells:
                rc = tuple(rc)
                assert rc not in conditions, (n, 'conflicting frames')
                conditions[rc] = {'red','blue'} if color == 'purple' else {color}
        for rc, ingredients in actual.items():
            assert ingredients, (n, 'empty')
            if rc in conditions:
                assert ingredients == conditions[rc], (n, rc, 'frame')
            else:
                assert len(ingredients) == 1, (n, rc, 'mixed normal')
        assert path_sets['red'] & path_sets['blue'] == set(map(tuple, stage['required_cells']['purple'])), n
        assert set(map(tuple, stage['shared_cells'])) == path_sets['red'] & path_sets['blue'], n
        assert stage['forbidden_cells'] == [], n
        assert identity(stage) not in puzzle_keys, (n, 'duplicate puzzle')
        assert identity(stage, True) not in route_keys, (n, 'duplicate route')
        puzzle_keys.add(identity(stage))
        route_keys.add(identity(stage, True))
        turn_counts = {}
        for color in ('red','blue'):
            p = stage['solution'][color]
            d = [(b[0]-a[0], b[1]-a[1]) for a,b in zip(p,p[1:])]
            turn_counts[color] = sum(a != b for a,b in zip(d,d[1:]))
            assert len(p) == stage['metrics'][f'{color}_cells'], n
            assert turn_counts[color] == stage['metrics'][f'{color}_turns'], n
        rows.append({'id': n, 'passed': True, 'painted_cells': 20, 'mixed_normal_cells': 0,
                     'purple_cells': len(stage['required_cells']['purple']), 'turns': turn_counts})
    report = {'stages_checked': 30, 'comparison_puzzles': 30,
              'all_solution_paths_valid': True, 'all_boards_have_20_painted_cells': True,
              'all_required_colors_match': True, 'mixed_normal_cells': 0, 'forbidden_cells': 0,
              'self_revisits': 0, 'nonadjacent_steps': 0, 'other_color_start_visits': 0,
              'no_duplicates_under_axis_reflections_and_color_swap': True,
              'solution_uniqueness_checked': False, 'player_difficulty_tested': False,
              'implemented_in_game': bool(data.get('implemented_in_game')),
              'data_sha256': hashlib.sha256((ROOT/'stages-31-60.json').read_bytes()).hexdigest(),
              'checks': rows}
    (ROOT/'validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    return report


if __name__ == '__main__':
    result = verify()
    print(json.dumps({k:v for k,v in result.items() if k != 'checks'}, ensure_ascii=False, indent=2))
