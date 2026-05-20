using System.Text.Json;

namespace STS2ManualActionRecorder.Recording;

internal sealed class RecorderSession
{
    private const string SchemaVersion = "0.1.0";
    private const string RecorderVersion = "0.1.0";
    private const string GameVersion = "v0.105.1";
    private const int MainAssemblyHash = 1363691567;

    private readonly NdjsonEventWriter writer;
    private long sequence;

    private RecorderSession(string sessionId, string runDirectory, NdjsonEventWriter writer)
    {
        SessionId = sessionId;
        RunDirectory = runDirectory;
        this.writer = writer;
    }

    public string SessionId { get; }

    public string RunDirectory { get; }

    public static RecorderSession Create()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string sessionId = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N")[..8];
        string runDirectory = Path.Combine(root, "STS2ManualActionRecorder", "runs", sessionId);
        var writer = new NdjsonEventWriter(runDirectory);
        var session = new RecorderSession(sessionId, runDirectory, writer);
        session.WriteMetadata();
        return session;
    }

    public void Record(string eventType, string source, string confidence, Dictionary<string, object?>? payload = null)
    {
        var recorderEvent = new RecorderEvent
        {
            SchemaVersion = SchemaVersion,
            RecorderVersion = RecorderVersion,
            GameVersion = GameVersion,
            MainAssemblyHash = MainAssemblyHash,
            SessionId = SessionId,
            RunId = null,
            Seq = Interlocked.Increment(ref sequence),
            TimeUtc = DateTimeOffset.UtcNow.ToString("O"),
            EventType = eventType,
            Source = source,
            Confidence = confidence,
            Payload = payload ?? new Dictionary<string, object?>()
        };
        writer.Write(recorderEvent);
    }

    public void RecordError(Exception exception)
    {
        writer.TryWriteError(exception);
    }

    private void WriteMetadata()
    {
        var metadata = new Dictionary<string, object?>
        {
            ["schema_version"] = SchemaVersion,
            ["recorder_version"] = RecorderVersion,
            ["game_version"] = GameVersion,
            ["main_assembly_hash"] = MainAssemblyHash,
            ["session_id"] = SessionId,
            ["started_at_utc"] = DateTimeOffset.UtcNow.ToString("O")
        };
        string path = Path.Combine(RunDirectory, "metadata.json");
        File.WriteAllText(path, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
    }
}
