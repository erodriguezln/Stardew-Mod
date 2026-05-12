using Newtonsoft.Json;

namespace bGamesPointsMod.Models
{
    public class PointsBgamesModel
    {
        [JsonProperty("id_players")]
        public int Id_players { get; set; }

        [JsonProperty("player_name")]
        public string PlayerName { get; set; } = string.Empty;

        [JsonProperty("player_email")]
        public string PlayerEmail { get; set; } = string.Empty;

        [JsonProperty("id_attributes")]
        public int Id_attributes { get; set; }

        [JsonProperty("attribute_name")]
        public string AttributeName { get; set; } = string.Empty;

        [JsonProperty("balance_ledger")]
        public int BalanceLedger { get; set; }

        [JsonProperty("snapshot_points")]
        public int SnapshotPoints { get; set; }

        [JsonProperty("diff_ledger_minus_snapshot")]
        public int DiffLedgerMinusSnapshot { get; set; }
    }
}
