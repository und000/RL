"""Check serialized pacing constraints and new room geometry without launching Unity."""
from pathlib import Path
import collections
import math
import re

ROOT = Path(__file__).resolve().parents[1] / 'Assets/Survivor_Game'


def read(p):
    return p.read_text(encoding='utf-8-sig')


def integer(text, field):
    return int(re.search(r'^\s+' + field + r': (\d+)$', text, re.M)[1])


ordinary = combat = minimum = maximum = 0
for chapter in range(1, 7):
    floors = sorted((ROOT / 'Data/Rooms/Floors').glob(f'FloorProfile_C{chapter}_F*.asset'))
    assert len(floors) == (4 if chapter <= 3 else 5)
    for path in floors:
        s = read(path)
        count = integer(s, 'roomCount')
        first = path.stem == 'FloorProfile_C1_F1'
        assert count == 7 if first else count in (5, 6)
        assert integer(s, 'treasureRoomCount') == 1
        assert integer(s, 'shopRoomCount') == (0 if first else 1)
        assert integer(s, 'eliteRoomCount') == 1
        fights = count - 1 - integer(s, 'treasureRoomCount') - integer(s, 'shopRoomCount')
        ordinary += count
        combat += fights
        minimum += (fights - 1) * integer(s, 'normalMinCount') + integer(s, 'eliteMinCount')
        maximum += (fights - 1) * integer(s, 'normalMaxCount') + integer(s, 'eliteMaxCount')
        normal = re.search(r'^  - kind: 1\n    prefabs:\n((?:    - .+\n)+)', s, re.M)[1]
        assert len(re.findall('guid:', normal)) == 4
        shop = s[s.index('    - kind: 4\n', s.index('  rewardPlan:')):]
        assert 1 <= float(re.search(r'priceMultiplier: ([\d.]+)', shop)[1]) <= 2
assert ordinary == 146 and combat == 66
first = read(ROOT / 'Data/Rooms/Floors/FloorProfile_C1_F1.asset')
assert integer(first, 'keepNonCombatRoomsAwayFromStart') == 1
assert 4 * integer(first, 'normalMinCount') * integer(first, 'creditsPerEnemy') >= 120
print(f'PASS: 27 ordinary stages, {ordinary + 12} total rooms, {combat} ordinary combat rooms, {minimum}-{maximum} ordinary enemies; first-stage and shop budgets.')

for name in ('Columns', 'Galleries', 'Islands', 'Flanks'):
    s = read(ROOT / f'Prefabs/Rooms/Room_Normal_{name}.prefab')
    blocks = re.split(r'(?=^--- !u!)', s, flags=re.M)
    transforms = {re.search(r'^--- !u!4 &(\d+)', b)[1]: b for b in blocks if b.startswith('--- !u!4 ')}
    obstacles = []
    spawns = []
    for block in blocks:
        if not block.startswith('--- !u!1 '):
            continue
        obj = re.search(r'm_Name: (.*)', block)[1]
        if not obj.startswith(('Obstacle_', 'SpawnPoint_')):
            continue
        transform = transforms[re.search(r'component: \{fileID: (\d+)\}', block)[1]]
        x, y = map(float, re.search(r'm_LocalPosition: \{x: ([\d.-]+), y: ([\d.-]+)', transform).groups())
        if obj.startswith('SpawnPoint_'):
            spawns.append((x, y))
        else:
            w, h = map(float, re.search(r'm_LocalScale: \{x: ([\d.-]+), y: ([\d.-]+)', transform).groups())
            obstacles.append((x, y, w, h))
    def free(p, clearance):
        return all(abs(p[0] - x) > w / 2 + clearance or abs(p[1] - y) > h / 2 + clearance for x, y, w, h in obstacles)
    cells = {(x, y) for x in range(-22, 23) for y in range(-14, 15) if free((x, y), 1)}
    assert (0, 0) in cells
    seen = {(0, 0)}
    pending = collections.deque(seen)
    while pending:
        x, y = pending.popleft()
        for nxt in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if nxt in cells and nxt not in seen:
                pending.append(nxt)
                seen.add(nxt)
    for entry in ((0, 13), (0, -13), (21, 0), (-21, 0)):
        assert entry in seen
        assert sum(math.dist(entry, p) >= 8 for p in spawns) >= 6
    assert len(spawns) == 14 and all(p in seen and free(p, 2) for p in spawns)
    assert f'designId: Room_Normal_{name}' in s
print('PASS: four room designs, four connected entrances and 56 clear spawn anchors (grid approximation, not Unity collision testing).')
