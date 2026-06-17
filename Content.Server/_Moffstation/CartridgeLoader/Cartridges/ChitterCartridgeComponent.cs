namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(ChitterCartridgeSystem))]
public sealed partial class ChitterCartridgeComponent : Component
{
    public Guid? CurrentChatId;
    public readonly Dictionary<Guid, int> LastSeenMessageCount = new();
}
