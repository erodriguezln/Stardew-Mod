using Newtonsoft.Json;

namespace bGamesPointsMod.Models
{
    public class SaveSessionModel
    {
        [JsonProperty("access_token")] public string AccessToken { get; set; }
        [JsonProperty("expires_at")] public DateTime ExpiresAt { get; set; }
    }
}