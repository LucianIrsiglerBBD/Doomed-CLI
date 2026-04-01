using System;
using System.Collections.Generic;
using System.IO;

namespace DoomedCLI;

abstract class BaseWeapon
{
    public enum FacingDirection { Up, Down, Left, Right }

    public string Name { get; set; }
    public string Description { get; set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Damage { get; set; }
    public int Range { get; set; }
    public bool IsEquipped { get; private set; }
    public FacingDirection Facing { get; set; }
    public PlayerModel? Owner { get; private set; }
    private string[] _spriteRight = Array.Empty<string>();
    private string[] _spriteLeft = Array.Empty<string>();
    private string[] _spriteVertical = Array.Empty<string>();
    private int _spriteRightWidth, _spriteLeftWidth, _spriteVerticalWidth;
    private int _spriteRightHeight, _spriteLeftHeight, _spriteVerticalHeight;

    public string[] Sprite => Facing switch
    {
        FacingDirection.Left => _spriteLeft,
        FacingDirection.Up or FacingDirection.Down => _spriteVertical,
        _ => _spriteRight,
    };
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

    private static string[] LoadSprite(string? path)
    {
        if (path == null || !File.Exists(path)) return new[] { "-" };
        var lines = File.ReadAllLines(path);
        return lines.Length == 0 ? new[] { "-" } : lines;
    }

    public BaseWeapon(string name, string description, int damage, int range, int startX, int startY, string spriteFile)
    {
        Name = name;
        Description = description;
        Damage = damage;
        Range = range;
        X = startX;
        Y = startY;
        IsEquipped = false;
        Facing = FacingDirection.Right;
        Owner = null;
        string baseName = Path.GetFileNameWithoutExtension(spriteFile);

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

        if (_spriteVertical.Length == 1 && _spriteVertical[0] == "-")
            _spriteVertical = _spriteRight;

        ComputeMaxExtents();
    }

    public int FireRateMs { get; protected set; } = 500;

    public int ShotAnimationMs { get; protected set; } = 80;

    protected Ammo? AmmoType { get; set; }

    private readonly List<Ammo> _activeBullets = new();

    public void Fire(int worldX, int worldY, BaseEntity.FacingDirection facing)
    {
        var bullet = AmmoType?.Spawn(worldX, worldY, facing);
        if (bullet != null) _activeBullets.Add(bullet);
    }

    public void Tick(GameMap map, IEnumerable<BaseEntity> targets)
    {
        foreach (var bullet in _activeBullets) bullet.Update(map);
        _activeBullets.RemoveAll(bullet => !bullet.IsActive);
        ResolveBulletHits(targets);
    }

    public void DrawBullets(FrameBuffer fb, GameMap map, int yOffset = 0)
    {
        foreach (var bullet in _activeBullets) bullet.Draw(fb, map, yOffset);
    }

    private void ResolveBulletHits(IEnumerable<BaseEntity> targets)
    {
        foreach (var bullet in _activeBullets)
        {
            if (!bullet.IsActive) continue;

            foreach (var target in targets)
            {
                if (target.GetHitbox().Contains(bullet.WorldX, bullet.WorldY))
                {
                    target.Health -= bullet.Damage;
                    bullet.Deactivate();
                    break;
                }
            }
        }
    }

    // null = never fired; avoids long.MinValue overflow that makes IsAnimatingShot always true.
    private long? _lastFireTick = null;

    public bool TryFire()
    {
        long currentTick = Environment.TickCount64;
        if (_lastFireTick.HasValue && currentTick - _lastFireTick.Value < FireRateMs) return false;
        _lastFireTick = currentTick;
        return true;
    }

    public bool IsAnimatingShot
        => _lastFireTick.HasValue && (Environment.TickCount64 - _lastFireTick.Value) < ShotAnimationMs;

    public void PickUp(PlayerModel player)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));

        IsEquipped = true;
        Owner = player;
    }

    public (int x, int y) GetMuzzlePosition(PlayerModel owner)
    {
        var (offsetX, offsetY) = owner.GetWeaponOffset(this);
        int weaponX = owner.X + offsetX;
        int weaponY = owner.Y + offsetY;

        return owner.Facing switch
        {
            BaseEntity.FacingDirection.Right => (weaponX + Width, weaponY),
            BaseEntity.FacingDirection.Left => (weaponX - 1, weaponY),
            BaseEntity.FacingDirection.Up => (weaponX + Width / 2, weaponY - 1),
            BaseEntity.FacingDirection.Down => (weaponX + Width / 2, weaponY + Height),
            _ => (weaponX + Width, weaponY),
        };
    }

    public void PutDown(int x, int y)
    {
        IsEquipped = false;
        Owner = null;
        X = x;
        Y = y;
    }

    private FacingDirection GetEffectiveFacing()
    {
        if (IsEquipped && Owner != null)
        {
            return Owner.Facing switch
            {
                BaseEntity.FacingDirection.Up => FacingDirection.Up,
                BaseEntity.FacingDirection.Down => FacingDirection.Down,
                BaseEntity.FacingDirection.Left => FacingDirection.Left,
                BaseEntity.FacingDirection.Right => FacingDirection.Right,
                _ => FacingDirection.Right,
            };
        }

        return Facing;
    }

    private string[] GetEffectiveSprite()
    {
        var facing = GetEffectiveFacing();
        return facing switch
        {
            FacingDirection.Left => _spriteLeft,
            FacingDirection.Up or FacingDirection.Down => _spriteVertical,
            _ => _spriteRight,
        };
    }

    public virtual void Draw(FrameBuffer fb, GameMap map, bool isShooting = false, int yOffset = 0)
    {
        var sprite = GetEffectiveSprite();
        int spriteWidth = Width;
        int worldX, worldY;

        if (IsEquipped && Owner != null)
        {
            var (offsetX, offsetY) = Owner.GetWeaponOffset(this);
            worldX = Owner.X + offsetX;
            worldY = Owner.Y + offsetY;
        }
        else
        {
            worldX = X;
            worldY = Y;
        }

        var (drawX, drawY) = map.WorldToScreen(worldX, worldY, yOffset);
        drawX = Math.Clamp(drawX, 0, Math.Max(0, fb.Width - spriteWidth));
        drawY = Math.Clamp(drawY, yOffset, Math.Max(yOffset, fb.Height - sprite.Length));

        for (int spriteRow = 0; spriteRow < sprite.Length; spriteRow++)
        {
            int screenRow = drawY + spriteRow;
            if (screenRow < yOffset || screenRow >= fb.Height) continue;

            int clampedScreenX = Math.Max(0, Math.Min(fb.Width - sprite[spriteRow].Length, drawX));
            fb.WriteString(clampedScreenX, screenRow, sprite[spriteRow]);
        }

        if (isShooting)
        {
            var facing = Owner?.Facing ?? BaseEntity.FacingDirection.Right;
            DrawShootingEffect(fb, map, drawX, drawY, facing);
        }
    }

    public int Width
    {
        get
        {
            return GetEffectiveFacing() switch
            {
                FacingDirection.Left => _spriteLeftWidth,
                FacingDirection.Up or FacingDirection.Down => _spriteVerticalWidth,
                _ => _spriteRightWidth,
            };
        }
    }

    public int Height
    {
        get
        {
            return GetEffectiveFacing() switch
            {
                FacingDirection.Left => _spriteLeftHeight,
                FacingDirection.Up or FacingDirection.Down => _spriteVerticalHeight,
                _ => _spriteRightHeight,
            };
        }
    }

    // Max column count and row count across every loaded sprite orientation.
    // Stable per axis — never changes with facing — safe to use as collision
    // padding without the box shape changing on a direction change.
    public int MaxWidth  { get; private set; }
    public int MaxHeight { get; private set; }

    private void ComputeMaxExtents()
    {
        static int MaxLineWidth(string[] lines) { int max = 0; foreach (var line in lines) if (line.Length > max) max = line.Length; return max; }
        _spriteRightWidth    = MaxLineWidth(_spriteRight);
        _spriteLeftWidth     = MaxLineWidth(_spriteLeft);
        _spriteVerticalWidth = MaxLineWidth(_spriteVertical);
        _spriteRightHeight   = _spriteRight.Length;
        _spriteLeftHeight    = _spriteLeft.Length;
        _spriteVerticalHeight = _spriteVertical.Length;
        MaxWidth  = Math.Max(_spriteRightWidth,  Math.Max(_spriteLeftWidth,  _spriteVerticalWidth));
        MaxHeight = Math.Max(_spriteRightHeight, Math.Max(_spriteLeftHeight, _spriteVerticalHeight));
    }

    public virtual void DrawShootingEffect(FrameBuffer fb, GameMap map, int screenX, int screenY, BaseEntity.FacingDirection facing)
    {
        // Default does nothing; subclass can override.
    }
}