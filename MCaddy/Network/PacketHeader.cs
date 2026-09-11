namespace MCaddy.Network;

public record PacketHeader(int PacketId, byte[] BodyBytes);