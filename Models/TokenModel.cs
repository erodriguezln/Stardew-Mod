using Newtonsoft.Json;

namespace bGamesPointsMod.Models
{
    public class TokenModel
    {
        [JsonProperty("expires_in_seconds")] public int ExpiresInSeconds { get; set; }
        [JsonProperty("expires_at")] public DateTime ExpiresAt { get; set; }
        [JsonProperty("issued_at")] public DateTime? IssuedAt { get; set; }
        [JsonProperty("player_id")] public int? PlayerId { get; set; }
        [JsonProperty("roles")] public List<string> Roles { get; set; } = new();
    }
}
