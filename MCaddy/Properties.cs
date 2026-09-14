namespace MCaddy;

public record Properties()
{
    public string Host { get; init; } = "127.0.0.1";
    public ushort Port { get; init; } = 25565;
    public string TargetHost { get; init; }= "127.0.0.1";
    public ushort TargetPort { get; init; } = 25566;
    
    public bool OnlineMode { get; init; } = true;
    public bool UseEncryption { get; init; } = true;
    public int? CompressionThreshold { get; init; } = 256;

    public string MicrosoftClientId { get; init; } = "00000000402b5328";
}