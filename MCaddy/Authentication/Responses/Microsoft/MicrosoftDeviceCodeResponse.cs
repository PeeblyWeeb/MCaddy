namespace MCaddy.Authentication.Responses;

public record MicrosoftDeviceCodeResponse
{
    public required string UserCode { get; init; }
    public required string DeviceCode { get; init; }
    public required string VerificationUri { get; init; }
    public required int Interval { get; init; }
    public required int ExpiresIn { get; init; }
}