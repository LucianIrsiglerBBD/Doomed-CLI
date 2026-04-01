namespace DoomedCLI;

class WeaponShotgun : BaseWeapon
{
    public WeaponShotgun(int startX, int startY)
        : base("Shotgun", "A heavy close-range firearm with high damage.", 18, 3, startX, startY, "base_shotgun.txt")
    {
        FireRateMs = 900;
        AmmoType = new Ammo(18, 1f, 'o');
    }

    public override void Draw(FrameBuffer fb, GameMap map, bool isShooting = false, int yOffset = 0)
    {

        if (isShooting)
        {
            int worldX, worldY;
            var facing = BaseEntity.FacingDirection.Right;

            if (IsEquipped && Owner != null)
            {
                var (offsetX, offsetY) = Owner.GetWeaponOffset(this);
                worldX = Owner.X + offsetX;
                worldY = Owner.Y + offsetY;
                facing = Owner.Facing;
            }
            else
            {
                worldX = X;
                worldY = Y;
            }

            var (weaponScreenX, weaponScreenY) = map.WorldToScreen(worldX, worldY, yOffset);

            int barrelCenterX    = Math.Clamp(weaponScreenX + Width / 2, 0, fb.Width - 1);
            int muzzleRightNear  = Math.Clamp(weaponScreenX + Width,     0, fb.Width - 1);
            int muzzleRightFar   = Math.Clamp(weaponScreenX + Width + 1, 0, fb.Width - 1);
            int muzzleLeftNear   = Math.Clamp(weaponScreenX - 1,         0, fb.Width - 1);
            int muzzleLeftFar    = Math.Clamp(weaponScreenX - 2,         0, fb.Width - 1);
            int muzzleTopNear    = Math.Clamp(weaponScreenY - 1,         yOffset, fb.Height - 1);
            int muzzleTopFar     = Math.Clamp(weaponScreenY - 2,         yOffset, fb.Height - 1);
            int muzzleBottomNear = Math.Clamp(weaponScreenY + Height,     yOffset, fb.Height - 1);
            int muzzleBottomFar  = Math.Clamp(weaponScreenY + Height + 1, yOffset, fb.Height - 1);

            (int flashX1, int flashY1, int flashX2, int flashY2) = facing switch
            {
                BaseEntity.FacingDirection.Left  => (muzzleLeftNear,  weaponScreenY, muzzleLeftFar,    weaponScreenY),
                BaseEntity.FacingDirection.Up    => (barrelCenterX,   muzzleTopNear, barrelCenterX,    muzzleTopFar),
                BaseEntity.FacingDirection.Down  => (barrelCenterX, muzzleBottomNear, barrelCenterX, muzzleBottomFar),
                _                                => (muzzleRightNear, weaponScreenY, muzzleRightFar,   weaponScreenY),
            };
            fb.Set(flashX1, flashY1, '*');
            fb.Set(flashX2, flashY2, '*');
        }
    }
}
