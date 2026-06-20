using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.LogProbe;

public abstract partial class BaseLogProbeComponent : Component
{
    [DataField]
    public string EntityName = string.Empty;

    [DataField, ViewVariables]
    public List<PulledAccessLog> PulledAccessLogs = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SoundScan = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg", AudioParams.Default.WithVariation(0.25f));

    [DataField]
    public EntProtoId<PaperComponent> PaperPrototype = "PaperAccessLogs";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    public abstract TimeSpan NextPrintAllowed { get; set; }
}
