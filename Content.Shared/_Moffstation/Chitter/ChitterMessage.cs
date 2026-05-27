using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

[Serializable, NetSerializable]
public sealed class ChitterAccount
{
    public uint AccountId;
    public string Name = string.Empty;
    public string JobTitle = string.Empty;
    public string ProfilePictureId = string.Empty;
}

[Serializable, NetSerializable]
public sealed class ChitterMessage
{
    public Guid MessageId = Guid.NewGuid();
    public uint SenderAccountId;
    public string SenderName = string.Empty;
    public TimeSpan Timestamp;
    public string Content = string.Empty;
    public bool DeliveryFailed;
}

[Serializable, NetSerializable]
public sealed class ChitterChat
{
    public Guid ChatId = Guid.NewGuid();
    public List<uint> ParticipantAccountIds = new();
    public List<ChitterMessage> Messages = new();
    public TimeSpan CreatedTime;
    public bool Archived;
}
