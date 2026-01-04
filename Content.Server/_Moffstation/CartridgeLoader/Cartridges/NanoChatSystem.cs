using Content.Server.NameIdentifier;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared.NameIdentifier;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

public sealed class NanoChatSystem : SharedNanoChatCardSystem
{

    [Dependency] private readonly NameIdentifierSystem _name = default!;

    private readonly ProtoId<NameIdentifierGroupPrototype> _nameIdentifierGroup = "NanoChat";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NanoChatCardComponent, ComponentStartup>(OnStartup);
    }

    public void OnStartup(Entity<NanoChatCardComponent>  ent, ComponentStartup args)
    {
        _name.GenerateUniqueName(ent.Owner,_nameIdentifierGroup, out var number);
        number = new NanoChatNumber(number);
        ent.Comp.Number = number; // assign a new and uniquie number to the enttity
    }
}
