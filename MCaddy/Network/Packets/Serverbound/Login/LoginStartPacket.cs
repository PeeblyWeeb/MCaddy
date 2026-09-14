using MCaddy.Authentication.Responses.Minecraft;
using MCaddy.Network.Packets.Clientbound.Login;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Serverbound.Login;

[Register(ConnectionState.Login, PacketType.ServerboundLoginStart)]
internal class LoginStartPacket(string username, Guid uuid) : IServerboundPacket
{
    public PacketType GetPacketType() => PacketType.ServerboundLoginStart;

    public void ToStream(BinaryWriter writer)
    {
        writer.Write(username);
        writer.Write(uuid.ToByteArray(bigEndian: true));
    }

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        return new LoginStartPacket(
            username: reader.ReadString(),
            uuid: new Guid(reader.ReadBytes(16), bigEndian: true)
        );
    }

    public async Task Handle(S2CConnection connection)
    {
        connection.Logger.Log($"Client is logging in as {username}");
        connection.SetIdentity($"{username}?");
        
        connection.Uuid = uuid;
        connection.Username = username;
        
        if (Server.Properties.CompressionThreshold != null)
        {
            await connection.SendAsync(new SetCompressionPacket(
                threshold: 256
            ));
            connection.CompressionThreshold = 256;
        }
        
        if (Server.Properties.UseEncryption)
        {
            await connection.SendAsync(
                new EncryptionRequestPacket(
                    "", // this is always empty, pretty much
                    Server.KeyPair.ExportSubjectPublicKeyInfo(),
                    connection.VerifyToken,
                    Server.Properties.OnlineMode
                )
            );
        }
        else
        {
            await connection.SendAsync(new LoginSuccessPacket(uuid, username, []));
            connection.State = ConnectionState.Configuration;
            
            await connection.SetupUpstreamClient();
        }
    }
}