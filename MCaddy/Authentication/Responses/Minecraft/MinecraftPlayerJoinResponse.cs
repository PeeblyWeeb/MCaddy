namespace MCaddy.Authentication.Responses.Minecraft;

public record MinecraftPlayerJoinResponse
{
    public record PropertiesField
    {
        public required string Name { get; init; }
        public required string Value { get; init; }
        public required string? Signature { get; init; }
    }
    
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required PropertiesField[] Properties { get; init; }
}