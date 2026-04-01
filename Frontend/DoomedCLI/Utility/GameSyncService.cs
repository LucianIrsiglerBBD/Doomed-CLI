using System.Text;
using System.Text.Json;
using DoomedCLI.Utility.HTTP;

namespace DoomedCLI.Utility;

public sealed class GameSyncService : IGameSyncService
{
    private readonly string _username;
    private readonly HttpHandler _http;

    public GameSyncService(string username, string hostIp)
    {
        _username = username;
        _http = new HttpHandler(new HttpClient { BaseAddress = new Uri(hostIp) });
    }

    public async Task PostStateAsync(int x, int y, int health, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            username = _username,
            x,
            y,
            health
        });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        await _http.PostAsync("api/game", content, ct).ConfigureAwait(false);
    }

    public async Task<List<PlayerState>> GetAllStatesAsync(CancellationToken ct = default)
    {
        var json = await _http.GetAsync($"api/game/{Uri.EscapeDataString(_username)}", ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<PlayerState>>(json) ?? new List<PlayerState>();
    }

    public async Task PostHitAsync(string shooter, string target, string weaponName, int damage, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new { shooter, target, weaponName, damage });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        await _http.PostAsync("api/game/hit", content, ct).ConfigureAwait(false);
    }
}
