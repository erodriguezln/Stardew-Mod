using Newtonsoft.Json;

namespace bGamesPointsMod.Models
{
    public class TokenModel
    {
        [JsonProperty("expires_in_seconds")] public int ExpiresInSeconds { get; set; }
        [JsonProperty("expires_at")] public DateTime ExpiresAt { get; set; }
        [JsonProperty("issued_at")] public DateTime? IssuedAt { get; set; }
    }
}