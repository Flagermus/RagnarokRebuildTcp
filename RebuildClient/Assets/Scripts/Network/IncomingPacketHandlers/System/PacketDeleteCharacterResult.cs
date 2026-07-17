using Assets.Scripts.Network.HandlerBase;
using RebuildSharedData.Networking;

namespace Assets.Scripts.Network.IncomingPacketHandlers.System
{
    [ClientPacketHandler(PacketType.DeleteCharacterResult)]
    public class PacketDeleteCharacterResult : ClientPacketHandlerBase
    {
        public override void ReceivePacket(ClientInboundMessage msg)
        {
            var success = msg.ReadBoolean();
            var slot = msg.ReadInt32();

            if (success)
                NetworkManager.Instance.TitleScreen.CharacterSelectWindow.OnCharacterDeleted(slot);
        }
    }
}
