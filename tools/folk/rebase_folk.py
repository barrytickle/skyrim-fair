"""Re-base Astra's paired folk dance so each dancer's clip starts at its own feet.

The two clips (character-actors/folk-dance/folk_turn_A.hkx, male; folk_turn_B.hkx,
female) share one origin: each dancer's root orbits the common centre, starting 26 units
either side of it, facing the partner. Played on two NPCs that way, both NPCs must stand
on the same spot, and the game pushes overlapping actors apart, which breaks the hold.

So only the root track is rewritten, relative to its first frame:
    R'(t) = R0^-1 R(t),  T'(t) = R0^-1 (T(t) - T0)
and each NPC is placed where its dancer starts (printed below; the generator's
fairWorld.folkDance.dancers carry the same offsets). The NPCs then stand 52 units apart,
and their bodies still meet over the centre. Every other bone is untouched.

The clips replace vanilla's special_cicerodance1.hkx through Open Animation Replacer, only
for the fair's two folk dancers (the generator writes the OAR conditions).

    python tools/folk/rebase_folk.py

Plain Python: the vendored PyNifly HKX codec (GPL-3.0, character-actors/folk-dance/vendor)
is imported, not bundled. Output is git-ignored, as the source animation is Astra's.
"""
import argparse, hashlib, math, pathlib, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
SRC = ROOT / "character-actors" / "folk-dance"
OUT = ROOT / "assets" / "meshes" / "actors" / "character" / "animations" / "OpenAnimationReplacer" / "SkyrimFairFolk"
CLIPS = [("folk_turn_A.hkx", "Male"), ("folk_turn_B.hkx", "Female")]
TARGET = "special_cicerodance1.hkx"


def qmul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return [aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz]


def qinv(q):
    return [-q[0], -q[1], -q[2], q[3]]


def qrot(q, v):
    p = qmul(qmul(q, [v[0], v[1], v[2], 0.0]), qinv(q))
    return p[:3]


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--src", default=str(SRC))
    ap.add_argument("--out", default=str(OUT))
    ap.add_argument("--repeat", type=int, default=6, help="loops chained into one clip (fairWorld.folkDance.length = 9.6 x this)")
    args = ap.parse_args()
    src, out = pathlib.Path(args.src), pathlib.Path(args.out)
    sys.path.insert(0, str(src / "vendor" / "hkx"))
    sys.path.insert(0, str(src))
    import hkx_block_adapter
    hkx_block_adapter.install()
    from anim_skyrim import load_skyrim_animation, write_skyrim_animation

    for name, sub in CLIPS:
        path = src / name
        anim = load_skyrim_animation(str(path))
        assert anim.bone_names[0] == "NPC Root [Root]", anim.bone_names[0]
        root = anim.tracks[0]
        t0, r0 = list(root.translations[0]), list(root.rotations[0])
        # A pure turn about Z is expected; anything else would need a tilted placement.
        assert abs(r0[0]) < 1e-4 and abs(r0[1]) < 1e-4, r0
        inv = qinv(r0)
        root.translations = [qrot(inv, [t[0] - t0[0], t[1] - t0[1], t[2] - t0[2]]) for t in root.translations]
        root.rotations = [qmul(inv, r) for r in root.rotations]
        # The start facing, as a Skyrim heading (degrees clockwise from +Y).
        ccw = math.degrees(2 * math.atan2(r0[2], r0[3]))
        heading = (-ccw) % 360
        for i, bone in enumerate(anim.bone_names[1:3], start=1):
            moved = max(abs(a - b) for t in anim.tracks[i].translations for a, b in zip(t, anim.tracks[i].translations[0]))
            assert moved < 0.01, f"{bone} carries motion ({moved:.3f}); only the root is re-based"

        # Chain the loop REPEAT times into one clip: the dance is authored as a seamless loop
        # (its last sample equals its first), and a vanilla idle plays its clip once, so the
        # script restarts it only every REPEAT loops instead of every one.
        if args.repeat > 1:
            loop = anim.num_frames - 1
            for t in anim.tracks:
                t.translations = t.translations[:loop] * args.repeat + t.translations[:1]
                t.rotations = t.rotations[:loop] * args.repeat + t.rotations[:1]
                t.scales = t.scales[:loop] * args.repeat + t.scales[:1]
            anim.num_frames = loop * args.repeat + 1
            anim.duration = loop * args.repeat * anim.frame_duration
            root = anim.tracks[0]

        target = out / sub / TARGET
        target.parent.mkdir(parents=True, exist_ok=True)
        write_skyrim_animation(str(target), anim, ptr_size=path.read_bytes()[16])

        # Round trip: the written root must match what was meant, within the codec's error.
        back = load_skyrim_animation(str(target))
        err = max(abs(a - b) for x, y in zip(back.tracks[0].translations, root.translations) for a, b in zip(x, y))
        sha = hashlib.sha256(target.read_bytes()).hexdigest()
        print(f"{name} -> {target.relative_to(ROOT)}")
        print(f"  dancer starts at ({t0[0]:.2f}, {t0[1]:.2f}) from the centre, heading {heading:.1f}; "
              f"{anim.num_frames} frames, {anim.duration:.2f} s; root round-trip error {err:.4f}; sha256 {sha[:16]}")


if __name__ == "__main__":
    main()
