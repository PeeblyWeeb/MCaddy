using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Clientbound.Status;

internal class PingResponsePacket(long timestamp) : IClientboundPacket
{
    public PacketType GetPacketType() => PacketType.ClientboundPingResponse;

    public void ToStream(BinaryWriter writer)
    {
        writer.Write(timestamp);
    }

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        throw new NotImplementedException();
    }

    public Task Handle(C2SConnection connection)
    {
        throw new NotImplementedException();
    }
}