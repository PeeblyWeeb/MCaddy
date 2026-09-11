using MCaddy.Network.Packets.Clientbound.Status;
using MCaddy.Util;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Serverbound.Status;

[Register(ConnectionState.Status, PacketType.ServerboundStatusRequest)]
internal class StatusRequestPacket() : IServerboundPacket
{
    public PacketType GetPacketType() => PacketType.ServerboundStatusRequest;
    
    public void ToStream(BinaryWriter writer)
    {
        throw new NotImplementedException();
    }

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        return new StatusRequestPacket();
    }

    public async Task Handle(S2CConnection connection)
    {
        ServerStatusResponse status = new()
        {
            Version = new ServerStatusResponse.VersionResponse
            {
                Name = "MCaddy",
                Protocol = connection.ProtocolVersion ?? 0
            },
            Description = new ServerStatusResponse.DescriptionResponse
            {
                Text = $"""
                        §9§lMCaddy §f§lServer §8§l• §r§7Targeting §f{Server.Properties.TargetHost}§7:§8{Server.Properties.TargetPort}
                        §fRunning since {Math.Round((DateTime.Now - Server.StartTime).TotalSeconds)} second(s) ago.
                        """
            },
            EnforcesSecureChat = false
        };

        await connection.SendAsync(new StatusResponsePacket(status));
    }
}