using System.Net.Sockets;

namespace MCaddy.Util;

public static class VarInt
{
    public static int GetByteLength(int value)
    {
        var length = 1;
        while ((value & ~0x7f) != 0)
        {
            length++;
            value >>>= 7;
        }

        return length;
    }

    public static int ReadVarIntFromStream(Stream stream)
    {
        var result = 0;
        for (var bytesRead = 0; bytesRead < 5; bytesRead++)
        {
            var currentByte = stream.ReadByte();
            if (currentByte == -1)
                throw new EndOfStreamException("Reached end of stream while reading a VarInt");
            
            result |= (currentByte & 0x7f) << (7 * bytesRead);

            if ((currentByte & 0x80) == 0) return result;
        }

        throw new InvalidOperationException("VarInt exceeds maximum length of 5 bytes");
    }
}