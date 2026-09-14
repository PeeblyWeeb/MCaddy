using MCaddy.Authentication.Responses.Minecraft;
using MCaddy.Util;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets.Clientbound.Login;

[Register(ConnectionState.Login, PacketType.ClientboundLoginSuccess)]
internal class LoginSuccessPacket(
    Guid uuid,
    string username,
    MinecraftPlayerJoinResponse.PropertiesField[] properties
) : IClientboundPacket
{
    public PacketType GetPacketType() => PacketType.ClientboundLoginSuccess;

    public void ToStream(BinaryWriter writer)
    {
        writer.Write(uuid.ToByteArray(bigEndian: true));
        writer.Write(username);
        
        writer.Write7BitEncodedInt(properties.Length);
        foreach (var property in properties)
        {
            Console.WriteLine($"Property: {property.Name}: {property.Value}: {property.Signature}");
            writer.Write(property.Name);
            writer.Write(property.Value);
            writer.Write(property.Signature != null);
            if (property.Signature != null)
                writer.Write(property.Signature);
        }
    }

    public static IMinecraftPacket FromStream(BinaryReader reader)
    {
        Guid uuid = new Guid(reader.ReadBytes(16), bigEndian: true);
        string username = reader.ReadString();
        var properties = new MinecraftPlayerJoinResponse.PropertiesField[reader.Read7BitEncodedInt()];

        for (var i = 0; i < properties.Length; i++)
        {
            properties[i] = new()
            {
                Name = reader.ReadString(),
                Value = reader.ReadString(),
                Signature = reader.ReadString(),
            };
        }
        
        return new LoginSuccessPacket(
            uuid: uuid,
            username: username,
            properties: properties
        );
    }

    public async Task Handle(C2SConnection connection)
    {
        connection.State = ConnectionState.Configuration;
        connection.DownstreamConnection.State = ConnectionState.Configuration;
        
        await connection.DownstreamConnection.SendAsync(new LoginSuccessPacket(
            uuid: uuid,
            username: username,
            properties: properties
        ));
    }
}