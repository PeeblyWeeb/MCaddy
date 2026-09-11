using System.Net.Sockets;
using System.Security.Cryptography;

namespace MCaddy.Network;

internal class S2CConnection(TcpClient client, CancellationToken ct = default) : MinecraftConnection(client, ct)
{
    internal readonly byte[] VerifyToken = RandomNumberGenerator.GetBytes(32);

    internal Guid? Uuid;
    internal string? Username;

    internal C2SConnection? UpstreamConnection;

    internal async Task SetupUpstreamClient()
    {
        TcpClient client = new();
        await client.ConnectAsync(Server.TargetHost, Server.TargetPort);

        UpstreamConnection = new(client, CancellationToken);
        await UpstreamConnection.StartLogin(this);
    }

    public override async ValueTask DisposeAsync()
    {
        if (UpstreamConnection != null)
            await UpstreamConnection.DisposeAsync();
        await base.DisposeAsync();
    }
}