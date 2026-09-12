using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MCaddy.Authentication.Responses;
using MCaddy.Util;

namespace MCaddy.Authentication;

internal class Session
{
    private readonly HttpClient _http;
    private readonly Logger _logger;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    
    # region Authentication State

    public record AuthenticationState
    {
        public MicrosoftTokenResponse? MicrosoftTokenResponse { get; set; }
    }
    public AuthenticationState State = new();
    
    # endregion

    internal Session()
    {
        _logger = new Logger("Session");
        
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "MinecraftLauncher/2.2.10675");
    }

    private async Task LoginWithMicrosoft()
    {
        using var body = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            { "client_id", MCaddy.Properties.MicrosoftClientId },
            { "scope", "service::user.auth.xboxlive.com::MBI_SSL" },
            { "response_type", "device_code" }
        });
        var response = await _http.PostAsync("https://login.live.com/oauth20_connect.srf", body);
        MicrosoftDeviceCodeResponse content = (await response.Content.ReadFromJsonAsync<MicrosoftDeviceCodeResponse>(_serializerOptions))!;
        
        _logger.Log($"""
                    Login to your Microsoft account with Minecraft Java Edition.
                    
                    Visit: {content.VerificationUri}
                    Enter Code: {content.UserCode}
                    """);
        _logger.Log($"Polling interval: {content.Interval}", Logger.LogLevel.Debug);

        var pollingBody = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
            { "client_id", MCaddy.Properties.MicrosoftClientId },
            { "device_code", content.DeviceCode }
        });
        
        var pollingStartTime = DateTime.Now;
        while ((DateTime.Now - pollingStartTime).TotalSeconds < content.ExpiresIn)
        {
            var pollResponse = await _http.PostAsync("https://login.live.com/oauth20_token.srf", pollingBody);
            if (pollResponse.IsSuccessStatusCode)
            {
                State.MicrosoftTokenResponse = (await pollResponse.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(_serializerOptions))!;
                break;
            }
            else
            {
                var pollContent = (await pollResponse.Content.ReadFromJsonAsync<MicrosoftErrorResponse>(_serializerOptions))!;
                if (pollContent.Error != "authorization_pending")
                {
                    _logger.Log($"""
                                 An unrecognized error '{pollContent.Error}' was returned by the API."
                                 ⤷ {pollContent.ErrorDescription}
                                 """, Logger.LogLevel.Error);
                    break;
                }
            }
            
            await Task.Delay(content.Interval * 1000);
        }

        if (State.MicrosoftTokenResponse == null)
        {
            Environment.Exit(1);
        }

        await SaveAuthenticationState();
    }

    private async Task LoginWithXbox()
    {
        if (State.MicrosoftTokenResponse == null)
            throw new InvalidOperationException("Cannot login with XBOX before logging in with Microsoft");
        throw new NotImplementedException();
    }

    private async Task LoginWithMinecraft()
    {
        throw new NotImplementedException();
    }
    
    internal async Task Login()
    {
        await LoginWithMicrosoft();
        await LoginWithXbox();
        await LoginWithMinecraft();
    }

    private async Task SaveAuthenticationState()
    {
        _logger.Log($"Saving authentication state to '{MCaddy.AuthCacheFilePath}'");

        await using FileStream stream = new(MCaddy.AuthCacheFilePath, FileMode.Create, FileAccess.Write);
        stream.Write(JsonSerializer.SerializeToUtf8Bytes(State, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4
        }));
    }
}