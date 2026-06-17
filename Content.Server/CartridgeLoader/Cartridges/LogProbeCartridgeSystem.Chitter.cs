using System.Linq;
using System.Text;
using Content.Shared._Moffstation.BladeServer;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared._Moffstation.Chitter;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.Paper;
using Robust.Shared.Timing;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class LogProbeCartridgeSystem
{
    private const int ChitterPrintCharLimit = 10000;

    private void InitializeChitter()
    {
        SubscribeLocalEvent<LogProbeCartridgeComponent, ChitterArchivePrintMessage>(OnChitterPrintMessage);
    }

    private bool HandleChitterScan(Entity<LogProbeCartridgeComponent> ent, AfterInteractEvent args, EntityUid target, Action updateState)
    {
        if (TryComp<ChitterServerComponent>(target, out var chitter))
        {
            DoChitterScan(ent, args, target, chitter, updateState);
            return true;
        }

        if (TryComp<BladeServerRackComponent>(target, out var rack))
        {
            foreach (var slot in rack.BladeSlots)
            {
                if (slot.Item is { } blade && TryComp(blade, out chitter))
                {
                    DoChitterScan(ent, args, blade, chitter, updateState);
                    return true;
                }
            }
        }

        return false;
    }

    private void DoChitterScan(Entity<LogProbeCartridgeComponent> ent, AfterInteractEvent args, EntityUid serverUid, ChitterServerComponent server, Action updateState)
    {
        _audio.PlayEntity(ent.Comp.SoundScan, args.User, serverUid);
        _popup.PopupCursor(Loc.GetString("log-probe-scan", ("device", serverUid)), args.User);

        ent.Comp.EntityName = Name(serverUid);
        ent.Comp.PulledAccessLogs.Clear();
        ent.Comp.ChitterData = new ChitterServerScanData { IsServerScan = true };

        foreach (var (chatId, chat) in server.ArchivedChats)
        {
            var participantNames = string.Join(", ",
                chat.ParticipantAccountIds
                    .Select(id => server.Accounts.GetValueOrDefault(id)?.Name ?? $"#{id:D4}"));

            var lastMsg = chat.Messages.Count > 0 ? chat.Messages[^1].Content : "";

            ent.Comp.ChitterData.ArchivedChats.Add(new ArchivedChatEntry
            {
                ChatId = chatId,
                Participants = participantNames,
                MessageCount = chat.Messages.Count,
                LastMessagePreview = lastMsg.Length > 50 ? lastMsg[..50] + "..." : lastMsg,
                Messages = chat.Messages.Select(m => new ArchivedMessage
                {
                    SenderName = m.SenderName,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                }).ToList(),
            });
        }

        updateState();
    }

    private bool HandleChitterPrint(Entity<LogProbeCartridgeComponent> ent, CartridgeMessageEvent args)
    {
        if (args is not ChitterArchivePrintMessage printMsg)
            return false;

        if (ent.Comp.ChitterData == null)
        {
            _popup.PopupCursor(Loc.GetString("chitter-logprobe-no-data"), args.User);
            return true;
        }

        var chat = ent.Comp.ChitterData.ArchivedChats.FirstOrDefault(c => c.ChatId == printMsg.ChatId);
        if (chat == null)
        {
            _popup.PopupCursor(Loc.GetString("chitter-logprobe-no-archives"), args.User);
            return true;
        }

        PrintChitterArchive(ent, printMsg.User, chat);
        return true;
    }

    private void OnChitterPrintMessage(Entity<LogProbeCartridgeComponent> ent, ref ChitterArchivePrintMessage args)
    {
        if (ent.Comp.ChitterData == null)
            return;

        var targetChatId = args.ChatId;
        var chat = ent.Comp.ChitterData.ArchivedChats.FirstOrDefault(c => c.ChatId == targetChatId);
        if (chat == null)
            return;

        PrintChitterArchive(ent, args.User, chat);
    }

    private void PrintChitterArchive(Entity<LogProbeCartridgeComponent> ent, EntityUid user, ArchivedChatEntry chat)
    {
        if (_timing.CurTime < ent.Comp.NextPrintAllowed)
            return;

        ent.Comp.NextPrintAllowed = _timing.CurTime + ent.Comp.PrintCooldown;

        var paper = Spawn(ent.Comp.PaperPrototype, _transform.GetMapCoordinates(user));
        _label.Label(paper, ent.Comp.EntityName);

        _audio.PlayEntity(ent.Comp.PrintSound, user, paper);
        _hands.PickupOrDrop(user, paper, checkActionBlocker: false);

        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("chitter-printout-header", ("server", ent.Comp.EntityName)));
        builder.AppendLine(Loc.GetString("chitter-printout-chat", ("participants", chat.Participants)));
        builder.AppendLine();

        foreach (var msg in chat.Messages)
        {
            var time = TimeSpan.FromSeconds(Math.Truncate(msg.Timestamp.TotalSeconds)).ToString();
            builder.AppendLine(Loc.GetString("chitter-printout-message",
                ("sender", msg.SenderName),
                ("time", time),
                ("content", msg.Content)));
        }

        var content = builder.ToString();

        if (content.Length > ChitterPrintCharLimit)
        {
            content = content[..ChitterPrintCharLimit] + "\n" +
                      Loc.GetString("chitter-printout-overheat");
        }

        var paperComp = Comp<PaperComponent>(paper);
        _paper.SetContent((paper, paperComp), content);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low,
            $"{ToPrettyString(user):user} printed Chitter archive from {ent.Comp.EntityName}");
    }
}
