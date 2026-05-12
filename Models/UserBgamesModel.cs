using Newtonsoft.Json;

namespace bGamesPointsMod.Models
{
    public class UserBgamesModel
    {
        [JsonProperty("id_players")]
        public int IdPlayers { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("age")]
        public int Age { get; set; }

        [JsonProperty("roles")]
        public List<string> Roles { get; set; } = new();
    }
}
