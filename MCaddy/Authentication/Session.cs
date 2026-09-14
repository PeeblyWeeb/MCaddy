using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Text.Json;
using MCaddy.Authentication.Requests;
using MCaddy.Authentication.Requests.Minecraft;
using MCaddy.Authentication.Responses;
using MCaddy.Authentication.Responses.Minecraft;
using MCaddy.Authentication.Responses.Xbox;
using MCaddy.Util;

namespace MCaddy.Authentication;

internal class Session
{
    private readonly HttpClient _http;
    private readonly Logger _logger;

    private readonly JsonSerializerOptions _microsoftSerializerOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    
    # region Authentication State

    public record AuthenticationState
    {
        public MicrosoftTokenResponse? MicrosoftTokenResponse { get; set; }
        public XboxTokenResponse? XboxTokenResponse { get; set; }
        public XboxTokenResponse? XboxXstsTokenResponse { get; set; }
        public MinecraftTokenResponse? MinecraftTokenResponse { get; set; }
    }
    public AuthenticationState State = new();
    public MinecraftGameProfileResponse GameProfile = null!;
    
    # endregion

    internal Session()
    {
        _logger = new Logger("Session");

        SocketsHttpHandler httpHandler = new()
        {
            SslOptions = new()
            {
                AllowRenegotiation = true,
            }
        };
        
        _http = new HttpClient(httpHandler);
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
        MicrosoftDeviceCodeResponse content = (await response.Content.ReadFromJsonAsync<MicrosoftDeviceCodeResponse>(_microsoftSerializerOptions))!;
        
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
            if (!pollResponse.IsSuccessStatusCode)
            {
                var pollContent = (await pollResponse.Content.ReadFromJsonAsync<MicrosoftErrorResponse>(_microsoftSerializerOptions))!;
                if (pollContent.Error != "authorization_pending")
                    throw new InvalidOperationException($"An unrecognized error was returned by the API: {pollContent.Error}: {pollContent.ErrorDescription}");
                
                await Task.Delay(content.Interval * 1000);
                continue;
            }
            
            State.MicrosoftTokenResponse = (await pollResponse.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(_microsoftSerializerOptions))!;
            break;
        }

        if (State.MicrosoftTokenResponse == null)
            throw new TimeoutException("Timed out waiting for Microsoft authorization.");

        await SaveAuthenticationState();
    }

    private async Task LoginWithXbox()
    {
        if (State.MicrosoftTokenResponse == null)
            throw new InvalidOperationException("Cannot login with XBOX before logging in with Microsoft");

        var request = new XboxTokenRequest()
        {
            Properties = new()
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"{State.MicrosoftTokenResponse.AccessToken}",
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://user.auth.xboxlive.com/user/authenticate"
        );
        httpRequest.Content = JsonContent.Create(
            request,
            options: new JsonSerializerOptions()
            {
                PropertyNamingPolicy = null
            }
        );
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var response = await _http.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to get Xbox Access Token: {response.StatusCode}");

        State.XboxTokenResponse = await response.Content.ReadFromJsonAsync<XboxTokenResponse>();
        await SaveAuthenticationState();
    }

    private async Task GetXboxXsts()
    {
        if (State.XboxTokenResponse == null)
            throw new InvalidOperationException("Cannot get Xbox Xsts Token before logging in with Xbox Live");

        var request = new XboxXstsTokenRequest()
        {
            Properties = new()
            {
                SandboxId = "RETAIL",
                UserTokens = [State.XboxTokenResponse.Token],
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };
        
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://xsts.auth.xboxlive.com/xsts/authorize"
        );
        httpRequest.Content = JsonContent.Create(
            request,
            options: new JsonSerializerOptions()
            {
                PropertyNamingPolicy = null
            }
        );
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var response = await _http.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to get Xbox Xsts Token: {response.StatusCode}");

        State.XboxXstsTokenResponse = await response.Content.ReadFromJsonAsync<XboxTokenResponse>();
        await SaveAuthenticationState();
    }

    private async Task LoginWithMinecraft()
    {
        if (State.XboxXstsTokenResponse == null)
            throw new InvalidOperationException("Cannot login with Minecraft before getting Xbox Xsts Token");
        
        var request = new MinecraftTokenRequest()
        {
            IdentityToken = $"XBL3.0 x={State.XboxXstsTokenResponse.DisplayClaims.Xui[0].Uhs};{State.XboxXstsTokenResponse.Token}"
        };
        
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.minecraftservices.com/authentication/login_with_xbox"
        );
        httpRequest.Content = JsonContent.Create(
            request,
            options: new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        );
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var response = await _http.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to get Minecraft Token: {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        State.MinecraftTokenResponse = await response.Content.ReadFromJsonAsync<MinecraftTokenResponse>(new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
        await SaveAuthenticationState();
    }

    private async Task GetMinecraftGameProfile()
    {
        if (State.MinecraftTokenResponse == null)
            throw new InvalidOperationException("Cannot get Minecraft Game Profile before getting Minecraft Token");

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.minecraftservices.com/minecraft/profile"
        );
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(State.MinecraftTokenResponse.TokenType, State.MinecraftTokenResponse.AccessToken);
        
        var response = await _http.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to get Minecraft Game Profile: {response.StatusCode}");
        
        GameProfile = (await response.Content.ReadFromJsonAsync<MinecraftGameProfileResponse>(new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }))!;
    }
    
    internal async Task Login()
    {
        await LoadAuthenticationState();
        if (State.MicrosoftTokenResponse == null || State.MicrosoftTokenResponse.IsExpired()) 
            await LoginWithMicrosoft();
        if (State.XboxTokenResponse == null || State.XboxTokenResponse.IsExpired())
            await LoginWithXbox();
        if (State.XboxXstsTokenResponse == null || State.XboxXstsTokenResponse.IsExpired())
            await GetXboxXsts();
        if (State.MinecraftTokenResponse == null || State.MinecraftTokenResponse.IsExpired())
            await LoginWithMinecraft();
        await GetMinecraftGameProfile();
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

    private async Task LoadAuthenticationState()
    {
        try
        {
            if (File.Exists(MCaddy.AuthCacheFilePath))
                State = JsonSerializer.Deserialize<AuthenticationState>(await File.ReadAllTextAsync(MCaddy.AuthCacheFilePath))!;
        } catch (JsonException ex)
        {
            _logger.Log($"Failed to load auth state from file: {ex}", Logger.LogLevel.Error);
        }
    }
}