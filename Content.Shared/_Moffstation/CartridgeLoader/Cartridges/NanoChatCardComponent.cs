using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(SharedNanoChatCardSystem))][NetworkedComponent]
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

public readonly record struct NanoChatNumber(int Value)
{
    //implicit conversion for ease of use
    public static implicit operator NanoChatNumber(int value)
    {
        return new NanoChatNumber(value);
    }

    public static implicit operator int(NanoChatNumber number)
    {
        return number.Value;
    }
};

public readonly record struct NanoChatUser(NanoChatNumber Number, string JobTitel, string Name);
