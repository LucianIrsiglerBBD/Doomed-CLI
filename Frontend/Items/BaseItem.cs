using System.Linq;

abstract class BaseItem // Represents a generic item in the game world, such as weapons, potions
{
    public enum FacingDirection { Up, Down, Left, Right }

    public string Name { get; set; }
    public string Description { get; set; }
    public FacingDirection Facing { get; set; }
    private string[] _spriteHorizontal = new string[0];
    private string[] _spriteVertical = new string[0];
    public string[] Sprite => (Facing == FacingDirection.Up || Facing == FacingDirection.Down)
        ? _spriteVertical
        : _spriteHorizontal;

// will be fallback when server integration is done
    private static string FindSpritePath(string spriteFile, string? fallbackFile = null)
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new List<string>
        {
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Sprites", spriteFile)),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Sprites", spriteFile)),
            Path.Combine(Directory.GetCurrentDirectory(), "Sprites", spriteFile),
        };
        if (fallbackFile != null)
        {
            candidates.Add(Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Sprites", fallbackFile)));
            candidates.Add(Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Sprites", fallbackFile)));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "Sprites", fallbackFile));
            candidates.Add(Path.Combine("Sprites", fallbackFile));
        }
        candidates.Add(Path.Combine("Sprites", spriteFile));
        return candidates.FirstOrDefault(File.Exists) ?? candidates.Last();
    }

    public BaseItem(string name, string description, string spriteFile = "base_item.txt")
    {
        Name = name;
        Description = description;
        Facing = FacingDirection.Right;
        string spriteBaseName = Path.GetFileNameWithoutExtension(spriteFile);
        string horizontalSpritePath = Path.Combine("Horizontal", spriteBaseName + "_horizontal.txt");
        string verticalSpritePath = Path.Combine("Vertical", spriteBaseName + "_vertical.txt");
        _spriteHorizontal = LoadSprite(FindSpritePath(horizontalSpritePath, spriteFile));
        _spriteVertical = LoadSprite(FindSpritePath(verticalSpritePath));
        if (_spriteVertical.Length == 1 && _spriteVertical[0] == "#")
            _spriteVertical = _spriteHorizontal;
    }

    private string[] LoadSprite(string path)
    {
        if (!File.Exists(path))
        {
            return new[] { "#" };
        }

        var lines = File.ReadAllLines(path);
        return lines.Length == 0 ? new[] { "#" } : lines;
    }

    public void Draw(int x, int y)
    {
        for (int spriteRow = 0; spriteRow < Sprite.Length; spriteRow++)
        {
            int screenRow = y + spriteRow;
            if (screenRow < 0 || screenRow >= Console.WindowHeight) continue;

            var line = Sprite[spriteRow];
            // Flip sprite horizontally if facing left
            if (Facing == FacingDirection.Left)
            {
                line = new string(line.Reverse().ToArray());
            }

            int clampedScreenX = Math.Max(0, Math.Min(Console.WindowWidth - line.Length, x));
            Console.SetCursorPosition(clampedScreenX, screenRow);
            Console.Write(line);
        }
    }
}