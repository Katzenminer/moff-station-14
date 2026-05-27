namespace Content.Shared._Moffstation.Chitter;

[RegisterComponent, Access(typeof(SharedChitterSystem))]
public sealed partial class ChitterServerComponent : Component
{
    public readonly Dictionary<uint, ChitterAccount> Accounts = new();
    public readonly Dictionary<Guid, ChitterChat> Chats = new();
    public readonly Dictionary<Guid, ChitterChat> ArchivedChats = new();
}
