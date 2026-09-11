using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Serverbound;

[Register(ConnectionState.Handshake, PacketType.ServerboundHandshake)]
internal class HandshakePacket(
    int protocolVersion,
    string serverAddress,
    ushort serverPort,
    ConnectionState targetState
) : IServerboundPacket
{
    public PacketType GetPacketType() => PacketType.ServerboundHandshake;
    
    public void ToStream(BinaryWriter writer)
    {
        writer.Write7BitEncodedInt(protocolVersion);
        writer.Write(serverAddress);
        writer.Write(serverPort);
        writer.Write7BitEncodedInt((int)targetState);
    }

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        var protocolVersion = reader.Read7BitEncodedInt();
        var serverAddress = reader.ReadString();
        
        return new HandshakePacket(
            protocolVersion: protocolVersion,
            serverAddress: serverAddress,
            serverPort: reader.ReadUInt16(),
            targetState: (ConnectionState)reader.Read7BitEncodedInt()
        );
    }

    public Task Handle(S2CConnection connection)
    {
        connection.Logger.Log(
            $"Client Version: {protocolVersion}; Connecting from {serverAddress}:{serverPort}; with Intent {targetState}");
        
        connection.ProtocolVersion = protocolVersion;
        connection.State = targetState;

        return Task.CompletedTask;
    }
}