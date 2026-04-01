namespace DoomedCLI;

class PlayerModel : BaseEntity
{
    public BaseWeapon? EquippedWeapon { get; private set; }

    public int Speed { get; set; } = 1;

    public PlayerModel(string name, int health, int x, int y) : base(name, health, x, y, "base_player.txt")
    {
        EquippedWeapon = null;
        Facing = FacingDirection.Right;
    }

    public void Move(int dx, int dy, GameMap map, IEnumerable<Hitbox>? entityBlockers = null)
    {
        if (dx != 0 || dy != 0)
        {
            if (Math.Abs(dx) > Math.Abs(dy))
                Facing = dx > 0 ? FacingDirection.Right : FacingDirection.Left;
            else if (Math.Abs(dy) > 0)
                Facing = dy > 0 ? FacingDirection.Down : FacingDirection.Up;
        }

        int weaponPad  = EquippedWeapon == null ? 0
            : Math.Max(EquippedWeapon.MaxWidth, EquippedWeapon.MaxHeight);
        // Use current facing sprite dims — sprites are designed with consistent max widths
        // so this is stable. +1 absorbs integer-division rounding from the ceil-half.
        int collisionRadius = (Math.Max(SpriteWidth, SpriteHeight) + 1) / 2 + weaponPad + 1;

        int boxLeft   = -collisionRadius;
        int boxRight  =  collisionRadius;
        int boxTop    = -collisionRadius;
        int boxBottom =  collisionRadius;
        int boxWidth  =  boxRight  - boxLeft  + 1;
        int boxHeight =  boxBottom - boxTop   + 1;

        const int wallBuffer = 1; // minimum gap to maintain between box edge and wall
        int newX = Math.Clamp(X + dx, wallBuffer - boxLeft,  map.Width  - 1 - boxRight  - wallBuffer);
        int newY = Math.Clamp(Y + dy, wallBuffer - boxTop,   map.Height - 1 - boxBottom - wallBuffer);

        bool canMoveX = dx == 0;
        if (!canMoveX)
        {
            int leadingEdgeX = dx > 0 ? newX + boxRight + wallBuffer : newX + boxLeft - wallBuffer;
            canMoveX = true;
            for (int row = newY + boxTop; row <= newY + boxBottom && canMoveX; row++)
                if (!map.IsWalkable(leadingEdgeX, row)) canMoveX = false;
        }

        int effectiveX = canMoveX ? newX : X;

        bool canMoveY = dy == 0;
        if (!canMoveY)
        {
            int leadingEdgeY = dy > 0 ? newY + boxBottom + wallBuffer : newY + boxTop - wallBuffer;
            canMoveY = true;
            for (int col = effectiveX + boxLeft; col <= effectiveX + boxRight && canMoveY; col++)
                if (!map.IsWalkable(col, leadingEdgeY)) canMoveY = false;
        }

        if (entityBlockers != null)
        {
            var hitboxX = new Hitbox(newX       + boxLeft, Y    + boxTop, boxWidth, boxHeight);
            var hitboxY = new Hitbox(effectiveX + boxLeft, newY + boxTop, boxWidth, boxHeight);
            foreach (var blocker in entityBlockers)
            {
                if (canMoveX && hitboxX.Overlaps(blocker)) canMoveX = false;
                if (canMoveY && hitboxY.Overlaps(blocker)) canMoveY = false;
                if (!canMoveX && !canMoveY) break;
            }
        }

        X = canMoveX ? newX : X;
        Y = canMoveY ? newY : Y;
    }
    public (int x, int y) GetWeaponOffset(BaseWeapon weapon)
    {
        int playerSpriteWidth = SpriteWidth;
        int playerSpriteHeight = SpriteHeight;
        int weaponSpriteWidth = weapon.Width;
        int weaponSpriteHeight = weapon.Height;

        return Facing switch
        {
            FacingDirection.Right => (playerSpriteWidth, playerSpriteHeight / 2 - weaponSpriteHeight / 2),
            FacingDirection.Left => (-weaponSpriteWidth, playerSpriteHeight / 2 - weaponSpriteHeight / 2),
            FacingDirection.Up => (playerSpriteWidth / 2 - weaponSpriteWidth / 2, -weaponSpriteHeight),
            FacingDirection.Down => (playerSpriteWidth / 2 - weaponSpriteWidth / 2, playerSpriteHeight),
            _ => (playerSpriteWidth, 0),
        };
    }

    public void TryPickupNearestWeapon(IEnumerable<BaseWeapon> weapons, int pickupRange = 2)
    {
        if (EquippedWeapon != null) return;

        BaseWeapon? nearest = null;
        double nearestDistance = double.MaxValue;
        foreach (var weapon in weapons)
        {
            if (weapon.IsEquipped) continue;
            double distance = Math.Abs(X - weapon.X) + Math.Abs(Y - weapon.Y);
            if (distance <= pickupRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = weapon;
            }
        }
        if (nearest != null) EquipWeapon(nearest);
    }

    public void EquipWeapon(BaseWeapon weapon)
    {
        if (weapon == null) throw new ArgumentNullException(nameof(weapon));

        // drop currently equipped at current player position
        EquippedWeapon?.PutDown(X, Y);

        weapon.PickUp(this);
        EquippedWeapon = weapon;
    }

    public void DropWeapon()
    {
        EquippedWeapon?.PutDown(X, Y);
        EquippedWeapon = null;
    }

    public void FireWeapon()
    {
        if (EquippedWeapon == null) return;
        if (!EquippedWeapon.TryFire()) return;

        var (muzzleX, muzzleY) = EquippedWeapon.GetMuzzlePosition(this);
        EquippedWeapon.Fire(muzzleX, muzzleY, Facing);
    }
}