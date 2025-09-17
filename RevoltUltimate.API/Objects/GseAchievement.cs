using Newtonsoft.Json;

namespace RevoltUltimate.API.Objects
{
    public class GseAchievement
    {
        [JsonProperty("earned")]
        public bool Earned { get; set; }
        [JsonProperty("earned_time")]
        public long EarnedTime { get; set; }
    }
}
