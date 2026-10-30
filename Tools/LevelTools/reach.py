"""Conservative reachability check for TileVania ASCII layouts.
Player: 2 cells tall, jump <= 3 up / 4 across (actual: ~3.5 up, ~5 across)."""
import sys
from collections import deque

def load(path):
    lines = open(path).read().splitlines()[1:]
    lines = [l for l in lines if l]
    h = len(lines); w = max(len(l) for l in lines)
    g = {}
    for r, l in enumerate(lines):
        for x in range(w):
            g[(x, h - 1 - r)] = l[x] if x < len(l) else '.'
    return g, w, h

def analyse(path, keys_order):
    g, w, h = load(path)
    gates = {}
    for (x, y), c in g.items():
        if c in 'LD':
            for dx in (0, 1):
                for dy in (0, 1): gates[(x + dx, y + dy)] = c
    def solid(x, y, opened):
        if x < 0 or x >= w or y < 0: return True
        if y >= h: return False
        if (x, y) in gates and gates[(x, y)] not in opened: return True
        return g[(x, y)] == '#'
    def oneway(x, y): return g.get((x, y)) in ('=', 'I')
    def ladder(x, y): return g.get((x, y)) in ('H', 'I')
    def free(x, y, o): return not solid(x, y, o) and not solid(x, y + 1, o)
    def support(x, y, o): return solid(x, y - 1, o) or oneway(x, y - 1)
    def deadly(x, y): return g.get((x, y)) == '^'
    def stand(x, y, o): return free(x, y, o) and (support(x, y, o) or ladder(x, y) or ladder(x, y - 1)) and not deadly(x, y)

    def fall(x, y, o):
        while y >= 0 and free(x, y, o) and not support(x, y, o) and not ladder(x, y):
            y -= 1
        if y < 0 or deadly(x, y) or not free(x, y, o): return None
        return (x, y)

    def reach(start, opened):
        seen = {start}; q = deque([start])
        while q:
            x, y = q.popleft()
            nxt = []
            for dx in (-1, 1):
                if free(x + dx, y, opened): nxt.append(fall(x + dx, y, opened))
            if ladder(x, y) or ladder(x, y + 1): nxt.append((x, y + 1) if free(x, y + 1, opened) else None)
            if ladder(x, y) or ladder(x, y - 1):
                nxt.append((x, y - 1) if free(x, y - 1, opened) or oneway(x, y - 1) else None)
            if oneway(x, y - 1) and not solid(x, y - 1, opened): nxt.append(fall(x, y - 1, opened))
            # jumps
            for up in range(0, 4):
                if not all(free(x, y + k, opened) for k in range(up + 1)): break
                top = y + up
                for d in (-1, 1):
                    for dx in range(1, 6 if up <= 2 else 5):
                        cx = x + d * dx
                        if not free(cx, top, opened): break
                        if stand(cx, top, opened): nxt.append((cx, top))
                        else:
                            f = fall(cx, top, opened)
                            nxt.append(f)
            for n in nxt:
                if n and n not in seen and 0 <= n[0] < w and n[1] < h:
                    seen.add(n); q.append(n)
        return seen

    start = next(p for p, c in g.items() if c == 'P')
    def near(cells, target):
        tx, ty = target
        return any(abs(x - tx) <= 1 and 0 <= ty - y <= 1 for x, y in cells)
    opened = set()
    ok = True
    for item, gate in keys_order:
        cells = reach(start, opened)
        targets = [p for p, c in g.items() if c == item]
        for t in targets:
            r = near(cells, t)
            print(f"  {item} at {t}: {'reachable' if r else 'NOT REACHABLE'} (gates open: {''.join(sorted(opened)) or '-'})")
            ok &= r
        if gate: opened.add(gate)
    cells = reach(start, opened)
    for c in 'gchm$sokKC':
        pass
    unreached = [p for p, c in g.items() if c in 'g$ch' and not near(cells, p)]
    print("  unreachable pickups/chests:", unreached or 'none')
    return ok

ok = True
base = sys.argv[1]
print("Level1"); ok &= analyse(base + "/Level1_Forest.txt", [('k', 'L'), ('E', None)])
print("Level2"); ok &= analyse(base + "/Level2_Castle.txt", [('k', 'L'), ('K', 'D'), ('E', None)])
print("Boss");   ok &= analyse(base + "/Level3_TreasureChamber.txt", [('T', None)])
print("ALL OK" if ok else "PROBLEMS")
