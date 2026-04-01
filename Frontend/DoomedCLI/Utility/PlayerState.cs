using System.Text.Json.Serialization;

namespace DoomedCLI.Utility;

public sealed record PlayerState(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("health")] int Health);
