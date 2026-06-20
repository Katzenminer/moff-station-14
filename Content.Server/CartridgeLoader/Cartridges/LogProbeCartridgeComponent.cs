using Content.Server._Moffstation.LogProbe;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(LogProbeCartridgeSystem))]
[AutoGenerateComponentPause]
public sealed partial class LogProbeCartridgeComponent : BaseLogProbeComponent
{
    [DataField]
    public ChitterServerScanData? ChitterData;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public override TimeSpan NextPrintAllowed { get; set; } = TimeSpan.FromSeconds(0);
}
