namespace MCaddy.Authentication.Requests;

public record XboxTokenRequest
{
    public record PropertiesField
    {
        public required string AuthMethod { get; init; }
        public required string SiteName { get; init; }
        public required string RpsTicket { get; init; }
    }
    
    public required PropertiesField Properties { get; init; }
    public required string RelyingParty { get; init; }
    public required string TokenType { get; init; }
}