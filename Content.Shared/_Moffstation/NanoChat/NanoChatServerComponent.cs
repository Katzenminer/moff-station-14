using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.NanoChat;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedNanoChatServerSystem))]
public sealed partial class NanoChatServerComponent : Component
{
    /// <summary>
    ///     All chat recipients stored on the server, keyed by card number.
    ///     [CardNumber][Recipientnumber] => specific recipient
    ///     [CardNumber] => all recipients for this card
    /// </summary>
    [DataField]
    public Dictionary<uint, Dictionary<uint, NanoChatRecipient>> AllRecipients = new();

    /// <summary>
    ///     All messages stored on the server, keyed by card number and the respectiv card recipients.
    ///
    ///     [CardNumber][Recipientnumber] => all messages for specific Recipient
    ///     [CardNumber] => All messages for this card
    /// </summary>
    [DataField]
    public Dictionary<uint, Dictionary<uint, List<NanoChatMessage>>> AllMessages = new();

    ///<summary>
    ///Wether this server is the main server, The main Server holds all masseges and such,
    /// and passes these to another if its destroyed
    /// </summary>
    [DataField]
    public bool IsMainServer;
}
