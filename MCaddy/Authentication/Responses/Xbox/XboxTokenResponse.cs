namespace MCaddy.Authentication.Responses.Xbox;

public record XboxTokenResponse
{
    public required DateTime IssueInstant { get; init; }
    public required DateTime NotAfter { get; init; }
    public required string Token { get; init; }
    
    public required DisplayClaimsField DisplayClaims { get; init; }

    public record DisplayClaimsField
    {
        public required XuiField[] Xui { get; init; }
    }

    public record XuiField
    {
        public required string Uhs { get; init; }
    }
    
    public bool IsExpired() => DateTime.UtcNow >= NotAfter;
}