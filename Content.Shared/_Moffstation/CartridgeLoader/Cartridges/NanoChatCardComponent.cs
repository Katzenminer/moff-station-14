namespace Content.Shared._Moffstation.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(SharedNanoChatServerSystem))]
public sealed partial class NanoChatCardComponent : Component
{
    /// <summary>
    /// This Cards NanoChat Number
    /// </summary>
    [DataField]
    public NanoChatNumber Number;

    /// <summary>
    /// All Chats this Card is participating in (Updated by the server)
    /// </summary>
    [DataField]
    public List<ChatId> Chats;

    /// <summary>
    /// This Cards User Profile
    /// </summary>
    [DataField]
    public NanoChatUser User;
}

public readonly record struct NanoChatNumber(int Value);

public readonly record struct NanoChatUser(NanoChatNumber Number, string JobTitel, string Name);
