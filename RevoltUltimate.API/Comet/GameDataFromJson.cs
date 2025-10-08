using Newtonsoft.Json;

namespace RevoltUltimate.API.Comet
{
    public class GameDataFromJson
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("appid")]
        public int AppId { get; set; }

        [JsonProperty("achievements")]
        public List<AchievementFromJson> Achievements { get; set; }
    }

    public class AchievementFromJson
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("hidden")]
        public int Hidden { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("unlocked")]
        public bool Unlocked { get; set; }

        [JsonProperty("apiName")]
        public string ApiName { get; set; }

        [JsonProperty("getglobalpercentage")]
        public float GetGlobalPercentage { get; set; }

        [JsonProperty("difficulty")]
        public int Difficulty { get; set; }
    }
}