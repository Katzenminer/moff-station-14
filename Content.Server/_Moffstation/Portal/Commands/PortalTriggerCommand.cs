using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Console;

namespace Content.Server._Moffstation.Portal.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class PortalTriggerCommand : LocalizedEntityCommands
{
    public override string Command => "portaltrigger";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Loc.GetString("cmd-portaltrigger-missing-entity"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity)
            || !EntityManager.TryGetEntity(netEntity, out var uid))
        {
            shell.WriteLine(Loc.GetString("cmd-portaltrigger-invalid-entity", ("entity", args[0])));
            return;
        }

        if (!EntityManager.HasComponent<SpawnEntityTableOnTriggerComponent>(uid.Value))
        {
            shell.WriteLine(Loc.GetString("cmd-portaltrigger-no-component"));
            return;
        }

        var triggerSystem = EntityManager.EntitySysManager.GetEntitySystem<TriggerSystem>();
        var result = triggerSystem.Trigger(uid.Value, predicted: false);

        if (result)
            shell.WriteLine(Loc.GetString("cmd-portaltrigger-success"));
        else
            shell.WriteLine(Loc.GetString("cmd-portaltrigger-no-trigger"));
    }
}
