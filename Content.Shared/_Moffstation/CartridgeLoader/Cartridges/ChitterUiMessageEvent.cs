using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public enum ChitterUiMessageType
{
    NewChat,
    SelectChat,
    SendMessage,
    LeaveChat,
    AddParticipant,
    RemoveParticipant,
    ArchiveChat,
    SetProfilePicture,
    RefreshContacts,
}

[Serializable, NetSerializable]
public sealed class ChitterUiMessageEvent : CartridgeMessageEvent
{
    public ChitterUiMessageType Type;
    public Guid? ChatId;
    public uint? TargetNumber;
    public List<uint>? TargetNumbers;
    public string? Content;
    public string? ProfilePictureId;
}
