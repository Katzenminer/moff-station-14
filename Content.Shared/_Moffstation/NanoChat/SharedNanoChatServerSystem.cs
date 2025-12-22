using Content.Shared._Moffstation.CartridgeLoader.Cartridges;

namespace Content.Shared._Moffstation.NanoChat;

public sealed class SharedNanoChatServerSystem : EntitySystem
{
    /// <summary>
    /// Attemts to get the Main Server
    /// </summary>
    /// <returns>Returns Null if no server is declared as the main server or no servers exist</returns>
    private Entity<NanoChatServerComponent>? TryGetServer()
    {
        var servers = EntityQuery<NanoChatServerComponent>();
        var test = EntityQueryEnumerator<NanoChatServerComponent>();
        while (test.MoveNext(out var uid, out var comp))
        {
            if (comp.IsMainServer)
            {
                return new Entity<NanoChatServerComponent>(uid, comp);
            }
        }

        return null;
    }

    public IReadOnlyDictionary<uint, List<NanoChatMessage>>? TryGetMessages(Entity<NanoChatCardComponent?> card)
    {
        var server = TryGetServer();
        return card.Comp != null && server != null && card.Comp.Number.HasValue
            ? server.Value.Comp.AllMessages[card.Comp.Number.Value]
            : null;
    }

    /// <summary>
    ///     Gets the recipients dictionary for a card.
    /// </summary>
    public IReadOnlyDictionary<uint, NanoChatRecipient>? TryGetRecipients(Entity<NanoChatCardComponent?> card)
    {
        var server = TryGetServer();
        return server != null && card.Comp is { Number: not null }
            ? server.Value.Comp.AllRecipients[card.Comp.Number.Value]
            : null;
    }

    /// <summary>
    ///     Gets the time of the last message for a card.
    /// </summary>
    public TimeSpan? GetLastMessageTime(Entity<NanoChatCardComponent?> card)
    {
        return card.Comp?.LastMessageTime;
    }

    /// <summary>
    ///     Gets a specific recipient for a card.
    /// </summary>
    public NanoChatRecipient? GetRecipient(Entity<NanoChatCardComponent?> card, uint recipientNumber)
    {
        var server = TryGetServer();
        return server != null && card.Comp is { Number: not null }
            ? server.Value.Comp.AllRecipients[card.Comp.Number.Value][recipientNumber]
            : null;
    }

    /// <summary>
    ///     Gets all messages for a specific recipient of a card.
    /// </summary>
    public List<NanoChatMessage>? TryGetMessagesForRecipient(Entity<NanoChatCardComponent?> card, uint recipientNumber)
    {
        var server = TryGetServer();
        return server != null && card.Comp is { Number: not null }
            ? server.Value.Comp.AllMessages[card.Comp.Number.Value][recipientNumber]
            : null;
    }

    /// <summary>
    ///     Adds a message to a recipient's conversation for a card.
    /// </summary>
    public void AddMessageToServer(Entity<NanoChatCardComponent?> card, uint recipientNumber, NanoChatMessage message)
    {
        var server = TryGetServer();
        if (server == null || card.Comp == null || !card.Comp.Number.HasValue)
            return;
        // If recpient doesnt have an entry in messages for this card add it
        if (!server.Value.Comp.AllMessages[card.Comp.Number.Value].ContainsKey(recipientNumber))
        {
            server.Value.Comp.AllMessages[card.Comp.Number.Value].Add(recipientNumber, new List<NanoChatMessage>());
        }

        server.Value.Comp.AllMessages[card.Comp.Number.Value][recipientNumber].Add(message);
        Dirty(server.Value);
    }

    /// <summary>
    ///     Clears all messages and recipients from the card.
    /// </summary>
    public void ClearCardFromServer(Entity<NanoChatCardComponent?> card)
    {
        var server = TryGetServer();
        if (server == null || card.Comp == null || !card.Comp.Number.HasValue)
            return;
        server.Value.Comp.AllMessages[card.Comp.Number.Value].Clear();
        server.Value.Comp.AllRecipients[card.Comp.Number.Value].Clear();
        Dirty(server.Value);
    }

    /// <summary>
    ///     Deletes a chat conversation with a recipient from the card.
    ///     Optionally keeps message history while removing from active chats.
    /// </summary>
    /// <returns>True if the chat was deleted successfully</returns>
    public bool TryDeleteChat(Entity<NanoChatCardComponent?> card, uint recipientNumber, bool keepMessages = false)
    {
        var server = TryGetServer();
        if (server == null || card.Comp == null || !card.Comp.Number.HasValue)
            return false;
        server.Value.Comp.AllRecipients[card.Comp.Number.Value].Remove(recipientNumber);
        if (!keepMessages)
        {
            server.Value.Comp.AllMessages[card.Comp.Number.Value][recipientNumber].Clear();
        }

        return true;
    }

    /// <summary>
    ///     Ensures a recipient exists in the card's contacts and message lists.
    ///     If the recipient doesn't exist, they will be added with the provided info.
    /// </summary>
    /// <returns>True if the recipient was added or already existed</returns>
    public bool EnsureRecipientExists(Entity<NanoChatCardComponent?> card,
        uint recipientNumber,
        NanoChatRecipient? recipientInfo = null)
    {
        var server = TryGetServer();
        if (server.HasValue && recipientInfo.HasValue && card.Comp != null && card.Comp.Number.HasValue)
        {
            if (!server.Value.Comp.AllRecipients[card.Comp.Number.Value].ContainsKey(recipientNumber))
            {
                server.Value.Comp.AllRecipients[card.Comp.Number.Value].Add(recipientNumber, recipientInfo.Value);
            }

            return true;
        }

        return false;
    }
}
