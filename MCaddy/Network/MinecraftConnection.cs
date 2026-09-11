using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using MCaddy.Network.Packets;
using MCaddy.Util;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Universal.Common;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network;

internal abstract class MinecraftConnection : IAsyncDisposable
{
    internal readonly Logger Logger;
    internal readonly CancellationToken CancellationToken;
    
    #region Streams
    
    private readonly NetworkStream _rawStream;
    private CipherStream? _cipherStream;
    internal Stream Stream => _cipherStream ?? (Stream)_rawStream;
    
    #endregion

    #region Peer Metadata

    internal readonly IPAddress RemoteIpAddress;
    internal readonly ushort RemotePort;

    internal int? ProtocolVersion = null;

    #endregion

    internal int? CompressionThreshold
    {
        get;
        set
        {
            Logger.Log($"This connection is now compressed (threshold: {value})");
            
            field = value;
        }
    }

    internal ConnectionState State
    {
        get;
        set
        {
            Logger.Log($"Switching from state {Enum.GetName(field)} -> {Enum.GetName(value)}", Logger.LogLevel.Debug);

            field = value;
        }
    } = ConnectionState.Handshake;

    protected MinecraftConnection(TcpClient client, CancellationToken ct = default)
    {
        CancellationToken = ct;
        
        _rawStream = client.GetStream();
        RemoteIpAddress = ((IPEndPoint)client.Client.RemoteEndPoint!).Address;
        RemotePort = (ushort)((IPEndPoint)client.Client.RemoteEndPoint!).Port;
        
        Logger = new Logger($"{GetType().Name}[{RemoteIpAddress.ToString()}]");
    }

    internal void SetEncryption(byte[] sharedSecret)
    {
        ICipherParameters cipherParams = new ParametersWithIV(new KeyParameter(sharedSecret), sharedSecret);
        
        IBlockCipherMode decryptEngine = new CfbBlockCipher(new AesEngine(), 8);
        IBlockCipherMode encryptEngine = new CfbBlockCipher(new AesEngine(), 8);
        
        IBufferedCipher decryptCipher = new BufferedBlockCipher(decryptEngine);
        IBufferedCipher encryptCipher = new BufferedBlockCipher(encryptEngine);
        decryptCipher.Init(false, cipherParams);
        encryptCipher.Init(true, cipherParams);

        _cipherStream = new CipherStream(_rawStream, decryptCipher, encryptCipher);
    }

    internal static byte[] WrapPacketBody(byte[] bodyBytes, int? compressionThreshold = null)
    {
        using var intermediateStream = new MemoryStream();
        using var intermediateWriter = new BinaryWriter(intermediateStream, Endian.Big);

        using var finalStream = new MemoryStream();
        using var finalWriter = new BinaryWriter(finalStream, Endian.Big);

        if (compressionThreshold != null)
        {
            // With compression
            bool overThreshold = bodyBytes.Length >= compressionThreshold;
            intermediateWriter.Write7BitEncodedInt(overThreshold ? bodyBytes.Length : 0); // [Data Length]

            if (overThreshold)
            {
                using var zlibMemoryStream = new MemoryStream();
                using (var zlibStream = new ZLibStream(zlibMemoryStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlibStream.Write(bodyBytes); // [Packet ID][Data] (compressed)
                }
                
                bodyBytes = zlibMemoryStream.ToArray();
            }
            
            intermediateWriter.Write(bodyBytes); // [Data Length][Packet ID][Data]
            byte[] intermediateBytes = intermediateStream.ToArray();
            
            finalWriter.Write7BitEncodedInt(intermediateBytes.Length); // [Packet Length]
            finalWriter.Write(intermediateBytes); // [Packet Length][Data Length][Packet ID][Data]
        }
        else
        {
            // Without compression
            finalWriter.Write7BitEncodedInt(bodyBytes.Length); // [Packet Length]
            finalWriter.Write(bodyBytes); // [Packet Length][Packet ID][Data]
        }

        return finalStream.ToArray();
    }
    
    internal static byte[] GetPacketBytes(IMinecraftPacket packet, int? compressionThreshold = null)
    {
        using var bodyStream = new MemoryStream();
        using var bodyWriter = new BinaryWriter(bodyStream, Endian.Big);
        
        bodyWriter.Write7BitEncodedInt((int)packet.GetPacketType()); // [Packet ID]
        packet.ToStream(bodyWriter); // [Packet ID][Data]

        byte[] bodyBytes = bodyStream.ToArray(); // [Packet ID][Data]

        return WrapPacketBody(bodyBytes, compressionThreshold);
    }

    internal static async Task<PacketHeader> ReadPacketHeader(Stream stream, int? compressionThreshold = null)
    {
        int length = VarInt.ReadVarIntFromStream(stream);
        int packetId;
        byte[] bodyBytes;

        if (compressionThreshold != null)
        {
            int dataLength = VarInt.ReadVarIntFromStream(stream);
            if (dataLength >= compressionThreshold)
            {
                // With compression
                byte[] compressedBytes = new byte[length - VarInt.GetByteLength(dataLength)];
                await stream.ReadExactlyAsync(compressedBytes);
            
                using var zlibMemoryStream = new MemoryStream(compressedBytes);
                await using var zlibStream = new ZLibStream(zlibMemoryStream, CompressionMode.Decompress);

                packetId = VarInt.ReadVarIntFromStream(zlibStream);
                bodyBytes = new byte[dataLength - VarInt.GetByteLength(packetId)];

                await zlibStream.ReadExactlyAsync(bodyBytes);
            }
            else
            {
                // Without compression
                packetId = VarInt.ReadVarIntFromStream(stream);

                int bodyLength = length - VarInt.GetByteLength(dataLength) - VarInt.GetByteLength(packetId);
                bodyBytes = new byte[bodyLength];
    
                if (bodyLength > 0)
                {
                    await stream.ReadExactlyAsync(bodyBytes);
                }
            }
        }
        else
        {
            packetId = VarInt.ReadVarIntFromStream(stream);
            bodyBytes = new byte[length - VarInt.GetByteLength(packetId)];
            
            await stream.ReadExactlyAsync(bodyBytes);
        }
        
        return new PacketHeader(packetId, bodyBytes);
    }
    
    internal async Task SendAsync(IMinecraftPacket packet)
    {
        var bytes = GetPacketBytes(packet, CompressionThreshold);

        Logger.Log(
            $"<< Outgoing: {packet.GetType().Name}({bytes.Length})"
            + $"\n   ⤷ {Convert.ToHexString(bytes)}",
            Logger.LogLevel.Debug
        );
        
        await Stream.WriteAsync(bytes, CancellationToken);
    }

    internal void SetIdentity(string text)
    {
        Logger.Name = $"{GetType().Name}[{text}]";
    }

    public virtual async ValueTask DisposeAsync()
    {
        await _rawStream.DisposeAsync();
        if (_cipherStream != null) await _cipherStream.DisposeAsync();
    }
}