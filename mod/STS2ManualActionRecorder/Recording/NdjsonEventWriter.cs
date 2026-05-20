using System.Text.Json;

namespace STS2ManualActionRecorder.Recording;

internal sealed class NdjsonEventWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    private readonly object sync = new();
    private readonly string eventsPath;
    private readonly string errorsPath;
    private bool disabled;

    public NdjsonEventWriter(string runDirectory)
    {
        Directory.CreateDirectory(runDirectory);
        eventsPath = Path.Combine(runDirectory, "events.ndjson");
        errorsPath = Path.Combine(runDirectory, "errors.log");
    }

    public string EventsPath => eventsPath;

    public void Write(RecorderEvent recorderEvent)
    {
        if (disabled)
        {
            return;
        }

        try
        {
            string line = JsonSerializer.Serialize(recorderEvent, JsonOptions);
            lock (sync)
            {
                File.AppendAllText(eventsPath, line + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            disabled = true;
            TryWriteError(ex);
        }
    }

    public void TryWriteError(Exception exception)
    {
        try
        {
            lock (sync)
            {
                File.AppendAllText(errorsPath, $"{DateTimeOffset.UtcNow:O} {exception}\n");
            }
        }
        catch
        {
            disabled = true;
        }
    }
}
