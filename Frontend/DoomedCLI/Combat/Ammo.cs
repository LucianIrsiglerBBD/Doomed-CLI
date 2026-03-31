namespace DoomedCLI;

class Ammo
{
    public int Damage { get; set; }
    public float Speed { get; set; }
    public int WorldX { get; set; }
    public int WorldY { get; set; }
    public BaseEntity.FacingDirection Facing { get; set; }
    public char Symbol { get; set; }
    public bool IsActive { get; private set; } = true;


    public void Deactivate() => IsActive = false;

    private float _subpixelX;
    private float _subpixelY;

    public Ammo(int damage, float speed, char symbol)
    {
        Damage = damage;
        Speed = speed;
        Symbol = symbol;
    }

    public Ammo Spawn(int worldX, int worldY, BaseEntity.FacingDirection facing)
    {
        return new Ammo(Damage, Speed, Symbol)
        {
            WorldX = worldX,
            WorldY = worldY,
            Facing = facing,
            IsActive = true,
        };
    }

    public void Update(GameMap map)
    {
        if (!IsActive) return;

        _subpixelX += Facing == BaseEntity.FacingDirection.Left ? -Speed
                 : Facing == BaseEntity.FacingDirection.Right ? Speed
                 : 0f;

        _subpixelY += Facing == BaseEntity.FacingDirection.Up ? -Speed
                 : Facing == BaseEntity.FacingDirection.Down ? Speed
                 : 0f;

        int tileStepsX = (int)_subpixelX;
        int tileStepsY = (int)_subpixelY;
        _subpixelX -= tileStepsX;
        _subpixelY -= tileStepsY;

        if (tileStepsX == 0 && tileStepsY == 0) return;

        // Step one tile at a time so fast bullets can't skip through thin walls.
        // Bullets only ever travel along one axis, so the two loops don't interact.
        int moveDirectionX = Math.Sign(tileStepsX);
        int moveDirectionY = Math.Sign(tileStepsY);

        for (int i = 0; i < Math.Abs(tileStepsX); i++)
        {
            if (!map.IsWalkable(WorldX + moveDirectionX, WorldY)) { IsActive = false; return; }
            WorldX += moveDirectionX;
        }

        for (int i = 0; i < Math.Abs(tileStepsY); i++)
        {
            if (!map.IsWalkable(WorldX, WorldY + moveDirectionY)) { IsActive = false; return; }
            WorldY += moveDirectionY;
        }
    }

    public void Draw(FrameBuffer fb, GameMap map, int yOffset = 0)
    {
        if (!IsActive) return;
        var (screenX, screenY) = map.WorldToScreen(WorldX, WorldY, yOffset);
        if (screenX < 0 || screenX >= fb.Width || screenY < yOffset || screenY >= fb.Height) return;
        fb.Set(screenX, screenY, Symbol);
    }
}
