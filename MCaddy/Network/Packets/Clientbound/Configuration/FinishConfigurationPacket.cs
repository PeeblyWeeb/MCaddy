using MCaddy.Network.Packets.Clientbound.Login;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Clientbound.Configuration;

[Register(ConnectionState.Configuration, PacketType.ClientboundFinishConfiguration)]
internal class FinishConfigurationPacket(

) : IClientboundPacket
{
    public PacketType GetPacketType() => PacketType.ClientboundFinishConfiguration;

    public void ToStream(BinaryWriter writer) {}

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        return new FinishConfigurationPacket();
    }

    public Task Handle(C2SConnection connection)
    {
        connection.State = ConnectionState.Play;
        connection.DownstreamConnection.State = ConnectionState.Play;

        return Task.CompletedTask;
    }
}