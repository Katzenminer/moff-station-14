using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class ChitterArchivePrintMessage : CartridgeMessageEvent
{
    public Guid ChatId;
}
