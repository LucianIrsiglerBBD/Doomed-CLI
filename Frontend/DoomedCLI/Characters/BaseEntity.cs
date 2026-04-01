using System;
using System.IO;

namespace DoomedCLI;

abstract class BaseEntity // Represents a generic entity in the game world, such as players, enemies
{
    public enum FacingDirection { Up, Down, Left, Right }

    public string Name { get; set; }
    public int Health { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public FacingDirection Facing { get; set; }
    private string[] _spriteRight = Array.Empty<string>();
    private string[] _spriteLeft = Array.Empty<string>();
    private string[] _spriteVertical = Array.Empty<string>();
    private int _spriteRightWidth, _spriteLeftWidth, _spriteVerticalWidth;

    public string[] Sprite => Facing switch
    {
        FacingDirection.Left => _spriteLeft,
        FacingDirection.Up or FacingDirection.Down => _spriteVertical,
        _ => _spriteRight,
    };

    public int SpriteWidth => Facing switch
    {
        FacingDirection.Left => _spriteLeftWidth,
        FacingDirection.Up or FacingDirection.Down => _spriteVerticalWidth,
        _ => _spriteRightWidth,
    };
    public int SpriteHeight => Sprite.Length;

    // Stable across all facing orientations — safe for collision box calculations.
    public int MaxSpriteWidth  { get; private set; }
    public int MaxSpriteHeight { get; private set; }

    private void ComputeMaxSpriteDims()
    {
        static int MaxLineWidth(string[] lines) { int max = 0; foreach (var line in lines) if (line.Length > max) max = line.Length; return max; }
        _spriteRightWidth    = MaxLineWidth(_spriteRight);
        _spriteLeftWidth     = MaxLineWidth(_spriteLeft);
        _spriteVerticalWidth = MaxLineWidth(_spriteVertical);
        MaxSpriteWidth  = Math.Max(_spriteRightWidth, Math.Max(_spriteLeftWidth, _spriteVerticalWidth));
        MaxSpriteHeight = Math.Max(_spriteRight.Length, Math.Max(_spriteLeft.Length, _spriteVertical.Length));
    }

    public Hitbox GetHitbox() => new Hitbox(X, Y, SpriteWidth, SpriteHeight);

    // Searches common asset directories for the sprite file, trying each relative path in order.
    private static string? FindSpriteFile(params string[] relPaths)
    {
        var baseDir = AppContext.BaseDirectory;
        var cwdDir = Directory.GetCurrentDirectory();
        foreach (var rel in relPaths)
        {
            var candidates = new[]
            {
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Assets", "Sprites", rel)),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Assets", "Sprites", rel)),
                Path.Combine(cwdDir, "Assets", "Sprites", rel),
                Path.Combine("Assets", "Sprites", rel),
            };
            var found = Array.Find(candidates, File.Exists);
            if (found != null) return found;
        }
        return null;
    }

    public BaseEntity(string name, int health, int startX, int startY, string spriteFile)
    {
        Name = name;
        Health = health;
        X = startX;
        Y = startY;
        Facing = FacingDirection.Right;
        string baseName = Path.GetFileNameWithoutExtension(spriteFile);

        //Sprite loading logic: sprite file names follow a naming convention
        _spriteRight = LoadSprite(FindSpriteFile(
            $"Horizontal/Right/{baseName}_horizontal_right.txt",
            $"Horizontal/{baseName}_horizontal.txt",
            spriteFile));

        _spriteLeft = LoadSprite(FindSpriteFile(
            $"Horizontal/Left/{baseName}_horizontal_left.txt",
            $"Horizontal/Right/{baseName}_horizontal_right.txt",
            $"Horizontal/{baseName}_horizontal.txt",
            spriteFile));

        _spriteVertical = LoadSprite(FindSpriteFile(
            $"Vertical/{baseName}_vertical.txt"));

        if (_spriteVertical.Length == 1 && _spriteVertical[0] == "@")
            _spriteVertical = _spriteRight;

        ComputeMaxSpriteDims();
    }

    private static string[] LoadSprite(string? path)
    {
        if (path == null || !File.Exists(path)) return new[] { "@" };
        var lines = File.ReadAllLines(path);
        return lines.Length == 0 ? new[] { "@" } : lines;
    }

    public void Draw(FrameBuffer fb, GameMap map, int yOffset = 0)
    {
        var (screenX, screenY) = map.WorldToScreen(X, Y, yOffset);

        // Don't show peeps outside console bounds
        if (
            (screenX >= fb.Width || screenX + SpriteWidth <= 0) ||
            (screenY >= fb.Height || screenY + SpriteHeight <= yOffset)
        )
            return;

        for (int spriteRow = 0; spriteRow < Sprite.Length; spriteRow++)
        {
            int screenRow = screenY + spriteRow;
            if (screenRow < yOffset || screenRow >= fb.Height) continue;

            var line = Sprite[spriteRow];

            int skipLeadingChars = Math.Max(0, -screenX);
            if (skipLeadingChars >= line.Length) continue;

            int destX = Math.Max(0, screenX);
            int charsVisible = Math.Min(line.Length - skipLeadingChars, fb.Width - destX);
            if (charsVisible <= 0) continue;

            fb.WriteString(destX, screenRow, line, skipLeadingChars, charsVisible);
        }
    }
}