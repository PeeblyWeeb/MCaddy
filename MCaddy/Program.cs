using System.Reflection;
using System.Security.Cryptography;
using MCaddy.Network;
using MCaddy.Network.Packets;
using MCaddy.Network.Packets.Clientbound;
using MCaddy.Network.Packets.Serverbound;
using MCaddy.Util;

namespace MCaddy;

public static class MCaddy
{
    public static void RegisterPackets()
    {
        foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()))
        {
            var attribute = type.GetCustomAttribute<Register>();
            if (attribute == null) continue;

            if (typeof(IServerboundPacket).IsAssignableFrom(type))
            {
                Registry.ServerboundPackets[(attribute.State, attribute.Type)] = type;
            }
            else if (typeof(IClientboundPacket).IsAssignableFrom(type))
            {
                Registry.ClientboundPackets[(attribute.State, attribute.Type)] = type;
            }
            else
            {
                throw new NotSupportedException("Bare IMinecraftPacket's can not be registered");
            }
        }
    }
    
    public static async Task Main(string[] args)
    {
        RegisterPackets();

        var server = new Server("0.0.0.0", 25565);
        await server.StartAsync();
    }
}