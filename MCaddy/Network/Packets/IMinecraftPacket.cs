using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network.Packets;

internal interface IMinecraftPacket
{
    internal PacketType GetPacketType();
    
    internal void ToStream(BinaryWriter writer);
    internal static abstract IMinecraftPacket FromStream(BinaryReader reader);
}