using System.Linq;
using Content.Server._Moffstation.Chitter;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared._Moffstation.Chitter;
using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.IdentityManagement;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

public sealed class ChitterCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private ChitterServerSystem _server = default!;
    [Dependency] private IGameTiming _timing = default!;

    private TimeSpan _nextRefresh = TimeSpan.Zero;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(3);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChitterCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<ChitterCartridgeComponent, CartridgeMessageEvent>(OnMessage);
        SubscribeLocalEvent<ChitterCartridgeComponent, CartridgeAddedEvent>(OnAdded);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextRefresh)
            return;

        _nextRefresh = _timing.CurTime + RefreshInterval;

        var query = EntityQueryEnumerator<CartridgeLoaderComponent>();
        while (query.MoveNext(out var loaderUid, out var loader))
        {
            if (loader.ActiveProgram == null)
                continue;

            if (!TryComp<ChitterCartridgeComponent>(loader.ActiveProgram.Value, out var cartridge))
                continue;

            UpdateUi((loader.ActiveProgram.Value, cartridge), loaderUid);
        }
    }

    private void OnAdded(Entity<ChitterCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
    }

    private void OnUiReady(Entity<ChitterCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUi(ent, args.Loader);
    }

    private void OnMessage(Entity<ChitterCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not ChitterUiMessageEvent msg)
            return;

        var loader = GetEntity(args.LoaderUid);

        switch (msg.Type)
        {
            case ChitterUiMessageType.NewChat:
                HandleNewChat(ent, loader, msg);
                break;
            case ChitterUiMessageType.SelectChat:
                break;
            case ChitterUiMessageType.SendMessage:
                HandleSendMessage(ent, loader, msg);
                break;
            case ChitterUiMessageType.LeaveChat:
                HandleLeaveChat(ent, loader, msg);
                break;
            case ChitterUiMessageType.AddParticipant:
                HandleAddParticipant(ent, loader, msg);
                break;
            case ChitterUiMessageType.RemoveParticipant:
                HandleRemoveParticipant(ent, loader, msg);
                break;
            case ChitterUiMessageType.ArchiveChat:
                HandleArchiveChat(ent, loader, msg);
                break;
            case ChitterUiMessageType.SetProfilePicture:
                HandleSetProfilePicture(ent, loader, msg);
                break;
            case ChitterUiMessageType.RefreshContacts:
                break;
        }

        UpdateUi(ent, loader);
    }

    private bool TryGetServerAndCard(EntityUid loader, out Entity<ChitterServerComponent> serverEnt, out ChitterAccountComponent card)
    {
        serverEnt = default;
        card = default!;

        if (!_server.TryFindServer(loader, out serverEnt))
            return false;

        if (!_server.TryGetPdaIdCard(loader, out var idCard))
            return false;

        if (!TryComp<ChitterAccountComponent>(idCard, out var foundCard))
            return false;
        card = foundCard;
        return true;
    }

    private void HandleNewChat(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        List<uint> participants;
        if (msg.TargetNumbers != null && msg.TargetNumbers.Count > 0)
        {
            participants = new List<uint> { card.AccountId };
            foreach (var target in msg.TargetNumbers)
            {
                if (target != card.AccountId && !participants.Contains(target))
                    participants.Add(target);
            }
        }
        else if (msg.TargetNumber != null && msg.TargetNumber != card.AccountId)
        {
            participants = new List<uint> { card.AccountId, msg.TargetNumber.Value };
        }
        else
        {
            return;
        }

        _server.CreateChat(serverEnt.Comp, participants);
    }

    private void HandleSendMessage(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        if (msg.ChatId == null || string.IsNullOrWhiteSpace(msg.Content))
            return;

        var senderName = Identity.Name(loader, EntityManager);
        _server.AddMessage(serverEnt.Comp, msg.ChatId.Value, card.AccountId, senderName, msg.Content);

        if (!_server.IsServerPowered(serverEnt))
            _server.MarkDeliveryFailed(serverEnt.Comp, msg.ChatId.Value);
    }

    private void HandleLeaveChat(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        if (msg.ChatId != null)
            _server.RemoveParticipantFromChat(serverEnt.Comp, msg.ChatId.Value, card.AccountId);
    }

    private void HandleAddParticipant(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        if (msg.ChatId != null && msg.TargetNumber != null)
            _server.AddParticipantToChat(serverEnt.Comp, msg.ChatId.Value, msg.TargetNumber.Value);
    }

    private void HandleRemoveParticipant(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var _))
            return;

        if (msg.ChatId != null && msg.TargetNumber != null)
            _server.RemoveParticipantFromChat(serverEnt.Comp, msg.ChatId.Value, msg.TargetNumber.Value);
    }

    private void HandleArchiveChat(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var _))
            return;

        if (msg.ChatId != null)
            _server.ArchiveChat(serverEnt.Comp, msg.ChatId.Value);
    }

    private void HandleSetProfilePicture(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var _, out var card))
            return;

        if (msg.ProfilePictureId != null)
        {
            card.ProfilePictureId = msg.ProfilePictureId;
            Dirty(loader, card);
        }
    }

    private void UpdateUi(Entity<ChitterCartridgeComponent> ent, EntityUid loader)
    {
        var hasIdCard = _server.TryGetPdaIdCard(loader, out var idCard);
        var serverOnline = _server.TryFindServer(loader, out var serverEnt);

        var state = new ChitterUiState
        {
            HasIdCard = hasIdCard,
            ServerOnline = serverOnline,
        };

        if (hasIdCard && TryComp<ChitterAccountComponent>(idCard, out var account))
        {
            state.OwnNumber = account.AccountId;
            state.OwnProfilePicture = account.ProfilePictureId;

            var identity = Identity.Name(loader, EntityManager);
            state.OwnName = identity;

            Log.Info($"[Chitter] UpdateUi: hasIdCard=true, serverOnline={serverOnline}, ownAccountId={account.AccountId}, ownName={identity}");

            if (serverOnline)
            {
                var serverComp = serverEnt.Comp;
                _server.RegisterOrUpdateAccount(serverComp, account.AccountId, identity, "", account.ProfilePictureId);

                var before = serverComp.Accounts.Count;
                DiscoverAccountsOnGrid(loader, serverComp);
                var after = serverComp.Accounts.Count;

                Log.Info($"[Chitter] UpdateUi: accounts before discovery={before}, after={after}");

                foreach (var (accId, acc) in serverComp.Accounts)
                {
                    if (accId == account.AccountId)
                        continue;
                    state.Contacts.Add(new AccountEntry
                    {
                        AccountId = accId,
                        Name = acc.Name,
                        JobTitle = acc.JobTitle,
                        ProfilePictureId = acc.ProfilePictureId,
                    });
                    Log.Info($"[Chitter] UpdateUi: added contact accId={accId}, name={acc.Name}");
                }

                foreach (var (chatId, chat) in serverComp.Chats)
                {
                    if (!chat.ParticipantAccountIds.Contains(account.AccountId))
                        continue;

                    var lastMsg = chat.Messages.Count > 0 ? chat.Messages[^1].Content : "";
                    var displayName = string.Join(", ",
                        chat.ParticipantAccountIds
                            .Where(id => id != account.AccountId)
                            .Select(id => serverComp.Accounts.GetValueOrDefault(id)?.Name ?? $"#{id:D4}"));

                    state.Chats.Add(new ChatEntry
                    {
                        ChatId = chatId,
                        DisplayName = displayName,
                        LastMessage = lastMsg,
                        HasUnread = false,
                    });

                    if (chatId == GetCurrentChatId(ent))
                    {
                        state.CurrentChat = BuildChatDetail(chat, account.AccountId, serverComp);
                    }
                }
            }
        }
        else
        {
            Log.Info($"[Chitter] UpdateUi: hasIdCard={hasIdCard}, hasAccount={hasIdCard && TryComp<ChitterAccountComponent>(idCard, out _)}");
        }

        Log.Info($"[Chitter] UpdateUi: sending state with {state.Contacts.Count} contacts, {state.Chats.Count} chats");
        _cartridge.UpdateCartridgeUiState(loader, state);
    }

    private void DiscoverAccountsOnGrid(EntityUid loader, ChitterServerComponent server)
    {
        var grid = Transform(loader).GridUid;
        Log.Info($"[Chitter] DiscoverAccountsOnGrid: loader grid = {grid}");

        var found = 0;
        var skippedZero = 0;
        var skippedGrid = 0;

        var query = EntityQueryEnumerator<ChitterAccountComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AccountId == 0)
            {
                skippedZero++;
                continue;
            }

            if (grid != null)
            {
                var entGrid = Transform(uid).GridUid;
                if (entGrid != null && entGrid != grid)
                {
                    skippedGrid++;
                    continue;
                }
            }

            _server.RegisterOrUpdateAccount(server, comp.AccountId,
                Identity.Name(uid, EntityManager), "", comp.ProfilePictureId);
            found++;
            Log.Info($"[Chitter] DiscoverAccountsOnGrid: registered accId={comp.AccountId}, name={Identity.Name(uid, EntityManager)}");
        }

        Log.Info($"[Chitter] DiscoverAccountsOnGrid: found={found}, skippedZeroId={skippedZero}, skippedDiffGrid={skippedGrid}");
    }

    private Guid? GetCurrentChatId(Entity<ChitterCartridgeComponent> ent)
    {
        return null;
    }

    private ChatDetail BuildChatDetail(ChitterChat chat, uint ownId, ChitterServerComponent server)
    {
        var detail = new ChatDetail
        {
            ChatId = chat.ChatId,
        };

        foreach (var msg in chat.Messages)
        {
            detail.Messages.Add(new MessageEntry
            {
                MessageId = msg.MessageId,
                SenderId = msg.SenderAccountId,
                SenderName = msg.SenderName,
                Timestamp = msg.Timestamp,
                Content = msg.Content,
                DeliveryFailed = msg.DeliveryFailed,
                IsOwn = msg.SenderAccountId == ownId,
            });
        }

        foreach (var pid in chat.ParticipantAccountIds)
        {
            var acc = server.Accounts.GetValueOrDefault(pid);
            detail.Participants.Add(new ParticipantEntry
            {
                AccountId = pid,
                Name = acc?.Name ?? $"#{pid:D4}",
                JobTitle = acc?.JobTitle ?? "",
            });
        }

        return detail;
    }
}
