namespace MCaddy.Network.Packets.Clientbound;

internal interface IClientboundPacket : IMinecraftPacket
{
    internal Task Handle(C2SConnection connection);
}