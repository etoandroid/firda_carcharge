using System.Text.Json.Serialization;

namespace imicCharge.API.Models;

// Represents the response from the Easee authentication endpoint
public class EaseeTokenResponse
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("tokenType")]
    public string? TokenType { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }
}