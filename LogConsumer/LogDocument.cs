using System.Text.Json;

namespace LogConsumer;

public class LogDocument
{
    public DateTimeOffset Timestamp { get; set; }

    public string Level { get; set; } = string.Empty;

    public string MessageTemplate { get; set; } = string.Empty;

    public string RenderedMessage { get; set; } = string.Empty;

    public string? Exception { get; set; }

    public Dictionary<string, JsonElement> Properties { get; set; } = [];
}