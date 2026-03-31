using System;
using System.IO;
using System.Linq;

namespace DoomedCLI;

class GameMap
{

    private static readonly HashSet<char> WallTiles =
    [
    // Legacy ASCII
    '#', '|', '-',
    // Single-line box-drawing
    '─', '│', '┌', '┐', '└', '┘', '├', '┤', '┬', '┴', '┼',
    // Double-line box-drawing
    '═', '║', '╔', '╗', '╚', '╝', '╠', '╣', '╦', '╩', '╬',
    // Solid fills
    '█', '▓', '▒',
    // Decoration (impassable)
    '♣', '■',
    ];
    private readonly char[][] _tiles;
    public int Width { get; }
    public int Height { get; }
    public int CameraX { get; private set; }
    public int CameraY { get; private set; }

    public GameMap(string mapFile)
    {
        var path = FindMapPath(mapFile);
        var lines = File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();

        if (lines.Length == 0)
        {
            _tiles = new[] { new[] { ' ' } };
            Width = 1;
            Height = 1;
            return;
        }

        Width = lines.Max(line => line.Length);
        Height = lines.Length;

        _tiles = new char[Height][];
        for (int row = 0; row < Height; row++)
        {
            var line = lines[row];
            _tiles[row] = new char[Width];
            for (int col = 0; col < Width; col++)
                _tiles[row][col] = col < line.Length ? line[col] : ' ';
        }
    }

    private static string FindMapPath(string mapFile)
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Assets", "Maps", mapFile)),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Assets", "Maps", mapFile)),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Maps", mapFile),
            Path.Combine("Assets", "Maps", mapFile),
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    public void CenterCamera(int worldX, int worldY, int viewportWidth, int viewportHeight)
    {
        CameraX = Math.Clamp(worldX - viewportWidth / 2, 0, Math.Max(0, Width - viewportWidth));
        CameraY = Math.Clamp(worldY - viewportHeight / 2, 0, Math.Max(0, Height - viewportHeight));
    }


    public (int sx, int sy) WorldToScreen(int worldX, int worldY, int yOffset = 0)
        => (worldX - CameraX, worldY - CameraY + yOffset);

    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        char tile = _tiles[y][x];
        return !WallTiles.Contains(tile);
    }

    public (int wx, int wy) ScreenToWorld(int screenX, int screenY, int yOffset = 0)
        => (screenX + CameraX, screenY - yOffset + CameraY);
    public bool TryFindSpawnPosition(int entityWidth, int entityHeight, Random rng, out int spawnX, out int spawnY)
    {
        const int maxAttempts = 500;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int candidateX = rng.Next(0, Math.Max(1, Width - entityWidth));
            int candidateY = rng.Next(0, Math.Max(1, Height - entityHeight));

            bool allCornersWalkable =
                IsWalkable(candidateX, candidateY) &&
                IsWalkable(candidateX + entityWidth - 1, candidateY) &&
                IsWalkable(candidateX, candidateY + entityHeight - 1) &&
                IsWalkable(candidateX + entityWidth - 1, candidateY + entityHeight - 1);

            if (allCornersWalkable)
            {
                spawnX = candidateX;
                spawnY = candidateY;
                return true;
            }
        }

        spawnX = Width / 2;
        spawnY = Height / 2;
        return false;
    }

    public void Draw(FrameBuffer fb, int yOffset = 0)
    {
        int viewportRows = fb.Height - yOffset;

        for (int screenY = 0; screenY < viewportRows; screenY++)
        {
            int worldY = screenY + CameraY;
            if (worldY < 0 || worldY >= Height) continue;

            // Clamp the visible world-X range
            int worldXStart = Math.Max(0, CameraX);
            int worldXEnd = Math.Min(Width, CameraX + fb.Width);
            if (worldXEnd <= worldXStart) continue;

            int destinationColumn = worldXStart - CameraX;           // destination column in fb
            int visibleColumnCount = worldXEnd - worldXStart;

            fb.WriteRow(destinationColumn, screenY + yOffset, _tiles[worldY], worldXStart, visibleColumnCount);
        }
    }
}
