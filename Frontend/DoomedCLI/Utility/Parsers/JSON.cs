using System.Text.Json;
using System.Text.Json.Nodes;

namespace DoomedCLI.Utility.Parsers;

public sealed class JSONHandler{
    public static string FormatJSONToString(string json)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);
            var node = JsonNode.Parse(json) ?? throw new JsonException("Invalid JSON: empty or whitespace");

            return node.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            });

        }
        catch (Exception)
        {
            throw;
        }
    }

    public static JsonNode FormatStringToJSONObject(string json)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);
            var node = JsonNode.Parse(json) ?? throw new JsonException("Invalid JSON: empty or whitespace");
            return node;
        }
        catch (Exception)
        {
            throw;
        }

    }
}
