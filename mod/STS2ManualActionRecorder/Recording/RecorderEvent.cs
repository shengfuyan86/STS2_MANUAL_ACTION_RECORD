using System.Text.Json.Serialization;

namespace STS2ManualActionRecorder.Recording;

internal sealed record RecorderEvent
{
    [JsonPropertyName("schema_version")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("recorder_version")]
    public required string RecorderVersion { get; init; }

    [JsonPropertyName("game_version")]
    public required string GameVersion { get; init; }

    [JsonPropertyName("main_assembly_hash")]
    public required int MainAssemblyHash { get; init; }

    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }

    [JsonPropertyName("run_id")]
    public string? RunId { get; init; }

    [JsonPropertyName("seq")]
    public required long Seq { get; init; }

    [JsonPropertyName("time_utc")]
    public required string TimeUtc { get; init; }

    [JsonPropertyName("event_type")]
    public required string EventType { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("confidence")]
    public required string Confidence { get; init; }

    [JsonPropertyName("payload")]
    public required Dictionary<string, object?> Payload { get; init; }
}
