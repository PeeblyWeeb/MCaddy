using System.Text.Json;
using MCaddy.Util;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Clientbound.Status;

internal class StatusResponsePacket(ServerStatusResponse status) : IClientboundPacket
{
    PacketType IMinecraftPacket.GetPacketType() => PacketType.ClientboundStatusResponse;

    public void ToStream(BinaryWriter writer)
    {
        var statusString = JsonSerializer.Serialize(status, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        writer.Write(statusString);
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