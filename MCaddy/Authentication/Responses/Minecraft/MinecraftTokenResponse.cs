namespace MCaddy.Authentication.Responses.Minecraft;

public record MinecraftTokenResponse
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
    
    public DateTime CreationTime { get; init; } = DateTime.UtcNow;
    
    public bool IsExpired() => DateTime.UtcNow >= CreationTime.AddSeconds(ExpiresIn);
}