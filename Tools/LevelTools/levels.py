"""Generates ASCII level layouts (y=0 is the bottom row) and colored previews."""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from png import write_png

OUT = sys.argv[1]
PREV = sys.argv[2]


class Map:
    def __init__(s, w, h):
        s.w, s.h = w, h
        s.g = [['.'] * w for _ in range(h)]

    def put(s, x, y, ch):
        if 0 <= x < s.w and 0 <= y < s.h: s.g[y][x] = ch

    def rect(s, x0, y0, x1, y1, ch='#'):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1): s.put(x, y, ch)

    def row(s, x0, x1, y, ch):
        for x in range(x0, x1 + 1): s.put(x, y, ch)

    def col(s, x, y0, y1, ch):
        for y in range(y0, y1 + 1): s.put(x, y, ch)

    def text(s, header):
        lines = [header]
        for y in range(s.h - 1, -1, -1): lines.append(''.join(s.g[y]))
        return '\n'.join(lines) + '\n'


COLORS = {'#': (110, 80, 60), '=': (190, 140, 70), 'I': (220, 170, 60), 'H': (230, 200, 90), '^': (255, 255, 255),
          'P': (0, 255, 0), 'C': (255, 140, 0), 'g': (0, 220, 255), 'h': (255, 80, 120), 'k': (255, 220, 0),
          'K': (80, 120, 255), 'L': (200, 200, 0), 'D': (60, 60, 255), 'c': (160, 90, 40), '$': (230, 230, 230),
          'E': (0, 200, 80), 's': (255, 0, 0), 'm': (255, 120, 200), 'o': (180, 0, 255), 'b': (140, 0, 0),
          'p': (255, 255, 0), 't': (255, 100, 0), 'w': (80, 80, 160), ',': (60, 150, 60), '*': (220, 60, 60),
          '|': (150, 150, 150), 'A': (120, 120, 140), 'B': (255, 0, 255), 'T': (255, 215, 0), 'X': (255, 0, 128),
          'Z': (90, 90, 90), '.': (25, 25, 40)}


def preview(m, path, scale=6):
    W, H = m.w * scale, m.h * scale
    px = []
    for Y in range(H):
        y = m.h - 1 - Y // scale
        row = []
        for X in range(W):
            ch = m.g[y][X // scale]
            row.append(COLORS.get(ch, (255, 0, 255)) + (255,))
        px.append(row)
    write_png(path, W, H, px)


# ---------------------------------------------------------------- Level 1: Forest
def level1():
    m = Map(128, 30)
    m.rect(0, 0, 0, 29)            # left wall
    m.rect(127, 0, 127, 29)        # right wall
    # Section A: tutorial meadow
    m.rect(1, 0, 22, 7)
    m.put(3, 8, 'P')
    for x in (8, 9, 10): m.put(x, 8, 'g')
    m.row(12, 15, 10, '=')
    m.put(13, 11, 'g'); m.put(14, 11, 'g')
    m.put(19, 8, 'm')
    for x in (5, 11, 16, 21): m.put(x, 8, ',')
    m.put(7, 8, '*')
    # spike dip
    m.rect(23, 0, 26, 5)
    m.row(23, 26, 6, '^')
    m.put(23, 10, 'g'); m.put(24, 11, 'g'); m.put(25, 11, 'g'); m.put(26, 10, 'g')
    # meadow 2 + checkpoint + step
    m.rect(27, 0, 33, 7)
    m.put(29, 8, 'C')
    m.rect(34, 0, 62, 9)
    m.put(40, 10, 's')
    m.put(31, 8, ','); m.put(36, 10, '*'); m.put(38, 10, ',')
    # Section B: ruins + ladder to key ledge
    m.col(45, 10, 15, 'H'); m.put(45, 16, 'I')
    m.row(42, 52, 16, '='); m.put(45, 16, 'I')
    m.put(51, 17, 'k')
    m.put(43, 17, 'g'); m.put(44, 17, 'g'); m.put(48, 17, 'g')
    m.put(41, 10, '|'); m.put(54, 10, '|')
    m.row(55, 58, 12, '=')
    m.put(56, 13, 'g'); m.put(57, 13, 'g')
    m.put(59, 15, 'o')
    m.put(49, 10, ','); m.put(60, 10, '*')
    # bottomless pit 63..66
    # Section C: surface + shaft entrance
    m.rect(67, 0, 126, 9)
    m.put(70, 10, 'C')
    m.put(73, 10, ','); m.put(68, 10, '*')
    m.put(76, 10, 'A')                    # ruin arch over the shaft (4x4 decor)
    m.col(78, 2, 8, 'H'); m.put(78, 9, 'I')
    # underground cave (interior y2..6), connected to the shaft
    m.rect(78, 2, 124, 6, '.')
    m.col(78, 2, 8, 'H'); m.put(78, 9, 'I')
    for x in (82, 84, 86): m.put(x, 2, 'g')
    m.put(90, 2, 's')
    m.row(95, 96, 2, '^')
    m.put(95, 5, 'g'); m.put(96, 5, 'g')
    m.put(100, 2, 'C')
    m.put(103, 5, 'p')
    m.put(106, 2, 'c')
    m.put(111, 2, 's')
    m.row(98, 101, 4, '=')
    m.put(99, 5, 'h')
    # gold gate (2 wide x 2 tall) and the portal chamber
    m.rect(114, 4, 115, 6)                # wall above gate
    m.put(114, 2, 'L')
    m.put(118, 2, '$')
    m.put(121, 2, 'E')
    m.put(124, 2, 'g')
    # surface above the cave
    for x in (84, 85, 86, 92, 93, 108, 109, 110): m.put(x, 10, 'g')
    m.row(88, 91, 12, '='); m.put(89, 13, 'g'); m.put(90, 13, 'g')
    m.put(98, 10, 's')
    m.put(104, 14, 'o')
    m.row(116, 119, 12, '='); m.put(118, 13, 'h')
    for x in (82, 95, 101, 113, 122): m.put(x, 10, ',')
    m.put(87, 10, '*'); m.put(106, 10, '*')
    return m


# ---------------------------------------------------------------- Level 2: Castle
def level2():
    m = Map(140, 34)
    m.rect(0, 0, 0, 33); m.rect(139, 0, 139, 33)
    m.rect(1, 0, 39, 2)
    m.put(3, 3, 'P')
    m.put(2, 6, 't'); m.put(12, 6, 't'); m.put(7, 6, 'w')
    for x in (8, 9, 10): m.put(x, 3, 'g')
    m.row(5, 8, 5, '='); m.put(6, 6, 'g'); m.put(7, 6, 'g')
    # spike hall with wooden bridges
    m.row(17, 21, 3, '^'); m.row(26, 30, 3, '^')
    m.row(16, 22, 5, '='); m.row(25, 31, 5, '=')
    m.put(19, 6, 'g'); m.put(28, 6, 'g')
    m.put(24, 9, 'b')
    m.put(15, 7, 't'); m.put(33, 7, 't'); m.put(23, 8, 'w')
    m.put(36, 3, 'C')
    m.put(34, 3, 'm')
    # bottomless gap with pillars 40..58
    for x0 in (43, 48, 53):
        m.rect(x0, 0, x0 + 1, 4)
        m.put(x0, 5, 'g')
    m.put(45, 9, 'b'); m.put(51, 10, 'b')
    m.rect(59, 0, 138, 2)
    m.put(58, 11, 'o')
    # tower climb to the gold key
    m.col(62, 3, 10, 'H'); m.row(60, 68, 11, '='); m.put(62, 11, 'I')
    m.col(67, 12, 18, 'H'); m.row(63, 74, 19, '='); m.put(67, 19, 'I')
    m.put(73, 20, 'k')
    m.put(64, 12, 'g'); m.put(65, 12, 'g'); m.put(70, 20, 'g'); m.put(71, 20, 'g')
    m.put(64, 16, 'p')
    m.put(72, 14, 'o')
    m.put(70, 3, 's')
    m.put(61, 6, 't'); m.put(69, 6, 't'); m.put(65, 7, 'w')
    m.put(76, 3, 'C')
    # gold gate wall
    m.rect(80, 5, 81, 33)
    m.put(80, 3, 'L')
    m.put(78, 6, 't')
    # spike floor with bridges
    m.row(84, 100, 3, '^')
    m.row(84, 88, 5, '='); m.row(91, 94, 5, '='); m.row(97, 100, 5, '=')
    m.put(92, 6, 's')
    m.put(86, 6, 'g'); m.put(93, 8, 'g'); m.put(99, 6, 'g')
    m.put(89, 10, 'b')
    m.put(96, 12, 'o')
    m.put(83, 6, 't'); m.put(102, 7, 't')
    m.put(102, 3, 'C')
    # treasure key tower
    m.col(104, 3, 12, 'H'); m.row(100, 110, 13, '='); m.put(104, 13, 'I')
    m.put(109, 14, 'K')
    m.put(101, 14, 'm')
    m.put(106, 14, '$')
    m.put(107, 9, 'p')
    # final hall
    m.put(113, 3, 'c')
    m.put(116, 3, 's'); m.put(122, 3, 's')
    m.put(119, 10, 'o')
    m.row(115, 119, 5, '='); m.put(116, 6, 'g'); m.put(117, 6, 'g'); m.put(118, 6, 'h')
    m.put(112, 6, 't'); m.put(124, 6, 't'); m.put(118, 5, 'w')
    m.rect(126, 5, 127, 33)
    m.put(126, 3, 'D')
    m.put(132, 3, 'E')
    m.put(130, 6, 't'); m.put(136, 6, 't')
    for x in (129, 130, 135, 136): m.put(x, 3, 'g')
    return m


# ---------------------------------------------------------------- Boss arena
def boss():
    m = Map(40, 22)
    m.rect(0, 0, 0, 21); m.rect(39, 0, 39, 21)
    m.rect(1, 0, 38, 2)
    m.rect(1, 6, 9, 21)       # corridor ceiling
    m.put(2, 3, 'P')
    m.put(4, 3, 'C')
    m.put(9, 3, 'Z')          # arena door (closes behind the player)
    m.put(12, 3, 'X')         # fight trigger
    m.row(13, 17, 5, '='); m.row(31, 35, 5, '=')
    m.row(20, 28, 8, '=')
    m.put(24, 17, 'B')
    m.put(24, 3, 'T')
    m.put(3, 4, 't'); m.put(7, 4, 't')
    m.put(11, 6, 't'); m.put(37, 6, 't'); m.put(18, 11, 't'); m.put(30, 11, 't')
    m.put(15, 13, 'w'); m.put(33, 13, 'w'); m.put(24, 15, 'w')
    m.put(14, 6, 'h'); m.put(34, 6, 'h')
    return m


for name, fn, header in [
    ('Level1_Forest', level1, '# theme=forest backwall=roof'),
    ('Level2_Castle', level2, '# theme=castle backwall=15'),
    ('Level3_TreasureChamber', boss, '# theme=castle backwall=21'),
]:
    m = fn()
    open(os.path.join(OUT, name + '.txt'), 'w', newline='\n').write(m.text(header))
    preview(m, os.path.join(PREV, name + '.png'))
    print(name, m.w, m.h)
