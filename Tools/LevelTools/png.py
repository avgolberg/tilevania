"""Minimal stdlib PNG reader/writer + helpers for inspecting pixel-art sheets."""
import struct, zlib, sys


def read_png(path):
    data = open(path, 'rb').read()
    assert data[:8] == b'\x89PNG\r\n\x1a\n'
    pos = 8
    idat = b''
    plte = None
    trns = None
    while pos + 8 <= len(data):
        ln, = struct.unpack('>I', data[pos:pos + 4]); typ = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + ln]; pos += 12 + ln
        if typ == b'IHDR':
            w, h, bd, ct, _, _, il = struct.unpack('>IIBBBBB', body)
        elif typ == b'PLTE':
            plte = [tuple(body[i:i + 3]) for i in range(0, len(body), 3)]
        elif typ == b'tRNS':
            trns = body
        elif typ == b'IDAT':
            idat += body
    assert il == 0, 'interlaced unsupported'
    raw = zlib.decompress(idat)
    chans = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[ct]
    if bd < 8:
        assert ct in (0, 3)
    bpp_bits = chans * bd
    stride = (w * bpp_bits + 7) // 8
    bpp = max(1, bpp_bits // 8)
    rows = []
    prev = bytearray(stride)
    i = 0
    for y in range(h):
        f = raw[i]; i += 1
        line = bytearray(raw[i:i + stride]); i += stride
        for x in range(stride):
            a = line[x - bpp] if x >= bpp else 0
            b = prev[x]
            c = prev[x - bpp] if x >= bpp else 0
            if f == 1: line[x] = (line[x] + a) & 255
            elif f == 2: line[x] = (line[x] + b) & 255
            elif f == 3: line[x] = (line[x] + ((a + b) >> 1)) & 255
            elif f == 4:
                p = a + b - c; pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pr = a if pa <= pb and pa <= pc else (b if pb <= pc else c)
                line[x] = (line[x] + pr) & 255
        rows.append(line); prev = line
    px = []
    for line in rows:
        out = []
        if bd < 8:
            vals = []
            for byte in line:
                for s in range(8 - bd, -1, -bd):
                    vals.append((byte >> s) & ((1 << bd) - 1))
            vals = vals[:w]
        else:
            step = bd // 8
            vals = [line[k] if step == 1 else (line[k] << 8 | line[k + 1]) >> 8 for k in range(0, len(line), step)]
        for x in range(w):
            if ct == 6: out.append(tuple(vals[x * 4:x * 4 + 4]))
            elif ct == 2:
                r, g, b_ = vals[x * 3:x * 3 + 3]
                out.append((r, g, b_, 255))
            elif ct == 3:
                idx = vals[x]; r, g, b_ = plte[idx]
                a = trns[idx] if trns and idx < len(trns) else 255
                out.append((r, g, b_, a))
            elif ct == 0:
                v = vals[x]
                if bd < 8: v = v * 255 // ((1 << bd) - 1)
                out.append((v, v, v, 255))
            elif ct == 4: out.append((vals[x * 2], vals[x * 2], vals[x * 2], vals[x * 2 + 1]))
        px.append(out)
    return w, h, px


def write_png(path, w, h, px):
    raw = b''.join(b'\x00' + bytes([c for p in row for c in p]) for row in px)
    def chunk(t, b):
        return struct.pack('>I', len(b)) + t + b + struct.pack('>I', zlib.crc32(t + b) & 0xffffffff)
    open(path, 'wb').write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
                           + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))


def zoom_grid(src, dst, scale=4, grid=16, x0=0, y0=0, x1=None, y1=None):
    """Zoom a region; draw grid lines every `grid` px; checker transparent bg."""
    w, h, px = read_png(src)
    x1 = x1 or w; y1 = y1 or h
    W, H = (x1 - x0) * scale, (y1 - y0) * scale
    out = []
    for Y in range(H):
        sy = y0 + Y // scale
        row = []
        for X in range(W):
            sx = x0 + X // scale
            if grid and ((sx % grid == 0 and X % scale == 0) or (sy % grid == 0 and Y % scale == 0)):
                row.append((255, 0, 255, 255)); continue
            r, g, b, a = px[sy][sx]
            bg = 200 if ((X // 8 + Y // 8) % 2) else 160
            row.append((int((r * a + bg * (255 - a)) / 255), int((g * a + bg * (255 - a)) / 255),
                        int((b * a + bg * (255 - a)) / 255), 255))
        out.append(row)
    write_png(dst, W, H, out)


def opaque_bounds(px, x0, y0, x1, y1):
    xs = [x for y in range(y0, y1) for x in range(x0, x1) if px[y][x][3] > 0]
    ys = [y for y in range(y0, y1) for x in range(x0, x1) if px[y][x][3] > 0]
    if not xs: return None
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def column_runs(px, w, h):
    """Return runs of non-empty columns (sprite separation)."""
    cols = [any(px[y][x][3] > 0 for y in range(h)) for x in range(w)]
    runs = []; s = None
    for x, c in enumerate(cols + [False]):
        if c and s is None: s = x
        if not c and s is not None: runs.append((s, x)); s = None
    return runs


if __name__ == '__main__':
    cmd = sys.argv[1]
    if cmd == 'zoom':
        a = sys.argv
        zoom_grid(a[2], a[3], int(a[4]), int(a[5]), *[int(v) for v in a[6:]])
    elif cmd == 'runs':
        w, h, px = read_png(sys.argv[2])
        for r in column_runs(px, w, h):
            print(r, opaque_bounds(px, r[0], 0, r[1], h))
    elif cmd == 'grid':  # list non-empty cells of a grid
        w, h, px = read_png(sys.argv[2]); g = int(sys.argv[3])
        for gy in range(h // g):
            print(''.join('#' if opaque_bounds(px, gx * g, gy * g, gx * g + g, gy * g + g) else '.' for gx in range(w // g)), gy)
