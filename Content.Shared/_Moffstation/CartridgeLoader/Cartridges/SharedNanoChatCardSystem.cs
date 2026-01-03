namespace Content.Shared._Moffstation.CartridgeLoader.Cartridges;

public sealed class SharedNanoChatCardSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NanoChatCardComponent, ComponentStartup>(OnStartup);
    }

    public void OnStartup(NanoChatCardComponent  component, ComponentStartup args)
    {
        component.Number
    }
}
