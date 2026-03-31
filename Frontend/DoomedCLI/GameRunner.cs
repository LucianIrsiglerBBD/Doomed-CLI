using System;
using System.Collections.Generic;
using System.Threading;

namespace DoomedCLI;

static class GameRunner
{
    static void Main()
    {
        Console.CursorVisible = false;
        Console.Clear();

        using var input = new InputManager();
        var rng = new Random();
        var map = new GameMap("test.txt");
        var frameBuffer = new FrameBuffer(Console.WindowWidth, Console.WindowHeight);

        const int WeaponSpawnOffset  = 8;
        const int InitialEnemyCount  = 5;
        const int FrameDelayMs       = 30;

        // Spawn player at a random walkable position
        map.TryFindSpawnPosition(5, 3, rng, out int playerSpawnX, out int playerSpawnY);
        var player = new PlayerModel("Hero", 100, playerSpawnX, playerSpawnY);

        var weapons = new List<BaseWeapon>
        {
            new WeaponPistol(player.X - WeaponSpawnOffset, player.Y),
            new WeaponShotgun(player.X + WeaponSpawnOffset, player.Y)
        };

        var enemies = EnemyModel.SpawnGroup(InitialEnemyCount, map, rng);
        var enemyHitboxes = new List<Hitbox>(8);

        int killCount = 0;
        while (true)
        {
            if (input.IsKeyDown(ConsoleKey.Q)) break;

            int dx = 0, dy = 0;
            if (input.IsKeyDown(ConsoleKey.A)) dx -= player.Speed;
            if (input.IsKeyDown(ConsoleKey.D)) dx += player.Speed;
            if (input.IsKeyDown(ConsoleKey.W)) dy -= player.Speed;
            if (input.IsKeyDown(ConsoleKey.S)) dy += player.Speed;

            if (input.IsKeyDown(ConsoleKey.P)) player.TryPickupNearestWeapon(weapons);
            if (input.IsKeyDown(ConsoleKey.L)) player.DropWeapon();

            if (input.IsKeyDown(ConsoleKey.Spacebar))
                player.FireWeapon();

            // IsAnimatingShot stays true for ShotAnimationMs after TryFire succeeds,
            // so the muzzle flash is visible across multiple frames regardless of frame rate.
            bool isShooting = player.EquippedWeapon?.IsAnimatingShot ?? false;

            enemyHitboxes.Clear();
            foreach (var enemy in enemies) enemyHitboxes.Add(enemy.GetHitbox());
            player.Move(dx, dy, map, enemyHitboxes);

            player.EquippedWeapon?.Tick(map, enemies);
            killCount += enemies.RemoveAll(enemy => enemy.Health <= 0);

            map.CenterCamera(player.X, player.Y, frameBuffer.Width, frameBuffer.Height - 2);

            frameBuffer.Clear();
            map.Draw(frameBuffer, yOffset: 2);

            foreach (var groundWeapon in weapons)
                if (!groundWeapon.IsEquipped) groundWeapon.Draw(frameBuffer, map, yOffset: 2);

            player.Draw(frameBuffer, map, yOffset: 2);
            player.EquippedWeapon?.Draw(frameBuffer, map, isShooting, yOffset: 2);
            player.EquippedWeapon?.DrawBullets(frameBuffer, map, yOffset: 2);

            foreach (var enemy in enemies)
                enemy.Draw(frameBuffer, map, yOffset: 2);

            // HUD always at rows 0-1, drawn last so nothing overwrites it
            frameBuffer.WriteString(0, 0, "WASD move  P pickup  L drop  Space shoot  Q quit          ");
            frameBuffer.WriteString(0, 1, $"HP:{player.Health}  Kills:{killCount}  Enemies:{enemies.Count}  Facing:{player.Facing}  Equipped:{player.EquippedWeapon?.Name ?? "None"}  World:({player.X},{player.Y})   ");

            frameBuffer.Flush();

            Thread.Sleep(FrameDelayMs);
        }

        Console.CursorVisible = true;
        Console.Clear();
        Console.WriteLine("Thanks for playing!");
    }
}

