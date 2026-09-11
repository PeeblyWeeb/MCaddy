namespace MCaddy.Network.Packets;

internal class Registry
{
    public static Dictionary<(ConnectionState state, PacketType type), Type> ServerboundPackets = new();
    public static Dictionary<(ConnectionState state, PacketType type), Type> ClientboundPackets = new();
}