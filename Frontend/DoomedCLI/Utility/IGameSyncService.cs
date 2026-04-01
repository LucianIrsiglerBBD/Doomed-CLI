namespace DoomedCLI.Utility;

public interface IGameSyncService
{
    Task PostStateAsync(int x, int y, int health, CancellationToken ct = default);
    Task<List<PlayerState>> GetAllStatesAsync(CancellationToken ct = default);
    Task PostHitAsync(string shooter, string target, string weaponName, int damage, CancellationToken ct = default);
}
