using System.Net;
using System.Security.Cryptography;
using System.Text;
using MCaddy.Network.Packets.Clientbound.Login;
using MCaddy.Util;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Serverbound.Login;

[Register(ConnectionState.Login, PacketType.ServerboundEncryptionResponse)]
internal class EncryptionResponsePacket(byte[] encryptedSharedSecret, byte[] encryptedVerifyToken) : IServerboundPacket
{
    public PacketType GetPacketType() => PacketType.ServerboundEncryptionResponse;

    public void ToStream(BinaryWriter writer)
    {
        throw new NotImplementedException();
    }

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        return new EncryptionResponsePacket(
            encryptedSharedSecret: reader.ReadBytes(reader.Read7BitEncodedInt()),
            encryptedVerifyToken: reader.ReadBytes(reader.Read7BitEncodedInt())
        );
    }

    public async Task Handle(S2CConnection connection)
    {
        if (connection.Uuid == null)
        {
            connection.Logger.Log("Out of order packet!", Logger.LogLevel.Error);
            return;
        }
        
        var sharedSecret = Server.KeyPair.Decrypt(encryptedSharedSecret, RSAEncryptionPadding.Pkcs1);
        var verifyToken = Server.KeyPair.Decrypt(encryptedVerifyToken, RSAEncryptionPadding.Pkcs1);

        var clientToken = Convert.ToHexString(verifyToken);
        var serverToken = Convert.ToHexString(connection.VerifyToken);
        
        if (clientToken != serverToken)
        {
            // TODO: disconnect that bitch
            connection.Logger.Log(
                $"Client failed encryption",
                Logger.LogLevel.Error
            );
            return;
        }
        connection.SetEncryption(sharedSecret);

        if (Server.Properties.OnlineMode)
        {
            using var hashStream = new MemoryStream();
            hashStream.Write(Encoding.ASCII.GetBytes(""));
            hashStream.Write(sharedSecret);
            hashStream.Write(Server.KeyPair.ExportSubjectPublicKeyInfo());

            var sessionHash = MinecraftSha.MinecraftShaDigest(hashStream.ToArray());
            var requestUrl = "https://sessionserver.mojang.com/session/minecraft/hasJoined" 
                                + $"?username={connection.Username}&serverId={sessionHash}";
            
            var res = await Server.Http.GetAsync(requestUrl);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                connection.Logger.Log($"Failed to validate username", Logger.LogLevel.Error);
                return;
            }
            
            // connection.Logger.Log($"Session server response: {await res.Content.ReadAsStringAsync()}", Logger.LogLevel.Debug);
            connection.SetIdentity(connection.Username!);
            connection.Logger.Log($"Client authenticated as {connection.Username}, session server said so!");
        }
        else
        {
            connection.Logger.Log($"Authentication is disabled, {connection.Username}'s identity will not be validated.", Logger.LogLevel.Warning);
        }

        await connection.SetupUpstreamClient();
    }
}