"""Orthographic previews of Skyrim SE static NIFs, for auditing vanilla pieces without
the Creation Kit: front (X-Z, seen from -Y), side (Y-Z, seen from +X) and top (X-Y),
flat-shaded, one PNG per mesh.

    python tools/bsa_extract.py --data "<Data>" --extract meshes/.../walkway01.nif --out build/vanilla
    python tools/nif_preview.py build/previews build/vanilla/walkway01.nif

Written for the stage asset audit (docs/AUDIT.md). Two things it had to learn:
- SSE (BS 100) BSTriShape positions are always full-precision floats, whatever the
  vertex-attribute flag says; reading them as halves gives garbage.
- Bounds lie about usable surfaces. Walkway01's OBND is 272 wide but its planks are 255,
  so tiling by bounds leaves slits. Measure the faces you intend to use.

Inspection only: nothing extracted or rendered is redistributed.
"""
import math
import struct
import sys
from pathlib import Path

from PIL import Image, ImageDraw

SHAPES = {"BSTriShape", "BSLODTriShape", "BSMeshLODTriShape", "BSSubIndexTriShape", "BSDynamicTriShape"}
NODES = {"NiNode", "BSFadeNode", "BSMultiBoundNode", "BSLeafAnimNode", "BSOrderedNode", "BSValueNode", "NiSwitchNode", "NiLODNode", "BSTreeNode", "NiBillboardNode"}


class R:
    def __init__(self, d, p=0):
        self.d, self.p = d, p

    def read(self, n):
        v = self.d[self.p:self.p + n]
        self.p += n
        return v

    def g(self, f):
        return struct.unpack("<" + f, self.read(struct.calcsize("<" + f)))[0]

    def s(self):
        return self.read(self.g("I")).decode("latin1")


def half(h):
    return struct.unpack("<e", struct.pack("<H", h))[0]


def load(path):
    r = R(Path(path).read_bytes())
    r.p = r.d.index(b"\n") + 1
    version = r.g("I"); r.g("B"); user = r.g("I"); count = r.g("I"); bs = r.g("I")
    assert version == 0x14020007 and bs == 100, (hex(version), bs)
    for _ in range(3):
        r.read(r.g("B"))
    types = [r.s() for _ in range(r.g("H"))]
    idx = [r.g("H") for _ in range(count)]
    sizes = [r.g("I") for _ in range(count)]
    ns = r.g("I"); r.g("I")
    strings = [r.s() for _ in range(ns)]
    r.read(r.g("I") * 4)
    blocks = []
    for t, size in zip(idx, sizes):
        blocks.append((types[t], r.read(size)))
    return blocks, strings


def av(q):
    """NiAVObject header (BS 100): returns (flags, translation, rotation rows, scale, collision)."""
    q.g("i")  # name
    q.read(q.g("I") * 4)  # extra data
    q.g("i")  # controller
    flags = q.g("I")
    t = struct.unpack("<3f", q.read(12))
    rot = struct.unpack("<9f", q.read(36))
    s = q.g("f")
    coll = q.g("i")
    return flags, t, rot, s, coll


def mul(a, b):  # 3x3 row-major
    return [sum(a[r * 3 + k] * b[k * 3 + c] for k in range(3)) for r in range(3) for c in range(3)]


def apply(xf, v):
    rot, t, s = xf
    return tuple(s * sum(rot[r * 3 + c] * v[c] for c in range(3)) + t[r] for r in range(3))


def compose(parent, local):
    prot, pt, ps = parent
    lrot, lt, ls = local
    rot = mul(prot, lrot)
    t = apply(parent, lt)
    return rot, t, ps * ls


def triangles(path):
    blocks, strings = load(path)
    tris = []
    children = {}
    locals_ = {}
    shapes = {}
    for i, (kind, data) in enumerate(blocks):
        q = R(data)
        if kind in NODES:
            flags, t, rot, s, _ = av(q)
            n = q.g("I")
            children[i] = [q.g("i") for _ in range(n)]
            locals_[i] = (list(rot), t, s)
        elif kind in SHAPES:
            flags, t, rot, s, _ = av(q)
            locals_[i] = (list(rot), t, s)
            q.read(16)  # bound
            q.g("i"); q.g("i"); q.g("i")  # skin, shader, alpha
            desc = q.g("Q")
            nt = q.g("I") if kind == "BSSubIndexTriShape" and False else q.g("H")
            nv = q.g("H")
            size = q.g("I")
            if size == 0 or nv == 0:
                continue
            stride = (desc & 15) * 4
            attrs = desc >> 44
            full = True  # SSE (BS 100) stores positions as floats whatever the flag says
            verts = []
            for _ in range(nv):
                raw = q.read(stride)
                if full:
                    verts.append(struct.unpack_from("<3f", raw, 0))
                else:
                    hx, hy, hz = struct.unpack_from("<3H", raw, 0)
                    verts.append((half(hx), half(hy), half(hz)))
            faces = [struct.unpack("<3H", q.read(6)) for _ in range(nt)]
            shapes[i] = (verts, faces, flags)
    # walk from the root
    def walk(i, xf):
        local = locals_.get(i)
        if local is None:
            return
        world = compose(xf, local)
        if i in shapes:
            verts, faces, flags = shapes[i]
            if flags & 1:  # hidden
                return
            wv = [apply(world, v) for v in verts]
            for a, b, c in faces:
                tris.append((wv[a], wv[b], wv[c]))
        for ch in children.get(i, []):
            if ch >= 0:
                walk(ch, world)
    walk(0, ([1, 0, 0, 0, 1, 0, 0, 0, 1], (0, 0, 0), 1.0))
    return tris


def render(tris, axes, depth_axis, depth_sign, size, bounds, colour):
    (a0, a1), = [axes]
    lo = [bounds[0][a0], bounds[0][a1]]
    hi = [bounds[1][a0], bounds[1][a1]]
    span = max(hi[0] - lo[0], hi[1] - lo[1], 1)
    scale = (size - 20) / span
    img = Image.new("RGB", (size, size), (238, 238, 232))
    d = ImageDraw.Draw(img)
    light = (0.3, -0.5, 0.8)
    order = sorted(tris, key=lambda t: depth_sign * sum(v[depth_axis] for v in t))
    for t in order:
        ux = [t[1][k] - t[0][k] for k in range(3)]
        vx = [t[2][k] - t[0][k] for k in range(3)]
        n = (ux[1] * vx[2] - ux[2] * vx[1], ux[2] * vx[0] - ux[0] * vx[2], ux[0] * vx[1] - ux[1] * vx[0])
        ln = math.sqrt(sum(c * c for c in n)) or 1
        shade = abs(sum(n[k] / ln * light[k] for k in range(3)))
        c = tuple(int(colour[k] * (0.35 + 0.65 * shade)) for k in range(3))
        pts = [(10 + (v[a0] - lo[0]) * scale, size - 10 - (v[a1] - lo[1]) * scale) for v in t]
        d.polygon(pts, fill=c)
    # ground line (z = 0) where it applies
    if a1 == 2 and lo[1] < 0 < hi[1]:
        y = size - 10 - (0 - lo[1]) * scale
        d.line([(0, y), (size, y)], fill=(200, 0, 0))
    return img


def sheet(path, out, size=300):
    tris = triangles(path)
    if not tris:
        print("no geometry", path)
        return
    xs = [v for t in tris for v in t]
    bounds = ([min(v[k] for v in xs) for k in range(3)], [max(v[k] for v in xs) for k in range(3)])
    colour = (150, 110, 70)
    views = [
        render(tris, (0, 2), 1, -1, size, bounds, colour),   # front: from -Y, far first = +Y first
        render(tris, (1, 2), 0, 1, size, bounds, colour),    # side: from +X
        render(tris, (0, 1), 2, 1, size, bounds, colour),    # top: from +Z
    ]
    img = Image.new("RGB", (size * 3 + 20, size + 24), (255, 255, 255))
    for i, v in enumerate(views):
        img.paste(v, (i * (size + 10), 24))
    d = ImageDraw.Draw(img)
    name = Path(path).stem
    d.text((4, 4), f"{name}  x {bounds[0][0]:.0f}..{bounds[1][0]:.0f}  y {bounds[0][1]:.0f}..{bounds[1][1]:.0f}  "
                   f"z {bounds[0][2]:.0f}..{bounds[1][2]:.0f}   [front from -Y | side from +X | top]  tris {len(tris)}",
           fill=(0, 0, 0))
    img.save(out)


if __name__ == "__main__":
    outdir = Path(sys.argv[1])
    outdir.mkdir(exist_ok=True)
    for p in sys.argv[2:]:
        try:
            sheet(p, outdir / (Path(p).stem + ".png"))
        except Exception as e:  # keep going
            print("FAILED", p, e)
