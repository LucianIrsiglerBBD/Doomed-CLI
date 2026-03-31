namespace DoomedCLI;

class WeaponPistol : BaseWeapon
{
    public WeaponPistol(int startX, int startY)
        : base("Pistol", "A basic handgun with moderate damage and range.", 10, 5, startX, startY, "base_pistol.txt")
    {
        FireRateMs = 400; // 2.5 shots/sec
        AmmoType = new Ammo(10, 2f, '*');
    }

    public override void Draw(FrameBuffer fb, GameMap map, bool isShooting = false, int yOffset = 0)
    {
        base.Draw(fb, map, isShooting, yOffset);

        if (isShooting)
        {
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

            var (weaponScreenX, weaponScreenY) = map.WorldToScreen(worldX, worldY, yOffset);
            var facing = Owner?.Facing ?? BaseEntity.FacingDirection.Right;

            int flashScreenX = facing switch
            {
                BaseEntity.FacingDirection.Left => Math.Clamp(weaponScreenX - 1, 0, fb.Width - 1),
                BaseEntity.FacingDirection.Up => Math.Clamp(weaponScreenX + Width / 2, 0, fb.Width - 1),
                BaseEntity.FacingDirection.Down => Math.Clamp(weaponScreenX + Width / 2, 0, fb.Width - 1),
                _ => Math.Clamp(weaponScreenX + Width, 0, fb.Width - 1),
            };
            int flashScreenY = facing switch
            {
                BaseEntity.FacingDirection.Up => Math.Clamp(weaponScreenY - 1, yOffset, fb.Height - 1),
                BaseEntity.FacingDirection.Down => Math.Clamp(weaponScreenY + Height, yOffset, fb.Height - 1),
                _ => Math.Clamp(weaponScreenY, yOffset, fb.Height - 1),
            };

            fb.Set(flashScreenX, flashScreenY, '*'); // flash clears next frame via fb.Clear()
        }
    }

}