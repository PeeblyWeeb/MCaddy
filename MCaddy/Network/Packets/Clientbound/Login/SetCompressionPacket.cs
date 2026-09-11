using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Clientbound.Login;

[Register(ConnectionState.Login, PacketType.ClientboundSetCompression)]
internal class SetCompressionPacket(
    int threshold
) : IClientboundPacket
{
    public PacketType GetPacketType() => PacketType.ClientboundSetCompression;

    public void ToStream(BinaryWriter writer)
    {
        writer.Write7BitEncodedInt(threshold);
    }

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        return new SetCompressionPacket(
            threshold: reader.Read7BitEncodedInt()    
        );
    }

    public Task Handle(C2SConnection connection)
    {
        connection.CompressionThreshold = threshold;

        return Task.CompletedTask;
    }
}