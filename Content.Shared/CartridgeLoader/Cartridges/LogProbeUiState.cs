using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class LogProbeUiState : BoundUserInterfaceState
{
    public string EntityName;
    public List<PulledAccessLog> PulledLogs;
    public ChitterServerScanData? ChitterData;

    public LogProbeUiState(string entityName, List<PulledAccessLog> pulledLogs, ChitterServerScanData? chitterData = null)
    {
        EntityName = entityName;
        PulledLogs = pulledLogs;
        ChitterData = chitterData;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed partial class PulledAccessLog
{
    public readonly TimeSpan Time;
    public readonly string Accessor;

    public PulledAccessLog(TimeSpan time, string accessor)
    {
        Time = time;
        Accessor = accessor;
    }
}

[Serializable, NetSerializable]
public sealed class ChitterServerScanData
{
    public bool IsServerScan;
    public List<ArchivedChatEntry> ArchivedChats = new();
}

[Serializable, NetSerializable]
public sealed class ArchivedChatEntry
{
    public Guid ChatId;
    public string Participants = string.Empty;
    public int MessageCount;
    public string LastMessagePreview = string.Empty;
    public List<ArchivedMessage> Messages = new();
}

[Serializable, NetSerializable]
public sealed class ArchivedMessage
{
    public string SenderName = string.Empty;
    public string Content = string.Empty;
    public TimeSpan Timestamp;
}
