"""
map_generator.py  —  Zone-based procedural map generator for DoomedCLI

Pipeline:
  1. Poisson-disk seed placement + Voronoi zone assignment
  2. 2-pass majority-neighbour smoothing
  3. Per-zone fill (Urban + Industrial first, then nature zones)
  4. Zone boundary corridor punching
  5. BFS connectivity fix (carve corridors to isolated pockets)
  6. Border enforcement
  7. UTF-8 text output
"""

import os
import math
import random
import collections
from enum import Enum, auto
from typing import Optional

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
MAP_ASSETS_FOLDER = "Assets/Maps/MapAssets/"
MAP_OUTPUT_FOLDER = "Assets/Maps/"

# ---------------------------------------------------------------------------
# Map size limits
# ---------------------------------------------------------------------------
MIN_WIDTH  = 330
MIN_HEIGHT = 300

# ---------------------------------------------------------------------------
# Map constants
# ---------------------------------------------------------------------------
H_WALL_CHAR  = "_"
V_WALL_CHAR  = "|"
FLOOR_CHAR   = " "
BORDER_CHAR  = "#"
DOOR_WIDTH   = 14   # room doorway width — player sprite is 9 wide, +5 clearance
CARVE_WIDTH  = 14   # zone corridor / connectivity passage width — must fit player sprite (9 wide) + margin
BUSH_SCATTER = 6

# Characters that are non-walkable — mirrors GameMap.WallTiles
_WALL_CHARS: frozenset = frozenset(
    "#|-_"
    "─│┌┐└┘"
    "├┤┬┴┼"
    "═║╔╗╚╝"
    "╠╣╦╩╬"
    "█▓▒"
    "♣■"
)


def is_walkable_char(ch: str) -> bool:
    return ch not in _WALL_CHARS


# ===========================================================================
# Zone types
# ===========================================================================

class ZoneType(Enum):
    FOREST     = auto()
    URBAN      = auto()
    INDUSTRIAL = auto()
    RUINS      = auto()
    WASTELAND  = auto()

_ALL_ZONE_TYPES = list(ZoneType)


# ===========================================================================
# Map types and zone weight tables
# ===========================================================================

class MapType(Enum):
    CITY     = "city"
    FOREST   = "forest"
    ABANDONED = "abandoned"

# Integer weights (repetition count) for each ZoneType per MapType.
# Higher weight = more Voronoi seeds of that zone type.
_ZONE_WEIGHTS: dict = {
    MapType.CITY: {
        ZoneType.URBAN:      8,
        ZoneType.INDUSTRIAL: 2,
        ZoneType.FOREST:     0,
        ZoneType.RUINS:      0,
        ZoneType.WASTELAND:  0,
    },
    MapType.FOREST: {
        ZoneType.URBAN:      0,
        ZoneType.INDUSTRIAL: 0,
        ZoneType.FOREST:     8,
        ZoneType.RUINS:      0,
        ZoneType.WASTELAND:  2,
    },
    MapType.ABANDONED: {
        ZoneType.URBAN:      1,
        ZoneType.INDUSTRIAL: 0,
        ZoneType.FOREST:     3,
        ZoneType.RUINS:      5,
        ZoneType.WASTELAND:  1,
    },
}


# ===========================================================================
# Asset Library
# ===========================================================================

class AssetLibrary:
    """Loads and caches every map asset from disk, grouped by prefix/category."""

    def __init__(self, base_folder: str):
        v = os.path.join(base_folder, "Vertical")
        h = os.path.join(base_folder, "Horizontal")

        self.trees      = self._load_prefix(v, "tree")
        self.bushes     = self._load_prefix(v, "bush")
        self.stones     = self._load_prefix(v, "stone")
        self.v_barriers = self._load_exclude(v, ("tree", "bush", "stone", "wall"))
        self.h_barriers = self._load_exclude(h, ("wall",))
        self.v_walls    = self._load_prefix(v, "wall")
        self.h_walls    = self._load_prefix(h, "wall")

        self.crates   = (self._load_prefix(v, "crate")   + self._load_prefix(h, "crate"))
        self.barrels  = (self._load_prefix(v, "barrel")  + self._load_prefix(h, "barrel"))
        self.rubble   =  self._load_prefix(v, "rubble")
        self.sandbags = (self._load_prefix(v, "sandbag") + self._load_prefix(h, "sandbag"))
        self.fences   = (self._load_prefix(v, "fence")   + self._load_prefix(h, "fence"))

    def _load_prefix(self, folder: str, prefix: str):
        assets = []
        if not os.path.isdir(folder):
            return assets
        for fname in sorted(os.listdir(folder)):
            if fname.endswith(".txt") and fname.startswith(prefix):
                a = self._load_file(os.path.join(folder, fname))
                if a is not None:
                    assets.append(a)
        return assets

    def _load_exclude(self, folder: str, exclude_prefixes: tuple):
        assets = []
        if not os.path.isdir(folder):
            return assets
        for fname in sorted(os.listdir(folder)):
            if not fname.endswith(".txt"):
                continue
            if any(fname.startswith(p) for p in exclude_prefixes):
                continue
            a = self._load_file(os.path.join(folder, fname))
            if a is not None:
                assets.append(a)
        return assets

    @staticmethod
    def _load_file(path: str):
        try:
            with open(path, encoding="utf-8") as f:
                lines = f.read().splitlines()
            if any(line.strip() for line in lines):
                return lines
        except OSError:
            pass
        return None


# ===========================================================================
# Grid helpers
# ===========================================================================

def asset_size(asset) -> tuple:
    return len(asset), max((len(line) for line in asset), default=0)


def safe_randint(lo: int, hi: int, rng: random.Random) -> int:
    return lo if lo >= hi else rng.randint(lo, hi)


def stamp(grid, asset, row: int, col: int) -> None:
    gh, gw = len(grid), len(grid[0])
    for r, line in enumerate(asset):
        gr = row + r
        if gr <= 0 or gr >= gh - 1:
            continue
        for c, ch in enumerate(line):
            gc = col + c
            if gc <= 0 or gc >= gw - 1:
                continue
            if ch != " ":
                grid[gr][gc] = ch


def stamp_if_clear(grid, asset, row: int, col: int, padding: int = 1) -> bool:
    ah, aw = asset_size(asset)
    r0 = max(1, row - padding)
    c0 = max(1, col - padding)
    r1 = min(len(grid) - 1,    row + ah + padding)
    c1 = min(len(grid[0]) - 1, col + aw + padding)
    for r in range(r0, r1):
        for c in range(c0, c1):
            if grid[r][c] != FLOOR_CHAR:
                return False
    stamp(grid, asset, row, col)
    return True


# ---------------------------------------------------------------------------
# Collision-aware stamp helpers
# ---------------------------------------------------------------------------

def make_solid(asset) -> list:
    """Replace EVERY non-space char with '#' so the stamped area is fully
    impassable. Use for stones, boulders, and large tree bases."""
    return [''.join('#' if ch != ' ' else ' ' for ch in line) for line in asset]


_BUSH_REMAP = {'#': '%', '_': '.', '|': ':', '-': '~', '█': ':', '▓': '.', '▒': '.', '♣': ':'}


def make_walkable_bush(asset) -> list:
    """Replace wall chars in a bush asset with walkable look-alike chars so
    the player can move through / hide in the bush.
      # -> %   _ -> .   | -> :   - -> ~   solid blocks -> softer chars"""
    return [''.join(_BUSH_REMAP.get(ch, ch) for ch in line) for line in asset]


def stamp_solid_if_clear(grid, asset, row: int, col: int, padding: int = 1) -> bool:
    """Same as stamp_if_clear but converts the asset to fully solid first."""
    return stamp_if_clear(grid, make_solid(asset), row, col, padding)


def stamp_bush_if_clear(grid, asset, row: int, col: int, padding: int = 1) -> bool:
    """Same as stamp_if_clear but uses walkable chars so the player can hide."""
    return stamp_if_clear(grid, make_walkable_bush(asset), row, col, padding)


# ===========================================================================
# Zone Layout — Voronoi
# ===========================================================================

class _Seed:
    __slots__ = ("row", "col", "zone_type")
    def __init__(self, row, col, zone_type):
        self.row, self.col, self.zone_type = row, col, zone_type


def _place_seeds(num_zones, width, height, rng, zone_pool=None):
    if zone_pool is None:
        zone_pool = _ALL_ZONE_TYPES
    min_dist = min(width, height) / math.sqrt(max(num_zones, 1)) * 0.6
    seeds = []
    types = zone_pool[:]
    rng.shuffle(types)
    type_queue = types + [rng.choice(zone_pool) for _ in range(max(0, num_zones - len(types)))]
    margin = 10
    for i in range(num_zones):
        zone_type = type_queue[i]
        placed = False
        for _ in range(400):
            r = rng.randint(margin, height - 1 - margin)
            c = rng.randint(margin, width  - 1 - margin)
            if all((r - s.row)**2 + (c - s.col)**2 >= min_dist**2 for s in seeds):
                seeds.append(_Seed(r, c, zone_type))
                placed = True
                break
        if not placed:
            for _ in range(200):
                r = rng.randint(margin, height - 1 - margin)
                c = rng.randint(margin, width  - 1 - margin)
                if all((r - s.row)**2 + (c - s.col)**2 >= (min_dist * 0.4)**2 for s in seeds):
                    seeds.append(_Seed(r, c, zone_type))
                    break
    return seeds


def _build_zone_grid(seeds, width, height):
    zone_grid = [[None] * width for _ in range(height)]
    for r in range(height):
        for c in range(width):
            best_d2, best_z = float("inf"), seeds[0].zone_type
            for s in seeds:
                d2 = (r - s.row)**2 + (c - s.col)**2
                if d2 < best_d2:
                    best_d2, best_z = d2, s.zone_type
            zone_grid[r][c] = best_z
    return zone_grid


def _smooth_zones(zone_grid, passes=2):
    height, width = len(zone_grid), len(zone_grid[0])
    for _ in range(passes):
        new_grid = [row[:] for row in zone_grid]
        for r in range(1, height - 1):
            for c in range(1, width - 1):
                counts = collections.Counter()
                for dr in (-1, 0, 1):
                    for dc in (-1, 0, 1):
                        counts[zone_grid[r + dr][c + dc]] += 1
                new_grid[r][c] = counts.most_common(1)[0][0]
        zone_grid = new_grid
    return zone_grid


def _get_zone_cells(zone_grid, target):
    return [(r, c) for r, row in enumerate(zone_grid) for c, z in enumerate(row) if z == target]


# ===========================================================================
# Room helpers
# ===========================================================================

def _draw_room(grid, r1, c1, r2, c2, rng):
    """Draw a room with 2-cell-thick walls so they are visible from both sides.
    Wall layout:  outer row r1, inner row r1+1  (top)
                  inner row r2-1, outer row r2   (bottom)
                  outer col c1, inner col c1+1   (left)
                  inner col c2-1, outer col c2   (right)
    Interior spans r1+2 .. r2-2, c1+2 .. c2-2.
    Each wall face gets one door gap DOOR_WIDTH wide."""
    gh, gw = len(grid), len(grid[0])
    span_w = c2 - c1
    span_h = r2 - r1
    # Door positions — place gap on the outer wall row/col
    door_top   = safe_randint(c1 + 3, max(c1 + 3, c1 + span_w - DOOR_WIDTH - 3), rng)
    door_bot   = safe_randint(c1 + 3, max(c1 + 3, c1 + span_w - DOOR_WIDTH - 3), rng)
    door_left  = safe_randint(r1 + 3, max(r1 + 3, r1 + span_h - DOOR_WIDTH - 3), rng)
    door_right = safe_randint(r1 + 3, max(r1 + 3, r1 + span_h - DOOR_WIDTH - 3), rng)

    def in_door_h(c, door_c): return door_c <= c < door_c + DOOR_WIDTH
    def in_door_v(r, door_r): return door_r <= r < door_r + DOOR_WIDTH

    # Horizontal walls (top and bottom) — 2 rows each
    for c in range(c1, c2 + 1):
        if c < 1 or c >= gw - 1: continue
        is_door_top = in_door_h(c, door_top)
        is_door_bot = in_door_h(c, door_bot)
        for row, is_door in ((r1, is_door_top), (r1 + 1, is_door_top),
                             (r2 - 1, is_door_bot), (r2, is_door_bot)):
            if 1 <= row < gh - 1 and not is_door:
                grid[row][c] = H_WALL_CHAR

    # Vertical walls (left and right) — 2 cols each
    for r in range(r1, r2 + 1):
        if r < 1 or r >= gh - 1: continue
        is_door_left  = in_door_v(r, door_left)
        is_door_right = in_door_v(r, door_right)
        for col, is_door in ((c1, is_door_left), (c1 + 1, is_door_left),
                             (c2 - 1, is_door_right), (c2, is_door_right)):
            if 1 <= col < gw - 1 and not is_door:
                # don't overwrite h-wall corners
                if grid[r][col] == FLOOR_CHAR:
                    grid[r][col] = V_WALL_CHAR


def _room_area_clear(grid, r1, c1, r2, c2, margin: int = 4) -> bool:
    """Return True if the padded rectangle is entirely floor on the grid.
    This checks directly against drawn chars so rooms from any filler or
    zone can never overlap each other."""
    h, w = len(grid), len(grid[0])
    for r in range(max(1, r1 - margin), min(h - 1, r2 + margin + 1)):
        for c in range(max(1, c1 - margin), min(w - 1, c2 + margin + 1)):
            if grid[r][c] != FLOOR_CHAR:
                return False
    return True


def _zone_bounds(cells):
    rows = [r for r, _ in cells]
    cols = [c for _, c in cells]
    return min(rows), min(cols), max(rows), max(cols)


# ===========================================================================
# Zone Fillers
# ===========================================================================

class _ForestFiller:
    """Dense woodland: trees block movement, bushes are walkable hiding spots,
    boulders are scarce (one per large area) and fully impassable."""
    def fill(self, grid, cells, assets, rng):
        if not cells: return
        h, w = len(grid), len(grid[0])
        trees  = assets.trees  or []
        bushes = assets.bushes or []
        stones = assets.stones or []
        # --- trees: stamp with original art; trunk chars ('|', '-') ARE wall chars
        #     so the tree body already blocks. We keep original art for appearance.
        num_trees = max(1, len(cells) // 350)
        placed = 0
        for _ in range(num_trees * 12):
            if placed >= num_trees or not trees: break
            row, col = rng.choice(cells)
            tree = rng.choice(trees)
            t_h, t_w = asset_size(tree)
            if not stamp_if_clear(grid, tree, row, col, padding=8): continue
            placed += 1
            # scatter walkable bushes around the tree so player can hide
            for _ in range(rng.randint(2, 5)):
                if not bushes: break
                bush  = rng.choice(bushes)
                b_h, b_w = asset_size(bush)
                b_row = safe_randint(max(1, row - BUSH_SCATTER), min(h - 2 - b_h, row + t_h + BUSH_SCATTER), rng)
                b_col = safe_randint(max(1, col - BUSH_SCATTER), min(w - 2 - b_w, col + t_w + BUSH_SCATTER), rng)
                stamp_bush_if_clear(grid, bush, b_row, b_col, padding=1)
        # --- standalone bushes in glades (walkable)
        if bushes:
            nb, bdone = max(1, len(cells) // 500), 0
            for _ in range(nb * 6):
                if bdone >= nb: break
                row, col = rng.choice(cells)
                if stamp_bush_if_clear(grid, rng.choice(bushes), row, col, padding=1): bdone += 1
        # --- boulders: very sparse, fully impassable ('#' filled)
        if stones:
            n, done = max(1, len(cells) // 3000), 0
            for _ in range(n * 6):
                if done >= n: break
                row, col = rng.choice(cells)
                if stamp_solid_if_clear(grid, rng.choice(stones), row, col, padding=8): done += 1


class _UrbanFiller:
    """City block: empty rooms. No trees. No loose stones."""
    def fill(self, grid, cells, assets, rng):
        if not cells: return
        zr1, zc1, zr2, zc2 = _zone_bounds(cells)
        zr1 += 2; zc1 += 2; zr2 -= 2; zc2 -= 2
        if zr2 - zr1 < 30 or zc2 - zc1 < 30: return
        # Scale room count by number of cells so a large city zone fills densely
        num_rooms = rng.randint(len(cells) // 5000, max(len(cells) // 5000 + 1, len(cells) // 2800))
        placed_rooms = 0
        for _ in range(num_rooms * 14):
            if placed_rooms >= num_rooms: break
            rh = rng.randint(30, min(55, max(31, (zr2 - zr1) // 4)))
            rw = rng.randint(30, min(70, max(31, (zc2 - zc1) // 4)))
            r1 = safe_randint(zr1, zr2 - rh, rng)
            c1 = safe_randint(zc1, zc2 - rw, rng)
            r2, c2 = r1 + rh, c1 + rw
            if not _room_area_clear(grid, r1, c1, r2, c2, margin=28): continue
            _draw_room(grid, r1, c1, r2, c2, rng)
            placed_rooms += 1

        # --- street scatter: barriers for cover ---
        # Build a list of open street cells (floor chars inside the zone)
        street_cells = [
            (r, c) for (r, c) in cells
            if grid[r][c] == FLOOR_CHAR
        ]
        if street_cells:
            # Street barriers (cover objects like '||') — one per ~1200 cells
            barrier_assets = (assets.v_barriers or []) + (assets.h_barriers or [])
            if barrier_assets:
                nb = max(2, len(street_cells) // 1200)
                placed_b = 0
                for _ in range(nb * 10):
                    if placed_b >= nb: break
                    row, col = rng.choice(street_cells)
                    asset = rng.choice(barrier_assets)
                    if stamp_if_clear(grid, asset, row, col, padding=4):
                        placed_b += 1


class _IndustrialFiller:
    """Factory-like interior: empty rooms."""
    def fill(self, grid, cells, assets, rng):
        if not cells: return
        zr1, zc1, zr2, zc2 = _zone_bounds(cells)
        zr1 += 2; zc1 += 2; zr2 -= 2; zc2 -= 2
        if zr2 - zr1 < 30 or zc2 - zc1 < 30: return
        # Scale rooms like urban — draw several factory areas instead of one huge room
        num_rooms = rng.randint(max(1, len(cells) // 6000), max(2, len(cells) // 3000))
        placed_rooms = 0
        for _ in range(num_rooms * 14):
            if placed_rooms >= num_rooms: break
            rh = rng.randint(30, min(55, max(31, (zr2 - zr1) // 3)))
            rw = rng.randint(30, min(70, max(31, (zc2 - zc1) // 3)))
            r1 = safe_randint(zr1, zr2 - rh, rng)
            c1 = safe_randint(zc1, zc2 - rw, rng)
            r2, c2 = r1 + rh, c1 + rw
            if not _room_area_clear(grid, r1, c1, r2, c2, margin=28): continue
            _draw_room(grid, r1, c1, r2, c2, rng)
            placed_rooms += 1


class _RuinsFiller:
    """Abandoned ruins: partial broken walls + nature reclaiming the space.
    A few solid boulders as rubble; bushes grow through the cracks (walkable)."""
    def fill(self, grid, cells, assets, rng):
        if not cells: return
        h, w = len(grid), len(grid[0])
        stones = assets.stones or assets.rubble or []
        bushes = assets.bushes or []
        trees  = assets.trees  or []
        def partial_wall(wall_char, row_mode):
            n, done = max(1, len(cells) // 300), 0
            for _ in range(n * 4):
                if done >= n: break
                row, col = rng.choice(cells)
                seg_len   = rng.randint(4, 20)
                gap_start = rng.randint(2, max(2, seg_len - CARVE_WIDTH - 2))
                gap_len   = rng.randint(CARVE_WIDTH, CARVE_WIDTH + 4)
                for i in range(seg_len):
                    if row_mode:
                        c = col + i
                        if c <= 0 or c >= w - 1: continue
                        if gap_start <= i < gap_start + gap_len: continue
                        if grid[row][c] == FLOOR_CHAR: grid[row][c] = wall_char
                    else:
                        r = row + i
                        if r <= 0 or r >= h - 1: continue
                        if gap_start <= i < gap_start + gap_len: continue
                        if grid[r][col] == FLOOR_CHAR: grid[r][col] = wall_char
                done += 1
        partial_wall(H_WALL_CHAR, True)
        partial_wall(V_WALL_CHAR, False)
        # rubble boulders — sparse, fully impassable
        if stones:
            n, done = max(1, len(cells) // 1500), 0
            for _ in range(n * 5):
                if done >= n: break
                row, col = rng.choice(cells)
                if stamp_solid_if_clear(grid, rng.choice(stones), row, col, padding=8): done += 1
        # nature reclaiming — walkable bushes in the rubble
        if bushes:
            nb, bdone = max(1, len(cells) // 800), 0
            for _ in range(nb * 5):
                if bdone >= nb: break
                row, col = rng.choice(cells)
                if stamp_bush_if_clear(grid, rng.choice(bushes), row, col, padding=1): bdone += 1
        # occasional tree growing through ruins
        if trees:
            nt, tdone = max(1, len(cells) // 2000), 0
            for _ in range(nt * 6):
                if tdone >= nt: break
                row, col = rng.choice(cells)
                if stamp_if_clear(grid, rng.choice(trees), row, col, padding=8): tdone += 1


class _WastelandFiller:
    """Open scrubland: walkable bushes for cover, scarce solid boulders, no structures."""
    def fill(self, grid, cells, assets, rng):
        if not cells: return
        stones = assets.stones or []
        bushes = assets.bushes or []
        # stones: few and fully impassable
        if stones:
            n, done = max(1, len(cells) // 2500), 0
            for _ in range(n * 5):
                if done >= n: break
                row, col = rng.choice(cells)
                if stamp_solid_if_clear(grid, rng.choice(stones), row, col, padding=2): done += 1
        # bushes: moderate density, walkable (player can hide)
        if bushes:
            nb, bdone = max(1, len(cells) // 700), 0
            for _ in range(nb * 5):
                if bdone >= nb: break
                row, col = rng.choice(cells)
                if stamp_bush_if_clear(grid, rng.choice(bushes), row, col, padding=1): bdone += 1


_FILLERS = {
    ZoneType.URBAN:      _UrbanFiller(),
    ZoneType.INDUSTRIAL: _IndustrialFiller(),
    ZoneType.RUINS:      _RuinsFiller(),
    ZoneType.FOREST:     _ForestFiller(),
    ZoneType.WASTELAND:  _WastelandFiller(),
}


# ===========================================================================
# Zone Boundary Corridor Punching
# ===========================================================================

def _punch_zone_corridors(grid, zone_grid, rng):
    h, w = len(grid), len(grid[0])
    h_bounds, v_bounds = [], []
    for r in range(1, h - 2):
        for c in range(1, w - 1):
            if zone_grid[r][c] != zone_grid[r + 1][c]:
                h_bounds.append((r, c))
            if c + 1 < w - 1 and zone_grid[r][c] != zone_grid[r][c + 1]:
                v_bounds.append((r, c))
    _punch_boundary_set(grid, h_bounds, horizontal=True)
    _punch_boundary_set(grid, v_bounds, horizontal=False)


def _punch_boundary_set(grid, bounds, horizontal, spacing=30, min_openings=2):
    if not bounds: return
    h_grid, w_grid = len(grid), len(grid[0])
    if horizontal:
        bounds_sorted = sorted(bounds, key=lambda x: (x[0], x[1]))
    else:
        bounds_sorted = sorted(bounds, key=lambda x: (x[1], x[0]))
    segments, current = [], [bounds_sorted[0]]
    for pt in bounds_sorted[1:]:
        prev = current[-1]
        adj = (pt[0] == prev[0] and pt[1] == prev[1] + 1) if horizontal else (pt[1] == prev[1] and pt[0] == prev[0] + 1)
        if adj: current.append(pt)
        else: segments.append(current); current = [pt]
    segments.append(current)
    for seg in segments:
        n = len(seg)
        if n < 4: continue
        num_openings = max(min_openings, n // spacing)
        indices = [int(i * n / num_openings) for i in range(num_openings)]
        for idx in indices:
            pt   = seg[idx]
            half = CARVE_WIDTH // 2
            for k in range(-half, half + 2):
                if horizontal:
                    c = pt[1] + k
                    for dr in (0, 1):
                        rr = pt[0] + dr
                        if 1 <= rr < h_grid - 1 and 1 <= c < w_grid - 1:
                            if not is_walkable_char(grid[rr][c]): grid[rr][c] = FLOOR_CHAR
                else:
                    r = pt[0] + k
                    for dc in (0, 1):
                        cc = pt[1] + dc
                        if 1 <= r < h_grid - 1 and 1 <= cc < w_grid - 1:
                            if not is_walkable_char(grid[r][cc]): grid[r][cc] = FLOOR_CHAR


# ===========================================================================
# Connectivity Fix
# ===========================================================================

def _fix_connectivity(grid, rng: random.Random):
    """
    Single-pass connectivity fix:
      1. BFS from centre → reachable set
      2. Label unreachable walkable cells into connected components (BFS-based)
      3. For each component, carve one L-corridor to the nearest reachable cell
      4. Final BFS check and report
    Never more than 2 full BFS scans regardless of map size.
    """
    h, w = len(grid), len(grid[0])

    reachable = _bfs_walkable(grid, h // 2, w // 2)

    # --- Find all unreachable walkable cells ---
    unreachable_set = set()
    for r in range(1, h - 1):
        for c in range(1, w - 1):
            if is_walkable_char(grid[r][c]) and (r, c) not in reachable:
                unreachable_set.add((r, c))

    if not unreachable_set:
        print("  Connectivity: fully connected (no fix needed).")
        return

    # --- Label unreachable cells into connected components ---
    components = []
    remaining = sorted(unreachable_set)       # sorted list for determinism
    visited_comp: set = set()
    for seed_cell in remaining:
        if seed_cell in visited_comp:
            continue
        comp: set = set()
        bq   = collections.deque([seed_cell])
        comp.add(seed_cell)
        while bq:
            r, c = bq.popleft()
            for dr, dc in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                nr, nc = r + dr, c + dc
                if (nr, nc) in unreachable_set and (nr, nc) not in comp:
                    comp.add((nr, nc))
                    bq.append((nr, nc))
        visited_comp |= comp
        components.append(comp)

    # --- Sample reachable cells for corridor targeting ---
    reach_sample = sorted(reachable)           # sort for deterministic ordering
    if len(reach_sample) > 400:
        reach_sample = rng.sample(reach_sample, 400)

    # --- Carve one corridor per component ---
    corridors = 0
    for comp in components:
        # Pick a representative cell from the component (sorted for determinism)
        cells_list = sorted(comp)
        ur, uc = cells_list[len(cells_list) // 2]

        best_tr, best_tc, best_d = h // 2, w // 2, float("inf")
        for tr, tc in reach_sample:
            d = abs(ur - tr) + abs(uc - tc)
            if d < best_d:
                best_d, best_tr, best_tc = d, tr, tc
        _carve_line(grid, ur, uc, best_tr, best_tc)
        corridors += 1

    # --- Final check ---
    reachable2   = _bfs_walkable(grid, h // 2, w // 2)
    still_bad    = sum(
        1 for r in range(1, h - 1) for c in range(1, w - 1)
        if is_walkable_char(grid[r][c]) and (r, c) not in reachable2
    )
    floor_total  = sum(
        1 for r in range(1, h - 1) for c in range(1, w - 1)
        if is_walkable_char(grid[r][c])
    )
    pct = still_bad / max(floor_total, 1) * 100
    print(f"  Connectivity: carved {corridors} corridor(s). "
          f"Unreachable: {still_bad}/{floor_total} ({pct:.1f}%)")


def _bfs_walkable(grid, start_r, start_c):
    h, w = len(grid), len(grid[0])
    if not is_walkable_char(grid[start_r][start_c]):
        found = False
        for radius in range(1, max(h, w)):
            for dr in range(-radius, radius + 1):
                for dc in range(-radius, radius + 1):
                    r, c = start_r + dr, start_c + dc
                    if 1 <= r < h - 1 and 1 <= c < w - 1 and is_walkable_char(grid[r][c]):
                        start_r, start_c, found = r, c, True
                        break
                if found: break
            if found: break
    visited = set()
    queue = collections.deque([(start_r, start_c)])
    visited.add((start_r, start_c))
    while queue:
        r, c = queue.popleft()
        for dr, dc in ((-1,0),(1,0),(0,-1),(0,1)):
            nr, nc = r + dr, c + dc
            if 1 <= nr < h - 1 and 1 <= nc < w - 1 and (nr, nc) not in visited and is_walkable_char(grid[nr][nc]):
                visited.add((nr, nc))
                queue.append((nr, nc))
    return visited


def _carve_line(grid, r0, c0, r1, c1):
    """L-shaped corridor, CARVE_WIDTH cells wide, so the player sprite can traverse it."""
    h, w = len(grid), len(grid[0])
    half = CARVE_WIDTH // 2
    # horizontal leg at r0
    step = 1 if c1 >= c0 else -1
    for c in range(c0, c1 + step, step):
        for offset in range(-half, half + 1):
            r = r0 + offset
            if 1 <= r < h - 1 and 1 <= c < w - 1:
                if not is_walkable_char(grid[r][c]): grid[r][c] = FLOOR_CHAR
    # vertical leg at c1
    step = 1 if r1 >= r0 else -1
    for r in range(r0, r1 + step, step):
        for offset in range(-half, half + 1):
            c = c1 + offset
            if 1 <= r < h - 1 and 1 <= c < w - 1:
                if not is_walkable_char(grid[r][c]): grid[r][c] = FLOOR_CHAR


# ===========================================================================
# Border enforcement
# ===========================================================================

def _enforce_border(grid):
    h, w = len(grid), len(grid[0])
    for c in range(w): grid[0][c] = grid[h-1][c] = BORDER_CHAR
    for r in range(h): grid[r][0] = grid[r][w-1] = BORDER_CHAR


# ===========================================================================
# Zone pool builder
# ===========================================================================

def _build_zone_pool(map_type: MapType, rng: random.Random) -> list:
    """Return a zone-type list weighted by map_type, shuffled for variety."""
    weights = _ZONE_WEIGHTS[map_type]
    pool = []
    for zt, w in weights.items():
        pool.extend([zt] * w)
    if not pool:
        pool = _ALL_ZONE_TYPES[:]
    rng.shuffle(pool)
    return pool


# ===========================================================================
# Main generation function
# ===========================================================================

def generate_map(width: int, height: int, output_name: str,
                 num_zones: int = 12, zone_seed: Optional[int] = None,
                 map_type: MapType = MapType.CITY) -> None:

    rng = random.Random(zone_seed)
    print(f"Generating {width}x{height} [{map_type.value}] map with {num_zones} zones (seed={zone_seed})...")

    grid = []
    for r in range(height):
        if r == 0 or r == height - 1:
            grid.append(list(BORDER_CHAR * width))
        else:
            grid.append(list(BORDER_CHAR + FLOOR_CHAR * (width - 2) + BORDER_CHAR))

    assets = AssetLibrary(MAP_ASSETS_FOLDER)

    print("  [1/5] Zone layout...")
    zone_pool = _build_zone_pool(map_type, rng)
    seeds     = _place_seeds(num_zones, width, height, rng, zone_pool=zone_pool)
    zone_grid = _build_zone_grid(seeds, width, height)
    zone_grid = _smooth_zones(zone_grid, passes=2)

    print("  [2/5] Filling zones...")
    for zone_type, filler in _FILLERS.items():
        cells = _get_zone_cells(zone_grid, zone_type)
        if cells:
            filler.fill(grid, cells, assets, rng)

    print("  [3/5] Linking zone boundaries...")
    _punch_zone_corridors(grid, zone_grid, rng)

    print("  [4/5] Validating connectivity...")
    _fix_connectivity(grid, rng)

    print("  [5/5] Enforcing border...")
    _enforce_border(grid)

    os.makedirs(MAP_OUTPUT_FOLDER, exist_ok=True)
    out_path = os.path.join(MAP_OUTPUT_FOLDER, output_name)
    with open(out_path, "w", encoding="utf-8") as f:
        f.write("\n".join("".join(row) for row in grid))

    zone_counts = {z: sum(1 for row in zone_grid for cell in row if cell == z) for z in ZoneType}
    summary = "  ".join(f"{z.name}={zone_counts[z]}" for z in ZoneType)
    print(f"Map saved -> {out_path}  ({width}x{height})")
    print(f"  Zones: {summary}")


# ===========================================================================
# Interactive prompt
# ===========================================================================

def _prompt_int(prompt: str, default: int, minimum: int = 0) -> int:
    while True:
        raw = input(f"{prompt} [{default}]: ").strip()
        if raw == "": return default
        if raw.lstrip("-").isdigit():
            val = int(raw)
            if val < minimum: print(f"  Must be at least {minimum}.")
            else: return val
        else: print("  Enter a valid integer.")


def _prompt_str(prompt: str, default: str) -> str:
    return input(f"{prompt} [{default}]: ").strip() or default


if __name__ == "__main__":
    print(f"Zone-based map generator  (min {MIN_WIDTH}w x {MIN_HEIGHT}h)")
    w     = _prompt_int(f"Width  (>= {MIN_WIDTH})",  600, MIN_WIDTH)
    h     = _prompt_int(f"Height (>= {MIN_HEIGHT})", 500, MIN_HEIGHT)
    name  = _prompt_str("Output filename",            "generated.txt")
    zones = _prompt_int("Number of zones",            12,  5)
    seed_raw = input("RNG seed (blank = random): ").strip()
    seed: Optional[int] = int(seed_raw) if seed_raw.lstrip("-").isdigit() else None
    # Map type selection
    _MAP_TYPE_CHOICES = {"1": MapType.CITY, "2": MapType.FOREST, "3": MapType.ABANDONED}
    print("Map type:")
    print("  1) city      — dense rooms, barriers, minimal nature")
    print("  2) forest    — trees, walkable bushes, no buildings")
    print("  3) abandoned — ruined buildings reclaimed by nature")
    mtype_raw = input("Choice [1]: ").strip() or "1"
    mtype = _MAP_TYPE_CHOICES.get(mtype_raw, MapType.CITY)
    generate_map(w, h, name, num_zones=zones, zone_seed=seed, map_type=mtype)
