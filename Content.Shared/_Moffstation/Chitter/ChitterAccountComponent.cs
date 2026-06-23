using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Chitter;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChitterAccountComponent : Component
{
    [DataField, AutoNetworkedField]
    public uint AccountId;

    [DataField, AutoNetworkedField]
    public string ProfilePictureId = string.Empty;
}
