# Zone-Based Map Generator — Design Plan

## Overview

Complete rewrite of `map_generator.py` producing large, high-quality maps divided into
**distinct organic zones**, each with its own visual identity, asset composition, and
game-feel. The generator uses a **Voronoi zone-layout pass** followed by **per-zone
fill strategies**, connected via corridor-punching and BFS connectivity validation.

---

## Zone Types

| Zone | Visual Identity | Primary Assets | Game Feel |
|------|----------------|----------------|-----------|
| **Forest** | Dense canopy, undergrowth | tree\*, bush\*, stone\* | Tight sightlines, ambush cover |
| **Urban** | Grid buildings, rubble | wall, barrier, crate | Room-clearing, close quarters |
| **Industrial** | Compound walls + barrier rows | barrier (H+V), stone, barrel | Chokepoints, wide corridors |
| **Ruins** | Broken partial walls, debris | stone, wall fragments, rubble | Erratic sightlines, open pockets |
| **Wasteland** | Barren, sparse | stone, bush (rare) | Long-range engagements, open traversal |

---

## Generation Pipeline

```
1. ZoneLayout       — Poisson-disk seeds + Voronoi assignment → zone_grid[r][c]
2. Zone smoothing   — 2-pass majority-neighbour dilation (removes 1-cell spikes)
3. AssetLibrary     — load & cache all Assets/Maps/MapAssets/ files
4. ZoneFill         — per-zone filler (Urban + Industrial first, then nature zones)
5. BoundaryLinker   — punch walkable openings at zone-to-zone borders
6. ConnectivityFix  — BFS from centre; carve L-shaped corridors to isolated pockets
7. BorderEnforcer   — stamp outer # perimeter (always last)
8. Write UTF-8 file
```

---

## Class Breakdown

### `ZoneType` (enum)
`FOREST, URBAN, INDUSTRIAL, RUINS, WASTELAND`

### `AssetLibrary`
Loads and caches all asset files grouped by prefix. Returns empty list gracefully for
not-yet-created asset groups (`crates`, `barrels`, `rubble`, `sandbags`, `fences`).

### `place_seeds(num_zones, width, height, rng) → list[Seed]`
Poisson-disk-like rejection sampling. Seeds must be at minimum distance
`min(W,H) / sqrt(N) * 0.6` apart. First N seeds have ZoneType assigned round-robin
from a shuffled list of all types to guarantee ≥ 1 of each type.

### `build_zone_grid(seeds, width, height) → zone_grid`
Nearest-seed Voronoi: assigns each cell the zone_type of the closest seed (squared
Euclidean distance — no sqrt needed for comparison).

### `smooth_zones(zone_grid, passes=2)`
Majority-neighbour dilation: each cell adopts the most common zone type among its
8 neighbours. Eliminates 1-cell spikes and jagged zone edges.

### `ForestFiller`
- ~1 tree per 400 zone cells; 2–5 bushes clustered within `BUSH_SCATTER=6` radius
- ~1 stone per 200 zone cells; uses `stamp_if_clear` (padding=2 trees, 1 others)
- ~30% of zone cells stay open (natural glades)

### `UrbanFiller`
- 3–8 rectangular rooms (10–35 tall, 15–50 wide) within zone bounding box
- Room walls: `_` (H) and `|` (V) with `DOOR_WIDTH=4` doorways on all four sides
- 3–6 cover assets (barriers, crates, stones) stamped inside each room
- Track room list; avoid overlapping rooms with margin=3

### `IndustrialFiller`
- One large outer compound perimeter covering the zone bounding box (drawn as a room)
- Tiled horizontal barrier rows every 12–20 rows inside the compound
- Stone/barrel scatter inside (~1 per 500 cells)

### `RuinsFiller`
- Partial `_` and `|` wall segments (length 4–20) with random gaps (2–6 cells wide)
- ~1 stone/rubble per 100 zone cells — intentionally dense debris scatter

### `WastelandFiller`
- ~1 stone per 800 cells, ~1 bush per 1500 cells — intentionally sparse/open

### `punch_zone_corridors(grid, zone_grid, rng)`
1. Scan every adjacent cell pair for zone-type changes → collect H-boundary and
   V-boundary edge lists
2. Cluster contiguous boundary edges into segments
3. For each segment > 4 cells: punch `DOOR_WIDTH`-wide openings spaced every
   ~30 cells; minimum 2 openings per segment

### `fix_connectivity(grid, max_iterations=20)`
1. BFS from map centre (walks outward to find a walkable start if centre is solid)
2. Find all walkable cells not reachable from the flood-fill origin
3. For each unreachable pocket: find nearest reachable cell (sampled for large maps),
   carve an L-shaped corridor (horizontal leg first, then vertical)
4. Repeat until fully connected or max iterations exceeded

---

## Format Contract (unchanged — no C# changes needed)

| Property | Value |
|---|---|
| Format | Plain UTF-8, one line per row |
| Floor | `' '` (space) — anything not in `WallTiles` |
| Walls | `#`, `\|`, `_`, `-`, box-drawing, `█▓▒`, `♣■` |
| H-room wall | `_` (`H_WALL_CHAR`) |
| V-room wall | `\|` (`V_WALL_CHAR`) |
| Zone connectors | `' '` (space) — always walkable regardless of engine wall set |
| Stamp rule | space = transparent; all other chars overwrite the grid |
| Min size | 330 × 300 |
| HUD rows | Top 2 rows reserved; rendered with `yOffset=2` in `GameRunner.cs` |

---

## Missing Textures — Recommendations

These assets would significantly enhance zone visual identity. Each follows the
existing naming convention and should be placed in the appropriate subfolder under
`Assets/Maps/MapAssets/`.

| Asset File | Folder(s) | Zone Association | Description | Priority |
|---|---|---|---|---|
| `crate.txt` | Vertical/ + Horizontal/ | Industrial, Urban | Stackable cover box `[###]` ~3×5 | **High** |
| `barrel.txt` | Vertical/ + Horizontal/ | Industrial, Urban | Cylindrical obstacle `(O)` ~3×3 | **High** |
| `rubble.txt` | Vertical/ | Ruins | Irregular debris `.,:'` ~3×8 | **High** |
| `sandbag.txt` | Vertical/ + Horizontal/ | Industrial | Low cover `[~~~~~]` ~2×6 | **Medium** |
| `fence.txt` | Vertical/ + Horizontal/ | Urban perimeter | Repeating `+--+--+` / `+\|+\|` ~1×8 | **Medium** |
| `pillar.txt` | Vertical/ + Horizontal/ | Indoor rooms | Structural column `[■]` ~3×3 | **Medium** |
| `car_wreck.txt` | Horizontal/ only | Urban, Ruins | Post-apoc ASCII car outline ~3×12 | **Low** |
| `campfire.txt` | Vertical/ only | Forest | Glow point using `*` and `.` ~3×5 | **Low** |
| `bunker_wall.txt` | Vertical/ + Horizontal/ | Industrial | `▓▓▓` (solid/non-walkable) | **Low** |
| `water_edge.txt` | Vertical/ + Horizontal/ | Zone boundary marker | Decorative `~~~` row | **Low** |

New assets are loaded automatically by `AssetLibrary` using their filename prefix.
The generator works without them — missing groups return empty lists as a graceful fallback.

---

## Verification Steps

1. `python map_generator.py` with defaults (600×500, 12 zones) — no errors, completes
2. Open output in a text editor with word-wrap off — visually distinct zone regions visible
3. Standalone BFS on output file — all floor cells reachable from map centre
4. `dotnet run` — player spawns, walks between zones, no crashes
5. Top 2 rows remain `#` border — HUD not corrupted
6. Minimum dimensions 330×300 — generates without assertion errors
7. Same `zone_seed` integer produces identical output

---

## Scope Boundaries

- **Included:** Full rewrite of `map_generator.py`
- **Excluded:** Changes to any C# source files — map format contract is unchanged
- **Excluded:** Creation of the new texture `.txt` files (listed above as recommendations)
- **Decision:** Voronoi + dilation chosen over noise-map for predictability and debuggability
