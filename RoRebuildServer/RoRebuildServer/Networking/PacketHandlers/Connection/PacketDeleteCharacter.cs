using RebuildSharedData.Networking;
using RoRebuildServer.Database;
using RoRebuildServer.Database.Requests;

namespace RoRebuildServer.Networking.PacketHandlers.Connection;

[ClientPacketHandler(PacketType.DeleteCharacter)]
public class PacketDeleteCharacter : IClientPacketHandler
{
    public void Process(NetworkConnection connection, InboundMessage msg)
    {
        if (connection.Character != null)
            return;

        var slot = msg.ReadInt32();
        var name = msg.ReadString();

        RoDatabase.EnqueueDbRequest(new DeleteCharacterRequest(connection, name, slot));
    }
}