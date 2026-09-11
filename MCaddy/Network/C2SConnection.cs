using System.Net;
using System.Net.Sockets;
using System.Reflection;
using MCaddy.Network.Packets;
using MCaddy.Network.Packets.Clientbound;
using MCaddy.Network.Packets.Serverbound;
using MCaddy.Network.Packets.Serverbound.Login;
using MCaddy.Util;
using Universal.Common;
using BinaryReader = Universal.Common.BinaryReader;
using BinaryWriter = Universal.Common.BinaryWriter;

namespace MCaddy.Network;

internal class C2SConnection(TcpClient client, CancellationToken ct = default) : MinecraftConnection(client, ct)
{
    internal S2CConnection DownstreamConnection;

    internal async Task NetworkLoop()
    {
        Logger.Log("Started reading from upstream server", Logger.LogLevel.Debug);
        while (!ct.IsCancellationRequested)
        {
            PacketHeader header;
            try
            {
                header = await ReadPacketHeader(Stream, CompressionThreshold);
            }
            catch (Exception ex)
            {
                break;
            }

            if (State >= ConnectionState.Configuration)
            {
                // forwarding:
                using var ms = new MemoryStream();
                await using var writer = new BinaryWriter(ms, Endian.Big);
                
                writer.Write7BitEncodedInt(header.PacketId);
                writer.Write(header.BodyBytes);
                
                byte[] packetBytes = WrapPacketBody(ms.ToArray(), DownstreamConnection.CompressionThreshold);
                await DownstreamConnection.Stream.WriteAsync(packetBytes);
            }
            
            Registry.ClientboundPackets.TryGetValue((State, (PacketType)header.PacketId),
                out var handlerType);
            if (handlerType == null)
            {
                Logger.Log(
                    $"No corresponding handler found. State: {Enum.GetName(State)}, Version: {ProtocolVersion}, ID: {header.PacketId}",
                    Logger.LogLevel.Warning
                );
                continue;
            }
            
            Logger.Log($">> Incoming: {handlerType.Name}", Logger.LogLevel.Debug);
            
            using var bodyStream = new MemoryStream(header.BodyBytes);
            using var bodyReader = new BinaryReader(bodyStream, Endian.Big);

            try
            {
                MethodInfo handleMethod = null!;
                foreach (var method in handlerType.GetMethods())
                {
                    if (method.Name == "FromStream")
                        handleMethod = method;
                }
                
                // var handleMethod = handlerType.GetMethod("FromStream", BindingFlags.Public | BindingFlags.Static,
                //     [typeof(BinaryReader)]);

                var handler = (IClientboundPacket)handleMethod!.Invoke(null, [bodyReader])!;
                await handler.Handle(this);
            }
            catch (Exception e)
            {
                Logger.Log($"Packet handling error: {e}", Logger.LogLevel.Error);
            }
        }
    }
    
    internal async Task StartLogin(S2CConnection downstream)
    {
        DownstreamConnection = downstream;
        ProtocolVersion = downstream.ProtocolVersion;
        
        await SendAsync(new HandshakePacket(
            protocolVersion: ProtocolVersion ?? 0,
            serverAddress: Server.Properties.TargetHost,
            serverPort: Server.Properties.TargetPort,
            targetState: ConnectionState.Login
        ));
        await SendAsync(new LoginStartPacket(
            username: "WeeblyPeeb",
            uuid: DownstreamConnection.Uuid ?? Guid.CreateVersion7()
        ));
        State = ConnectionState.Login;
        
        _ = Task.Run(NetworkLoop);
    }
}