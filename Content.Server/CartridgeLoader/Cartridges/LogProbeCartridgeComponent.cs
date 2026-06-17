using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(LogProbeCartridgeSystem))]
[AutoGenerateComponentPause]
public sealed partial class LogProbeCartridgeComponent : Component
{
    [DataField]
    public string EntityName = string.Empty;

    [DataField, ViewVariables]
    public List<PulledAccessLog> PulledAccessLogs = new();

    [DataField]
    public ChitterServerScanData? ChitterData;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SoundScan = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg", AudioParams.Default.WithVariation(0.25f));

    [DataField]
    public EntProtoId<PaperComponent> PaperPrototype = "PaperAccessLogs";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    // Moffstation - Begin - Split the component to be reusable

    /// <summary>
    /// When anyone is allowed to spawn another printout.
    /// </summary>
    /// <remarks>
    /// This is abstract as it need to be implemented concretely on component
    /// to put the auto pause attribute.
    /// </remarks>
    public abstract TimeSpan NextPrintAllowed { get; set; }

    // Moffstation - End

}
