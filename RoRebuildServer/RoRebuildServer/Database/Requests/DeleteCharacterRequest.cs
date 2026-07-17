using Microsoft.EntityFrameworkCore;
using RebuildSharedData.Networking;
using RoRebuildServer.Networking;

namespace RoRebuildServer.Database.Requests
{
    public class DeleteCharacterRequest : IDbRequest
    {
        private NetworkConnection connection;
        private string deleteName;
        private int slotId;

        public DeleteCharacterRequest(NetworkConnection connection, string deleteName, int slotId)
        {
            this.connection = connection;
            this.deleteName = deleteName;
            this.slotId = slotId;
        }

        public async Task ExecuteAsync(RoContext dbContext)
        {
            var rows = await dbContext.Character
                .Where(c => c.AccountId == connection.AccountId
                         && c.CharacterSlot == slotId
                         && c.Name == deleteName)
                .ExecuteDeleteAsync();

            var success = rows > 0;

            var packet = NetworkManager.StartPacket(PacketType.DeleteCharacterResult, 8);
            packet.Write(success);
            packet.Write(slotId);

            NetworkManager.SendMessage(packet, connection);
        }
    }
}