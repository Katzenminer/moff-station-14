using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.Station;

namespace Content.Shared._Moffstation.NanoChat;

public sealed partial class SharedNanoChatServerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NanoChatServerComponent, ComponentStartup>(OnServerStartup);
    }

    private void OnServerStartup(Entity<NanoChatServerComponent> component, ref ComponentStartup args)
    {
        var servers = EntityQueryEnumerator<NanoChatServerComponent>();
        var mapsWithMainServer = new List<EntityUid>();
        while (servers.MoveNext(out var serverUid, out var server)) // Go through every server
        {
            var serverMap = _station.GetOwningStation(serverUid);
            if (serverMap != null) // If THe server is on a station
            {
                if (!mapsWithMainServer.Contains(serverMap.Value))// If for this station there isnt already a main server
                {
                    server.IsMainServer = true;
                    mapsWithMainServer.Add(serverMap.Value);
                }
            }

        }
    }
}
