using Newtonsoft.Json;

namespace bGamesPointsMod.Models
{
    public class HealthResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    }
}

