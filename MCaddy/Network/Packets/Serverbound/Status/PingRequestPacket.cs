using MCaddy.Network.Packets.Clientbound.Status;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Serverbound.Status;

[Register(ConnectionState.Status, PacketType.ServerboundPingRequest)]
internal class PingRequestPacket(long timestamp) : IServerboundPacket
{
    public PacketType GetPacketType() => PacketType.ServerboundHandshake;
    
    public void ToStream(BinaryWriter writer)
    {
        throw new NotImplementedException();
    }

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        return new PingRequestPacket(
            timestamp: reader.ReadInt64()    
        );
    }

    public async Task Handle(S2CConnection connection)
    {
        await connection.SendAsync(new PingResponsePacket(timestamp));
    }
}