"""Builds the seam-clipped tile parts used when a matched pair joins into one panel.

For each frame part, emits two variants: `_KeepLeft` keeps everything left of
+LIMIT (the tile that sits on the left of a joined pair, with its own right-hand
bezel removed) and `_KeepRight` keeps everything right of -LIMIT.

LIMIT is the x where the frame's top and bottom runs are still straight, so two
clipped tiles placed 2*LIMIT apart have collinear, touching bezel runs and read
as a single wide tile. Triangles straddling the plane are cut exactly
(Sutherland-Hodgman) rather than dropped, so the cut is a clean straight edge.

Usage: python -I seam_clip_obj.py <in.obj> <out.obj>
"""
import sys

# Where the rounded corner arcs begin; measured from the model, not assumed.
LIMIT = 0.41
# Parts that make up the bezel. The face plates and the badge sit inside LIMIT
# already, so clipping them would be a no-op.
CLIPPED = ("WK_Frame", "WK_Trim", "WK_Emission", "WK_EmissionTubes")
EPS = 1e-9


def read_obj(path):
    verts = []
    groups = []
    current = None
    with open(path, "r", encoding="utf-8") as handle:
        for line in handle:
            if line.startswith("v "):
                p = line.split()
                verts.append((float(p[1]), float(p[2]), float(p[3])))
            elif line.startswith("g "):
                current = (line.split(None, 1)[1].strip(), [])
                groups.append(current)
            elif line.startswith("f ") and current is not None:
                idx = [int(t.split("/")[0]) - 1 for t in line.split()[1:]]
                current[1].append(tuple(idx))
    return verts, groups


def clip_triangle(tri, keep_sign, limit):
    """Clip one triangle to the half-space keep_sign * (x - limit) <= 0.

    Returns a list of triangles (0, 1 or 2) as tuples of xyz points."""
    def inside(p):
        return keep_sign * (p[0] - limit) <= EPS

    poly = list(tri)
    out = []
    count = len(poly)
    for i in range(count):
        a = poly[i]
        b = poly[(i + 1) % count]
        a_in, b_in = inside(a), inside(b)
        if a_in:
            out.append(a)
        if a_in != b_in:
            denom = (b[0] - a[0])
            if abs(denom) < EPS:
                continue
            t = (limit - a[0]) / denom
            out.append((
                a[0] + (b[0] - a[0]) * t,
                a[1] + (b[1] - a[1]) * t,
                a[2] + (b[2] - a[2]) * t,
            ))
    if len(out) < 3:
        return []
    # Fan-triangulate the clipped polygon (convex, since a triangle cut by a
    # plane yields a triangle or a quad).
    return [(out[0], out[i], out[i + 1]) for i in range(1, len(out) - 1)]


def build(verts, groups, keep_sign, limit, shift_x=0.0):
    """Returns [(group_name, [triangle, ...]), ...] clipped to one side.

    shift_x translates the result, used to put a cap piece's cut plane on its own
    origin so the GameObject can scale it away towards the seam."""
    result = []
    for name, faces in groups:
        if name not in CLIPPED:
            # Plates and badge already sit inside LIMIT: nothing to swap at the seam.
            continue
        tris = []
        for f in faces:
            tri = tuple(verts[i] for i in f)
            for out in clip_triangle(tri, keep_sign, limit):
                if shift_x:
                    out = tuple((p[0] + shift_x, p[1], p[2]) for p in out)
                tris.append(out)
        result.append((name, tris))
    return result


def write_obj(path, variants, header):
    """variants: [(suffix, [(group, [triangle, ...]), ...]), ...]"""
    verts = []
    index = {}
    lines = []

    def vid(p):
        key = (round(p[0], 7), round(p[1], 7), round(p[2], 7))
        got = index.get(key)
        if got is None:
            verts.append(key)
            got = len(verts)
            index[key] = got
        return got

    for suffix, groups in variants:
        for name, tris in groups:
            if not tris:
                continue
            face_lines = []
            for tri in tris:
                a, b, c = vid(tri[0]), vid(tri[1]), vid(tri[2])
                if a == b or b == c or a == c:
                    continue
                face_lines.append("f %d %d %d" % (a, b, c))
            if not face_lines:
                continue
            lines.append("\ng %s%s" % (name, suffix))
            lines.extend(face_lines)

    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("# %s\n" % header)
        for v in verts:
            handle.write("v %.8f %.8f %.8f\n" % v)
        handle.write("\n".join(lines))
        handle.write("\n")


def main():
    src, dst = sys.argv[1], sys.argv[2]
    verts, groups = read_obj(src)

    # Unity's OBJ importer negates X (right-handed source to left-handed engine),
    # so a half clipped on the +X side of this file lands on the -X side in engine.
    # These names describe the result in Unity, which is what the prefab wires up.
    keep_left = build(verts, groups, -1, -LIMIT)   # in Unity: keeps -X, drops the right bezel
    keep_right = build(verts, groups, +1, LIMIT)   # in Unity: keeps +X, drops the left bezel

    # The bezel each tile gives up, as its own piece. Its cut plane is moved to the
    # origin so TileController can scale it to nothing along X as the pair closes,
    # and back as they separate. Unity's X flip applies here too, so the cap that
    # retracts on a left-hand tile is generated from the source's -X side.
    # Each cap is the exact complement of the keep piece on the other side.
    cap_left = build(verts, groups, -1, LIMIT, shift_x=-LIMIT)    # Unity -X cap
    cap_right = build(verts, groups, +1, -LIMIT, shift_x=LIMIT)   # Unity +X cap

    for label, variant in (("KeepLeft", keep_left), ("KeepRight", keep_right)):
        total = sum(len(t) for _, t in variant)
        detail = ", ".join("%s=%d" % (n, len(t)) for n, t in variant if n in CLIPPED)
        print("%-10s total=%d tris  (%s)" % (label, total, detail))

    base = dst[:-4] if dst.endswith(".obj") else dst

    for label, variant in (("CapLeft", cap_left), ("CapRight", cap_right)):
        total = sum(len(t) for _, t in variant)
        xs = [p[0] for _, tris in variant for tri in tris for p in tri]
        print("%-10s total=%4d tris  x range in Unity %.4f .. %.4f"
              % (label, total, -max(xs), -min(xs)))
    write_obj(base + "CapL.obj", [("", cap_left)],
              "Left-hand bezel cap, pivot on the cut plane, from %s" % src.split("/")[-1])
    write_obj(base + "CapR.obj", [("", cap_right)],
              "Right-hand bezel cap, pivot on the cut plane, from %s" % src.split("/")[-1])
    write_obj(base + "L.obj", [("", keep_left)],
              "Left half of a joined pair (inner bezel removed), from %s" % src.split("/")[-1])
    write_obj(base + "R.obj", [("", keep_right)],
              "Right half of a joined pair (inner bezel removed), from %s" % src.split("/")[-1])

    # Report the resulting silhouette so the join distance can be checked.
    for label, variant in (("KeepLeft", keep_left), ("KeepRight", keep_right)):
        xs = [p[0] for _, tris in variant for tri in tris for p in tri]
        # Reported in Unity space (X negated), so it can be checked against the engine.
        print("%-10s x range in Unity %.4f .. %.4f" % (label, -max(xs), -min(xs)))


if __name__ == "__main__":
    main()
