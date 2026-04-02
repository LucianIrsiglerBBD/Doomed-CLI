using System;
using System.Collections.Generic;
using System.Threading;
using DoomedCLI.Utility;

namespace DoomedCLI;

static class GameRunner
{
    // Called from StartGameCommand for a live networked session.
    public static void Run(GameMap map, string username, string lobbyCode, string hostIp)
    {
        var sync = new GameSyncService(username, hostIp);
        RunCore(map, username, sync);
    }

    // Called from TestGameCommand — no auth, no backend, two wandering bots.
    public static void RunTest(string username = "TestPlayer")
    {
        var map = new GameMap("generated.txt");
        var rng = new Random();
        map.TryFindSpawnPosition(5, 3, rng, out int sx, out int sy);
        var sync = new FakeGameSyncService(username, map, sx, sy);
        RunCore(map, username, sync);
    }

    private static void RunCore(GameMap map, string username, IGameSyncService? sync)
    {
        Console.CursorVisible = false;
        Console.Clear();

        using var input = new InputManager();
        var rng = new Random();
        var frameBuffer = new FrameBuffer(Console.WindowWidth, Console.WindowHeight);

        const int WeaponSpawnOffset = 8;
        const int FrameDelayMs      = 30;
        const int SyncEveryNFrames  = 10; // ~300 ms

        map.TryFindSpawnPosition(5, 3, rng, out int playerSpawnX, out int playerSpawnY);
        var player = new PlayerModel(username, 100, playerSpawnX, playerSpawnY);

        var weapons = new List<BaseWeapon>
        {
            new WeaponPistol(player.X - WeaponSpawnOffset, player.Y),
            new WeaponShotgun(player.X + WeaponSpawnOffset, player.Y)
        };

        // In networked mode enemies are populated from the server; in local mode spawn dummies.
        var enemies = sync is null
            ? EnemyModel.SpawnGroup(40, map, rng)
            : new List<EnemyModel>();

        var enemyHitboxes = new List<Hitbox>(8);
        int killCount = 0;
        int syncFrame = 0;
        // Shared cell for server-reported self health (set from async callback, read on game thread).
        // int[] makes the reference captured; int writes are atomic on .NET for aligned words.
        var pendingHealth = new int[] { -1 };

        while (true)
        {
            if (input.IsKeyDown(ConsoleKey.Q)) break;
            if (player.Health <= 0) break;

            int dx = 0, dy = 0;
            if (input.IsKeyDown(ConsoleKey.A)) dx -= player.Speed;
            if (input.IsKeyDown(ConsoleKey.D)) dx += player.Speed;
            if (input.IsKeyDown(ConsoleKey.W)) dy -= player.Speed;
            if (input.IsKeyDown(ConsoleKey.S)) dy += player.Speed;

            if (input.IsKeyDown(ConsoleKey.P)) player.TryPickupNearestWeapon(weapons);
            if (input.IsKeyDown(ConsoleKey.L)) player.DropWeapon();

            if (input.IsKeyDown(ConsoleKey.Spacebar))
                player.FireWeapon();

            bool isShooting = player.EquippedWeapon?.IsAnimatingShot ?? false;

            enemyHitboxes.Clear();
            foreach (var enemy in enemies) enemyHitboxes.Add(enemy.GetHitbox());
            // Only block on enemy hitboxes for local NPCs. Networked/fake players have
            // stale positions due to polling lag — blocking on them wedges you against walls.
            player.Move(dx, dy, map, sync is null ? enemyHitboxes : null);

            // Snapshot enemy health before Tick() so we can detect bullet hits.
            Dictionary<string, int>? healthSnap = null;
            if (sync is not null && player.EquippedWeapon is not null && enemies.Count > 0)
            {
                healthSnap = new Dictionary<string, int>(enemies.Count);
                foreach (var e in enemies) healthSnap[e.Name] = e.Health;
            }

            player.EquippedWeapon?.Tick(map, enemies);

            // Post a hit to the server for every enemy whose health dropped this frame.
            if (sync is not null && healthSnap is not null && player.EquippedWeapon is not null)
            {
                foreach (var enemy in enemies)
                {
                    if (healthSnap.TryGetValue(enemy.Name, out int prev) && enemy.Health < prev)
                    {
                        int dmg = prev - enemy.Health;
                        string wep = player.EquippedWeapon.Name;
                        string target = enemy.Name;
                        _ = sync.PostHitAsync(username, target, wep, dmg)
                            .ContinueWith(t2 => { }, TaskContinuationOptions.OnlyOnFaulted);
                    }
                }
            }

            killCount += enemies.RemoveAll(e => e.Health <= 0);

            // Apply incoming damage posted by other clients via the /hit endpoint.
            int incoming = pendingHealth[0];
            if (incoming >= 0 && incoming < player.Health)
            {
                player.Health = incoming;
                pendingHealth[0] = -1;
            }

            // Network sync tick
            if (sync is not null && syncFrame++ % SyncEveryNFrames == 0)
            {
                // Fire-and-forget — we don't await these on the game thread.
                _ = sync.PostStateAsync(player.X, player.Y, player.Health).ContinueWith(
                    t => { /* swallow network errors silently */ }, TaskContinuationOptions.OnlyOnFaulted);

                _ = sync.GetAllStatesAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted) return;
                    var self = t.Result.FirstOrDefault(s => s.Username == username);
                    if (self is not null) pendingHealth[0] = self.Health;
                    ReconcileEnemies(enemies, t.Result, username, map, rng);
                }, TaskScheduler.Default);
            }

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

            frameBuffer.WriteString(0, 0, "WASD move  P pickup  L drop  Space shoot  Q quit          ");
            frameBuffer.WriteString(0, 1, $"HP:{player.Health}  Kills:{killCount}  Players:{enemies.Count}  Facing:{player.Facing}  Equipped:{player.EquippedWeapon?.Name ?? "None"}  World:({player.X},{player.Y})   ");

            frameBuffer.Flush();

            Thread.Sleep(FrameDelayMs);
        }

        // Post dead state so other clients see us gone
        if (sync is not null && player.Health <= 0)
            sync.PostStateAsync(player.X, player.Y, 0).GetAwaiter().GetResult();

        Console.CursorVisible = true;
        Console.Clear();
        Console.WriteLine("Thanks for playing!");
    }

    private static void ReconcileEnemies(
        List<EnemyModel> enemies,
        List<PlayerState> states,
        string selfUsername,
        GameMap map,
        Random rng)
    {
        var otherPlayers = states.Where(s => s.Username != selfUsername).ToList();
        var seenNames = new HashSet<string>(otherPlayers.Count);

        foreach (var state in otherPlayers)
        {
            seenNames.Add(state.Username);
            var existing = enemies.Find(e => e.Name == state.Username);
            if (existing is not null)
            {
                existing.X = state.X;
                existing.Y = state.Y;
                existing.Health = state.Health;
            }
            else if (state.Health > 0)
            {
                enemies.Add(new EnemyModel(state.Username, state.Health, state.X, state.Y));
            }
        }

        // Remove players who are no longer in the session
        enemies.RemoveAll(e => !seenNames.Contains(e.Name));
    }
}

