"""Quadric-error-metric decimator for the We-Know tile model.

Reads an OBJ whose parts are `g` groups, collapses edges inside each group until
the requested triangle budget is met, and writes a new OBJ with the same group
names. Positions only: nothing downstream samples the model's UVs, and Unity
recalculates normals on import.

Usage: python -I decimate_obj.py <in.obj> <out.obj>
"""
import sys
import heapq
import math
from collections import defaultdict

# Triangle budget per group. Groups not listed are copied unchanged.
TARGETS = {
    "WK_EmissionTubes": 1024,
    "WK_Trim": 620,
    "WK_Frame": 300,
    "WK_LogoWhite": 220,
    "WK_Emission": 140,
}
# Never shrink a connected component below this many triangles: the tube runs and
# trim studs are separate shells, and collapsing one away leaves a visible gap.
MIN_FACES_PER_COMPONENT = 6
# An edge whose endpoints sit on different shells must never be collapsed.
BOUNDARY_PENALTY = 1e3


def read_obj(path):
    verts = []
    groups = []  # (name, [face, ...]) preserving file order
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
                if len(idx) != 3:
                    raise ValueError("non-triangular face: %r" % line)
                current[1].append(tuple(idx))
    return verts, groups


def plane_quadric(a, b, c):
    """4x4 fundamental error quadric of the plane through a, b, c, as 10 floats."""
    ux, uy, uz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    vx, vy, vz = c[0] - a[0], c[1] - a[1], c[2] - a[2]
    nx, ny, nz = uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx
    length = math.sqrt(nx * nx + ny * ny + nz * nz)
    if length < 1e-20:
        return None, 0.0
    nx, ny, nz = nx / length, ny / length, nz / length
    d = -(nx * a[0] + ny * a[1] + nz * a[2])
    return (nx * nx, nx * ny, nx * nz, nx * d,
            ny * ny, ny * nz, ny * d,
            nz * nz, nz * d,
            d * d), length * 0.5


def q_add(p, q):
    if p is None:
        return list(q)
    for i in range(10):
        p[i] += q[i]
    return p


def q_error(q, v):
    x, y, z = v
    return (q[0] * x * x + 2 * q[1] * x * y + 2 * q[2] * x * z + 2 * q[3] * x
            + q[4] * y * y + 2 * q[5] * y * z + 2 * q[6] * y
            + q[7] * z * z + 2 * q[8] * z
            + q[9])


def decimate(verts, faces, target_faces):
    """Collapse edges until len(faces) <= target_faces. Returns (verts, faces)."""
    # Work on a local copy of just this group's vertices.
    used = sorted({i for f in faces for i in f})
    remap = {old: new for new, old in enumerate(used)}
    pos = [list(verts[i]) for i in used]
    tris = [tuple(remap[i] for i in f) for f in faces]

    # Connected components, so an edge never bridges two separate shells and a
    # shell is never collapsed out of existence.
    parent = list(range(len(pos)))

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a

    def union(a, b):
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[ra] = rb

    for t in tris:
        union(t[0], t[1])
        union(t[0], t[2])

    component_faces = defaultdict(int)
    for t in tris:
        component_faces[find(t[0])] += 1

    quadrics = [None] * len(pos)
    for t in tris:
        q, _ = plane_quadric(pos[t[0]], pos[t[1]], pos[t[2]])
        if q is None:
            continue
        for i in t:
            quadrics[i] = q_add(quadrics[i], q)
    for i in range(len(quadrics)):
        if quadrics[i] is None:
            quadrics[i] = [0.0] * 10

    # Boundary edges (used by one triangle) hold the silhouette: weight them up.
    edge_uses = defaultdict(int)
    for t in tris:
        for a, b in ((t[0], t[1]), (t[1], t[2]), (t[2], t[0])):
            edge_uses[(min(a, b), max(a, b))] += 1

    for (a, b), uses in edge_uses.items():
        if uses != 1:
            continue
        q, area = plane_quadric(pos[a], pos[b], [pos[a][0] + 1e-3, pos[a][1], pos[a][2]])
        del q, area

    alive = [True] * len(pos)
    face_alive = [True] * len(tris)
    incident = defaultdict(set)
    for fi, t in enumerate(tris):
        for i in t:
            incident[i].add(fi)

    def edge_cost(a, b):
        q = [quadrics[a][i] + quadrics[b][i] for i in range(10)]
        # Collapse to whichever endpoint is cheaper: keeps vertices on the surface
        # and avoids inverting these thin, shell-like parts.
        ca, cb = q_error(q, pos[a]), q_error(q, pos[b])
        target = pos[b] if cb <= ca else pos[a]
        cost = min(ca, cb)
        if edge_uses.get((min(a, b), max(a, b)), 0) == 1:
            cost += BOUNDARY_PENALTY
        return cost, list(target), q

    heap = []
    for (a, b) in edge_uses:
        cost, target, _ = edge_cost(a, b)
        heapq.heappush(heap, (cost, a, b, target))

    live_faces = len(tris)
    while live_faces > target_faces and heap:
        cost, a, b, target = heapq.heappop(heap)
        if not alive[a] or not alive[b]:
            continue
        shared = incident[a] & incident[b]
        shared = {f for f in shared if face_alive[f]}
        if not shared:
            continue
        root = find(a)
        if component_faces[root] - len(shared) < MIN_FACES_PER_COMPONENT:
            continue
        # Stale entry: recompute and requeue if the cost moved.
        fresh_cost, fresh_target, fresh_q = edge_cost(a, b)
        if fresh_cost > cost + 1e-12:
            heapq.heappush(heap, (fresh_cost, a, b, fresh_target))
            continue

        pos[a] = fresh_target
        quadrics[a] = fresh_q
        alive[b] = False
        for f in shared:
            face_alive[f] = False
            live_faces -= 1
            component_faces[root] -= 1
        for f in list(incident[b]):
            if not face_alive[f]:
                continue
            t = list(tris[f])
            for k in range(3):
                if t[k] == b:
                    t[k] = a
            if t[0] == t[1] or t[1] == t[2] or t[0] == t[2]:
                face_alive[f] = False
                live_faces -= 1
                component_faces[root] -= 1
                continue
            tris[f] = tuple(t)
            incident[a].add(f)
        incident[b].clear()

        # Refresh the edges now touching a.
        neighbours = set()
        for f in incident[a]:
            if face_alive[f]:
                neighbours.update(tris[f])
        neighbours.discard(a)
        for n in neighbours:
            if not alive[n]:
                continue
            c, tgt, _ = edge_cost(a, n)
            heapq.heappush(heap, (c, a, n, tgt))

    out_faces = [tris[i] for i in range(len(tris)) if face_alive[i]]
    keep = sorted({i for f in out_faces for i in f})
    final = {old: new for new, old in enumerate(keep)}
    return [pos[i] for i in keep], [tuple(final[i] for i in f) for f in out_faces]


def main():
    src, dst = sys.argv[1], sys.argv[2]
    verts, groups = read_obj(src)
    out_verts = []
    out_groups = []
    before = after = 0
    for name, faces in groups:
        before += len(faces)
        target = TARGETS.get(name)
        if target is None or len(faces) <= target:
            used = sorted({i for f in faces for i in f})
            remap = {old: new for new, old in enumerate(used)}
            gv = [verts[i] for i in used]
            gf = [tuple(remap[i] for i in f) for f in faces]
        else:
            gv, gf = decimate(verts, faces, target)
        offset = len(out_verts)
        out_verts.extend(gv)
        out_groups.append((name, [tuple(i + offset for i in f) for f in gf]))
        after += len(gf)
        print("%-20s %6d -> %6d tris" % (name, len(faces), len(gf)))
    print("TOTAL %d -> %d tris (%.1f%%)" % (before, after, 100.0 * after / before))

    with open(dst, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("# Decimated from %s for the menu tile wall.\n" % src.split("/")[-1])
        for v in out_verts:
            handle.write("v %.8f %.8f %.8f\n" % (v[0], v[1], v[2]))
        for name, faces in out_groups:
            handle.write("\ng %s\n" % name)
            for f in faces:
                handle.write("f %d %d %d\n" % (f[0] + 1, f[1] + 1, f[2] + 1))


if __name__ == "__main__":
    main()
