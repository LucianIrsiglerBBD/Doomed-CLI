namespace DoomedCLI;

class EnemyModel : BaseEntity
{
    private const int DefaultSpawnWidth = 5;
    private const int DefaultSpawnHeight = 3;

    public EnemyModel(string name, int health, int x, int y) : base(name, health, x, y, "base_enemy.txt")
    {
    }

    public static List<EnemyModel> SpawnGroup(int count, GameMap map, Random rng)
    {
        var enemies = new List<EnemyModel>(count);
        for (int i = 0; i < count; i++)
        {
            map.TryFindSpawnPosition(DefaultSpawnWidth, DefaultSpawnHeight, rng, out int spawnX, out int spawnY);
            enemies.Add(new EnemyModel($"Enemy{i + 1}", 50, spawnX, spawnY));
        }
        return enemies;
    }

    public void Update()
    {
        //Should be enemy movement fr, don't worry about it for now
    }
}