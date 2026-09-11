using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using MCaddy.Network;
using MCaddy.Network.Packets;
using MCaddy.Network.Packets.Serverbound;
using MCaddy.Util;
using Universal.Common;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy;

public class Server
{
    public static Properties Properties = null!;
    
    private readonly Logger _logger = new("Server");
    
    private readonly CancellationTokenSource _cts = new();
    private readonly TcpListener _listener;

    internal static readonly RSA KeyPair = RSA.Create(2048);
    internal static readonly HttpClient Http = new();

    internal static readonly DateTime StartTime = DateTime.Now;

    internal Server(Properties props)
    {
        Properties = props;

        _listener = new(IPAddress.Parse(Properties.Host), Properties.Port);
    }
    
    internal async Task StartAsync()
    {
        _listener.Start();
        _logger.Log($"Listening on {Properties.Host}:{Properties.Port} ..");

        while (!_cts.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync();
            S2CConnection connection = new(client, _cts.Token);
            
            _logger.Log($"Receiving client connection from {connection.RemoteIpAddress}:{connection.RemotePort}");
            _ = HandleClientAsync(connection);
        }
    }

    private static async Task HandleClientAsync(S2CConnection connection)
    {
        Exception? disconnectReason = null;
        
        while (!connection.CancellationToken.IsCancellationRequested)
        {
            PacketHeader header;
            try
            {
                header = await MinecraftConnection.ReadPacketHeader(connection.Stream, connection.CompressionThreshold);
            }
            catch (Exception ex)
            {
                disconnectReason = ex;
                break;
            }

            if (connection.State >= ConnectionState.Configuration)
            {
                // forwarding:
                using var ms = new MemoryStream();
                await using var writer = new BinaryWriter(ms, Endian.Big);
                
                writer.Write7BitEncodedInt(header.PacketId);
                writer.Write(header.BodyBytes);
                
                byte[] packetBytes = MinecraftConnection.WrapPacketBody(ms.ToArray(), connection.UpstreamConnection!.CompressionThreshold);
                try
                {
                    await connection.UpstreamConnection.Stream.WriteAsync(packetBytes);
                }
                catch (Exception ex)
                {
                    disconnectReason = ex;
                    break;
                }
            }

            Registry.ServerboundPackets.TryGetValue((connection.State, (PacketType)header.PacketId),
                out var handlerType);
            if (handlerType == null)
            {
                connection.Logger.Log(
                    $"No corresponding handler found. State: {Enum.GetName(connection.State)}, Version: {connection.ProtocolVersion}, ID: {header.PacketId}",
                    Logger.LogLevel.Warning
                );
                continue;
            }

            connection.Logger.Log($">> Incoming: {handlerType.Name}", Logger.LogLevel.Debug);
            
            using var bodyStream = new MemoryStream(header.BodyBytes);
            using var bodyReader = new BinaryReader(bodyStream, Endian.Big);

            try
            {
                var handleMethod = handlerType.GetMethod("FromStream", BindingFlags.Public | BindingFlags.Static,
                    [typeof(BinaryReader)]);
                
                var handler = (IServerboundPacket)handleMethod!.Invoke(null, [bodyReader])!;
                await handler.Handle(connection);
            }
            catch (Exception e)
            {
                connection.Logger.Log($"Packet handling error: {e}", Logger.LogLevel.Error);
            }
        }
        
        connection.Logger.Log($"Connection Closed: {disconnectReason?.ToString() ?? "Graceful disconnect initiated by the server."}");
        await connection.DisposeAsync();
    }
}