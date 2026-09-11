using MCaddy.Network.Packets;

namespace MCaddy.Network;

public class Register : Attribute
{
    public readonly ConnectionState State;
    public readonly PacketType Type;

    public Register(
        ConnectionState state,
        PacketType type
    )
    {
        State = state;
        Type = type;
    }
}