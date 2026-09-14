namespace MCaddy.Authentication.Requests.Minecraft;

public record MinecraftJoinRequest
{
    public required string AccessToken { get; init; }
    public required string SelectedProfile { get; init; }
    public required string ServerId { get; init; }
}