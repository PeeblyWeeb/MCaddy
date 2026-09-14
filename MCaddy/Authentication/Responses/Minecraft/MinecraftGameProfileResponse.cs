namespace MCaddy.Authentication.Responses.Minecraft;

public record MinecraftGameProfileResponse
{
    public record MinecraftSkinResponse
    {
        public required string Id { get; init; }
        public required string State { get; init; }
        public required string Url { get; init; }
        public required string Variant { get; init; }
        public string? Alias { get; init; }
    }
    public record MinecraftCapeResponse
    {
        public required string Id { get; init; }
        public required string State { get; init; }
        public required string Url { get; init; }
        public required string Alias { get; init; }
    }
    
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required MinecraftSkinResponse[] Skins { get; init; }
    public required MinecraftCapeResponse[] Capes { get; init; }
};