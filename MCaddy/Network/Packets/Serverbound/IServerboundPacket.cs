namespace MCaddy.Network.Packets.Serverbound;

internal interface IServerboundPacket : IMinecraftPacket
{
    internal Task Handle(S2CConnection connection);
}