"""Read files out of a Skyrim SE BSA archive.

Standalone: no game, no MO2, no third-party tools.

    python tools/bsa_extract.py --data "<stock Data>" --find stonewallterracestairs
    python tools/bsa_extract.py --data "<stock Data>" \
        --extract "meshes/architecture/farmhouse/stonewall/stonewallterracestairs01.nif" \
        --out build/vanilla

Why this exists: almost everything Skyrim Fair places is a vanilla asset, and placing
one correctly needs its real geometry - which way its visible face points, where its
pivot sits, how tall its wall is. That geometry lives in the BSAs. Working from the
DynDOLOD LOD versions instead is a guess: they are simplified, and on DirtCliffs01 that
guess put the cliff face pointing into the platform with its missing back wall aimed at
the player.

Nothing is redistributed. Files are extracted to a scratch folder for inspection only.

Format notes for anyone maintaining this: SSE archives are version 105. Folder records
are 24 bytes (the 16-byte Oldrim layout is version 104). File data is LZ4 FRAME
compressed when the archive's compressed flag is set, XOR the file's own bit 30, and a
compressed entry is prefixed with its uncompressed size.
"""

import argparse
import os
import struct
import sys


def lz4_block(src, out):
    """Minimal LZ4 block decoder, appending to out. Matches may reach back into earlier
    blocks of the same frame (SSE archives link their 64 KB blocks), so the history is
    the whole of out, not just this block."""
    si = 0
    n = len(src)
    while si < n:
        token = src[si]; si += 1
        lit = token >> 4
        if lit == 15:
            while True:
                b = src[si]; si += 1
                lit += b
                if b != 255:
                    break
        out += src[si:si + lit]
        si += lit
        if si >= n:
            break
        offset = src[si] | (src[si + 1] << 8); si += 2
        mlen = token & 0x0F
        if mlen == 15:
            while True:
                b = src[si]; si += 1
                mlen += b
                if b != 255:
                    break
        mlen += 4
        start = len(out) - offset
        if start < 0:
            raise ValueError("LZ4 match reaches before the start of the frame")
        for k in range(mlen):
            out.append(out[start + k])


def lz4_frame(buf):
    """Decode an LZ4 frame: header, then a chain of blocks, then a zero terminator.

    Before 2026-09-23 each block was decoded on its own, which corrupted the tail of
    every file over 64 KB (the first block size): its matches into the previous block
    read zeros. A crash loading such a mesh (ElvenSword.nif) found it."""
    if struct.unpack_from("<I", buf, 0)[0] != 0x184D2204:
        raise ValueError("not an LZ4 frame")
    p = 4
    flg = buf[p]; p += 2
    if flg & 0x08:          # content size present
        p += 8
    if flg & 0x01:          # dictionary id present
        p += 4
    p += 1                  # header checksum
    block_checksum = bool(flg & 0x10)
    out = bytearray()
    while True:
        size = struct.unpack_from("<I", buf, p)[0]; p += 4
        if size == 0:
            break
        if size & 0x80000000:                      # stored uncompressed
            n = size & 0x7FFFFFFF
            out += buf[p:p + n]; p += n
        else:
            lz4_block(buf[p:p + size], out)
            p += size
        if block_checksum:
            p += 4
    return bytes(out)


def read_index(path):
    """path -> (archive path, data offset, size, compressed) for every file in one BSA."""
    with open(path, "rb") as f:
        head = f.read(36)
        if head[:4] != b"BSA\x00":
            return {}
        (version, offset, flags, folder_count, file_count,
         folder_name_len, file_name_len, _file_flags) = struct.unpack_from("<8I", head, 4)
        compressed_archive = bool(flags & 0x4)
        embedded_names = bool(flags & 0x100)

        f.seek(offset)
        rec = 24 if version >= 105 else 16
        folders = []
        for _ in range(folder_count):
            data = f.read(rec)
            if version >= 105:
                _hash, count, _pad, _off = struct.unpack("<QIIQ", data)
            else:
                _hash, count, _off = struct.unpack("<QII", data)
            folders.append(count)

        entries = []
        for count in folders:
            name = ""
            if flags & 0x1:
                ln = f.read(1)[0]
                name = f.read(ln)[:-1].decode("cp1252").replace("\\", "/").lower()
            for _ in range(count):
                _h, size, off = struct.unpack("<QII", f.read(16))
                entries.append([name, size, off])

        names = []
        if flags & 0x2:
            blob = f.read(file_name_len)
            names = [n.decode("cp1252").lower() for n in blob.split(b"\x00") if n]

    index = {}
    for i, (folder, size, off) in enumerate(entries):
        leaf = names[i] if i < len(names) else f"<unnamed {i}>"
        full = f"{folder}/{leaf}" if folder else leaf
        flagged = bool(size & 0x40000000)
        index[full] = (path, off, size & 0x3FFFFFFF,
                       compressed_archive != flagged, embedded_names)
    return index


def build_index(data_dir):
    index = {}
    for name in sorted(os.listdir(data_dir)):
        if name.lower().endswith(".bsa"):
            try:
                index.update(read_index(os.path.join(data_dir, name)))
            except Exception as exc:                      # noqa: BLE001
                print(f"  ! {name}: {exc}", file=sys.stderr)
    return index


def extract(entry):
    archive, off, size, compressed, embedded = entry
    with open(archive, "rb") as f:
        f.seek(off)
        blob = f.read(size)
    p = 0
    if embedded:
        ln = blob[0]
        p = 1 + ln
    if compressed:
        original = struct.unpack_from("<I", blob, p)[0]
        raw = lz4_frame(blob[p + 4:])
        return raw[:original]
    return blob[p:]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", required=True, help="Skyrim Data folder holding the BSAs")
    ap.add_argument("--find", help="substring to search the archive index for")
    ap.add_argument("--extract", help="exact archive path to extract")
    ap.add_argument("--out", default="build/vanilla", help="where extracted files land")
    ap.add_argument("--limit", type=int, default=40)
    args = ap.parse_args()

    index = build_index(args.data)
    print(f"indexed {len(index)} files across the archives in {args.data}")

    if args.find:
        hits = sorted(k for k in index if args.find.lower() in k)
        print(f"\n{len(hits)} match '{args.find}':")
        for k in hits[:args.limit]:
            archive, off, size, comp, _ = index[k]
            print(f"  {size:>9} {'lz4' if comp else 'raw'}  {k}")
        if len(hits) > args.limit:
            print(f"  ... and {len(hits) - args.limit} more")

    if args.extract:
        key = args.extract.replace("\\", "/").lower()
        if key not in index:
            print(f"\nnot in any archive: {key}")
            return 1
        data = extract(index[key])
        dest = os.path.join(args.out, os.path.basename(key))
        os.makedirs(args.out, exist_ok=True)
        with open(dest, "wb") as f:
            f.write(data)
        print(f"\nextracted {len(data)} bytes -> {dest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
