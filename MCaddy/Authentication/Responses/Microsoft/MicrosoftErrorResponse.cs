namespace MCaddy.Authentication.Responses;

public record MicrosoftErrorResponse
{
    public required string Error { get; init; }
    public required string ErrorDescription { get; init; }
    public required string CorrelationId { get; init; }
}