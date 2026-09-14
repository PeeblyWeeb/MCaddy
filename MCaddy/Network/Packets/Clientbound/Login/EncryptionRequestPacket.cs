using System.Net.Http.Json;
using System.Security.Cryptography;
using MCaddy.Authentication.Requests.Minecraft;
using MCaddy.Network.Packets.Serverbound.Login;
using MCaddy.Util;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Clientbound.Login;

[Register(ConnectionState.Login, PacketType.ClientboundEncryptionRequest)]
internal class EncryptionRequestPacket(
    string serverId,
    byte[] publicKey,
    byte[] verifyToken,
    bool shouldAuthenticate
) : IClientboundPacket
{
    public PacketType GetPacketType() => PacketType.ClientboundEncryptionRequest;

    public void ToStream(BinaryWriter writer)
    {
        writer.Write(serverId);

        // prefixed array of bytes
        writer.Write7BitEncodedInt(publicKey.Length);
        writer.Write(publicKey);
        writer.Write7BitEncodedInt(verifyToken.Length);
        writer.Write(verifyToken);
        
        writer.Write(shouldAuthenticate);
    }

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        return new EncryptionRequestPacket(
            serverId: reader.ReadString(),
            publicKey: reader.ReadBytes(reader.Read7BitEncodedInt()),
            verifyToken: reader.ReadBytes(reader.Read7BitEncodedInt()),
            shouldAuthenticate: reader.ReadBoolean()
        );
    }

    public async Task Handle(C2SConnection connection)
    {
        byte[] sharedSecret = RandomNumberGenerator.GetBytes(16);
        string serverHash = Minecraft.GetServerHash(sharedSecret, publicKey, serverId);

        RSA loadedPublicKey = RSA.Create();
        loadedPublicKey.ImportSubjectPublicKeyInfo(publicKey, out _);
        
        var response = await Server.Http.PostAsJsonAsync(
            "https://sessionserver.mojang.com/session/minecraft/join",
            new MinecraftJoinRequest()
            {
                AccessToken = MCaddy.Session.State.MinecraftTokenResponse!.AccessToken,
                SelectedProfile = MCaddy.Session.GameProfile.Id,
                ServerId = serverHash,
            }
        );
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Failed to authenticate with session server: {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        byte[] encryptedSharedSecret = loadedPublicKey.Encrypt(sharedSecret, RSAEncryptionPadding.Pkcs1);
        byte[] encryptedVerifyToken = loadedPublicKey.Encrypt(verifyToken, RSAEncryptionPadding.Pkcs1);

        await connection.SendAsync(new EncryptionResponsePacket(encryptedSharedSecret, encryptedVerifyToken));
        
        connection.SetEncryption(sharedSecret);
    }
}