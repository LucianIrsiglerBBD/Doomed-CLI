namespace DoomedCLI.Utility;

/// <summary>
/// Drop-in sync service that simulates two fake players wandering the map.
/// Bots are real PlayerModel instances so they obey wall collision natively.
/// No network calls — safe to use without any backend running.
/// </summary>
internal sealed class FakeGameSyncService : IGameSyncService
{
    private static readonly (int dx, int dy)[] Dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];
    private const int TurnEvery = 20;

    private sealed class BotPlayer
    {
        public readonly PlayerModel Model;
        private int _dx, _dy;
        private int _stepCount;
        private readonly Random _rng;

        public BotPlayer(string name, int x, int y, Random rng)
        {
            Model = new PlayerModel(name, 100, x, y);
            _rng = rng;
            PickNewDir();
        }

        public void Wander(GameMap map)
        {
            Model.Move(_dx, _dy, map);

            if (++_stepCount >= TurnEvery)
            {
                _stepCount = 0;
                PickNewDir();
            }
        }

        private void PickNewDir()
        {
            var d = Dirs[_rng.Next(Dirs.Length)];
            (_dx, _dy) = d;
        }

        public PlayerState ToState() =>
            new(Model.Name, Model.X, Model.Y, Model.Health);
    }

    private readonly string _selfUsername;
    private readonly GameMap _map;
    private readonly BotPlayer[] _bots;
    private readonly Random _rng = new();
    private int _tickCount;

    public FakeGameSyncService(string selfUsername, GameMap map, int startX, int startY)
    {
        _selfUsername = selfUsername;
        _map = map;

        var rng = new Random();
        // Spawn each bot at a valid map position near the player start
        map.TryFindSpawnPosition(5, 3, rng, out int ax, out int ay);
        map.TryFindSpawnPosition(5, 3, rng, out int bx, out int by);

        _bots =
        [
            new BotPlayer("Bot_Alpha", ax, ay, rng),
            new BotPlayer("Bot_Beta",  bx, by, rng),
        ];
    }

    public Task PostStateAsync(int x, int y, int health, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<List<PlayerState>> GetAllStatesAsync(CancellationToken ct = default)
    {
        // Move bots every other poll so they don't sprint across the screen
        if (_tickCount++ % 2 == 0)
            foreach (var bot in _bots) bot.Wander(_map);

        var states = new List<PlayerState>(_bots.Length + 1);
        // Include self so the health reconciliation path in GameRunner works normally
        states.Add(new PlayerState(_selfUsername, 0, 0, 100));
        foreach (var bot in _bots) states.Add(bot.ToState());
        return Task.FromResult(states);
    }

    public Task PostHitAsync(string shooter, string target, string weaponName, int damage, CancellationToken ct = default)
    {
        foreach (var bot in _bots)
        {
            if (bot.Model.Name == target)
            {
                bot.Model.Health = Math.Max(0, bot.Model.Health - damage);
                break;
            }
        }
        return Task.CompletedTask;
    }
}
