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
        throw new NotImplementedException();
    }

    public Task Handle(C2SConnection connection)
    {
        throw new NotImplementedException();
    }
}