namespace MCaddy.Authentication.Requests;

public record XboxXstsTokenRequest
{
    public record PropertiesField
    {
        public required string SandboxId { get; init; }
        public required string[] UserTokens { get; init; }
    }
    
    public required PropertiesField Properties { get; init; }
    public required string RelyingParty { get; init; }
    public required string TokenType { get; init; }
}