using MCaddy.Util;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Clientbound.Login;

[Register(ConnectionState.Login, PacketType.ClientboundLoginSuccess)]
internal class LoginSuccessPacket(
    Guid uuid,
    string username
) : IClientboundPacket
{
    public PacketType GetPacketType() => PacketType.ClientboundLoginSuccess;

    public void ToStream(BinaryWriter writer)
    {
        var uuidBytes = uuid.ToByteArray();
        
        writer.Write(uuidBytes);
        writer.Write(username);
        
        // empty properties array // TODO
        writer.Write7BitEncodedInt(0);
    }

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        return new LoginSuccessPacket(
            uuid: new Guid(reader.ReadBytes(16), bigEndian: true),
            username: reader.ReadString()
        );
    }

    public async Task Handle(C2SConnection connection)
    {
        connection.State = ConnectionState.Configuration;
        connection.DownstreamConnection.State = ConnectionState.Configuration;
        
        await connection.DownstreamConnection.SendAsync(new LoginSuccessPacket(
            uuid: (Guid)connection.DownstreamConnection.Uuid!,
            username: connection.DownstreamConnection.Username!
        ));
    }
}