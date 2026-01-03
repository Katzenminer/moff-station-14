namespace Content.Shared._Moffstation.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(SharedNanoChatServerSystem))]
public sealed partial class NanoChatServerComponent : Component
{
    /// <summary>
    /// A list of All Chats saved on the server
    /// </summary>
    [DataField]
    public Dictionary<ChatId, List<NanochatMessage>> Chats = new();

    /// <summary>
    /// A List that is Updated whenever a new user is added or removed, to prevent querying all users etc...
    /// </summary>
    [DataField]
    public List<NanoChatUser> Users;
}

public readonly record struct ChatId(int Value);

public readonly record struct NanochatMessage(
    TimeSpan MessageTime,
    string MessageContent,
    ChatId ChatId,
    NanoChatNumber SenderNumber);
