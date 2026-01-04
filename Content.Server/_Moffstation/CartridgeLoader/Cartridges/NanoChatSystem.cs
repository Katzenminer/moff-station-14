using Content.Server._Moffstation.Access.Systems;
using Content.Server.NameIdentifier;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared.Access.Components;
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
        SubscribeLocalEvent<IdCardComponent, IdCardChangedEvent>(OnIdUpdate);
    }

    private void OnStartup(Entity<NanoChatCardComponent> ent, ref ComponentStartup args)
    {
        _name.GenerateUniqueName(ent.Owner, _nameIdentifierGroup, out var number);
        number = new NanoChatNumber(number);
        ent.Comp.Number = number; // assign a unique number to the entity.
        ent.Comp.User = FillOutNanoChatUser(ent);
        Dirty(ent);
    }

    private NanoChatUser FillOutNanoChatUser(Entity<NanoChatCardComponent> ent, IdCardComponent? idcard = null)
    {
        if (idcard == null)
        {
            if (!HasComp<IdCardComponent>(ent))
            {
                return new NanoChatUser(ent.Comp.Number, "Unknown", "Unknown"); // Incase there is no IdCardComponent
            }

            idcard = Comp<IdCardComponent>(ent);
        }

        return new NanoChatUser(ent.Comp.Number,
            idcard.FullName ?? "Unknown",
            idcard.JobTitle ?? "Unknown"); //Uses unknown if there is no name or job
    }
    private void OnIdUpdate(Entity<IdCardComponent> ent, ref IdCardChangedEvent args)
    {
        var nanoChatComp = new Entity<NanoChatCardComponent>(ent.Owner,Comp<NanoChatCardComponent>(ent.Owner));
        nanoChatComp.Comp.User = FillOutNanoChatUser(nanoChatComp, ent);
        Dirty(nanoChatComp);
    }
}
