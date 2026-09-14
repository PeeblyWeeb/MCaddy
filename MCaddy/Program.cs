using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using MCaddy.Authentication;
using MCaddy.Network;
using MCaddy.Network.Packets;
using MCaddy.Network.Packets.Clientbound;
using MCaddy.Network.Packets.Serverbound;
using MCaddy.Util;

namespace MCaddy;

public static class MCaddy
{
    private static readonly Logger Logger = new Logger("Main");
    
    private static readonly string AppPath = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string PropertiesFilePath = $"{AppPath}Properties.json";
    internal static readonly string AuthCacheFilePath = $"{AppPath}AuthCache.json";

    internal static Properties Properties = null!;
    internal static Session Session = new();
    
    private static void RegisterPackets()
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

    private static Properties LoadProperties()
    {
        try
        {
            if (File.Exists(PropertiesFilePath))
                return JsonSerializer.Deserialize<Properties>(File.ReadAllText(PropertiesFilePath))!;
        } catch (JsonException ex)
        {
            Logger.Log($"Failed to load from properties file: {ex}", Logger.LogLevel.Error);
        }
        
        Logger.Log("Properties file is missing or invalid, using defaults.");
        return new Properties();
    }

    private static void SaveProperties(Properties properties)
    {
        Logger.Log($"Saving properties to '{PropertiesFilePath}'");

        using FileStream stream = new(PropertiesFilePath, FileMode.Create, FileAccess.Write);
        stream.Write(JsonSerializer.SerializeToUtf8Bytes(properties, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4
        }));
    }
    
    public static async Task Main(string[] args)
    {
        Properties = LoadProperties();
        Console.CancelKeyPress += (_, _) =>
        {
            SaveProperties(Properties);
        };

        await Session.Login();
        Logger.Log($"Logged in as {Session.GameProfile.Name}");
        
        RegisterPackets();
        
        var server = new Server(Properties);
        await server.StartAsync();
        
        SaveProperties(Properties);
    }
}