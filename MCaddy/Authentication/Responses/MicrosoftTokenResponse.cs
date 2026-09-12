namespace MCaddy.Authentication.Responses;

public record MicrosoftTokenResponse
{
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
    public required string Scope { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required string UserId { get; init; }
    
    public DateTime CreationTime { get; init; } = DateTime.UtcNow;
    
    public bool IsExpired() => DateTime.UtcNow >= CreationTime.AddSeconds(ExpiresIn);
}