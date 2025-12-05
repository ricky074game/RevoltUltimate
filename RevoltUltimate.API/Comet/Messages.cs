using Newtonsoft.Json;

namespace RevoltUltimate.API.Comet
{
    // Incoming Messages
    public class CometMessage
    {
        [JsonProperty("type")]
        public string? Type { get; set; }
    }

    public class GameConnectedData
    {
        [JsonProperty("game_id")]
        public long Id { get; set; }
    }

    public class AchievementUnlockedData
    {
        [JsonProperty("achievement_id")]
        public string ApiName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("unlocked_at")]
        public long UnlockedAt { get; set; }

        [JsonProperty("image_url_unlocked")]
        public string ImageUrl { get; set; }
    }


    // Outgoing Messages

    public class AchievementListData
    {
        [JsonProperty("achievement_id")]
        public string? AchievementId { get; set; }

        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("unlocked_at")]
        public object? UnlockedAt { get; set; }

        [JsonProperty("image_url_unlocked")]
        public string? ImageUrlUnlocked { get; set; }

        [JsonProperty("image_url_locked")]
        public string? ImageUrlLocked { get; set; }
    }

    public class AchievementsListMessage
    {
        [JsonProperty("type")]
        public string Type { get; } = "achievements_list";

        [JsonProperty("game_id")]
        public long GameId { get; set; }

        [JsonProperty("data")]
        public List<AchievementListData>? Data { get; set; }
    }
}