namespace MCaddy.Authentication.Requests.Minecraft;

public record MinecraftTokenRequest
{
    public required string IdentityToken { get; init; }
}