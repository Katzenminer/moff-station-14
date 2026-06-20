using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using System.Text;
using Content.Shared.Interaction;
using Content.Server._Moffstation.LogProbe;
using Content.Shared._Moffstation.LogProbe;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class LogProbeCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private LabelSystem _label = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PaperSystem _paper = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeAfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    private void AfterInteract(Entity<LogProbeCartridgeComponent> ent, ref CartridgeAfterInteractEvent args)
    {
        // Moffstation - Begin - Split the component to be reusable
        var loader = args.Loader;
        var interact = args.InteractEvent;
        if (interact.Handled || !interact.CanReach || interact.Target is not { } target)
            return;

        if (HandleChitterScan(ent, interact, target, () => UpdateUiState(ent, loader)))
            return;

        AfterInteract((ent.Owner, (BaseLogProbeComponent)ent.Comp), interact, loader, () => UpdateUiState(ent, loader));
        // Moffstation - End
    }

    private void AfterInteract(Entity<BaseLogProbeComponent> ent, AfterInteractEvent args, EntityUid loader, Action updateState) // Moffstation - Split the component to be reusable
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp(target, out AccessReaderComponent? accessReaderComponent))
            return;

        _audio.PlayEntity(ent.Comp.SoundScan, args.User, target);
        _popup.PopupCursor(Loc.GetString("log-probe-scan", ("device", target)), args.User);

        ent.Comp.EntityName = Name(target);
        ent.Comp.PulledAccessLogs.Clear();

        foreach (var accessRecord in accessReaderComponent.AccessLog)
        {
            var log = new PulledAccessLog(
                accessRecord.AccessTime,
                accessRecord.Accessor
            );

            ent.Comp.PulledAccessLogs.Add(log);
        }

        ent.Comp.PulledAccessLogs.Reverse();

        updateState();
    }

    private void OnUiReady(Entity<LogProbeCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(ent, args.Loader);
    }

    private void OnMessage(Entity<LogProbeCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (HandleChitterPrint(ent, args))
            return;

        if (args is LogProbePrintMessage cast)
            PrintLogs(ent, cast.User);
    }

    private void PrintLogs(Entity<LogProbeCartridgeComponent> ent, EntityUid user)
    {
        if (string.IsNullOrEmpty(ent.Comp.EntityName))
            return;

        if (_timing.CurTime < ent.Comp.NextPrintAllowed)
            return;

        ent.Comp.NextPrintAllowed = _timing.CurTime + ent.Comp.PrintCooldown;

        var paper = Spawn(ent.Comp.PaperPrototype, _transform.GetMapCoordinates(user));
        _label.Label(paper, ent.Comp.EntityName);

        _audio.PlayEntity(ent.Comp.PrintSound, user, paper);
        _hands.PickupOrDrop(user, paper, checkActionBlocker: false);

        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("log-probe-printout-device", ("name", ent.Comp.EntityName)));
        builder.AppendLine(Loc.GetString("log-probe-printout-header"));
        var number = 1;
        foreach (var log in ent.Comp.PulledAccessLogs)
        {
            var time = TimeSpan.FromSeconds(Math.Truncate(log.Time.TotalSeconds)).ToString();
            builder.AppendLine(Loc.GetString("log-probe-printout-entry", ("number", number), ("time", time), ("accessor", log.Accessor)));
            number++;
        }

        var paperComp = Comp<PaperComponent>(paper);
        _paper.SetContent((paper, paperComp), builder.ToString());

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user):user} printed out LogProbe logs ({paper}) of {ent.Comp.EntityName}");
    }

    public void UpdateUiState(Entity<LogProbeCartridgeComponent> ent, EntityUid loaderUid)
    {
        var state = new LogProbeUiState(ent.Comp.EntityName, ent.Comp.PulledAccessLogs, ent.Comp.ChitterData);
        _cartridge.UpdateCartridgeUiState(loaderUid, state);
    }

}
