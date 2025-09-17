using System.Text.Json.Serialization;

namespace RevoltUltimate.API.Objects
{
    public class SteamAchievement
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("icongray")]
        public string IconGray { get; set; }

        [JsonPropertyName("hidden")]
        public int Hidden { get; set; }
    }
}
