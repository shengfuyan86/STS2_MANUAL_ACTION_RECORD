using System;
using System.Collections.Generic;

namespace STS2ManualActionRecorder.Recording;

internal static class RecorderRuntime
{
    private static RecorderSession? session;

    public static void Initialize(RecorderSession recorderSession)
    {
        session = recorderSession;
    }

    public static void Record(string eventType, string source, string confidence, Dictionary<string, object?>? payload = null)
    {
        session?.Record(eventType, source, confidence, payload);
    }

    public static void RecordError(Exception exception)
    {
        session?.RecordError(exception);
    }
}
