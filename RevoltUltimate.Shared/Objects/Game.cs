using Newtonsoft.Json;

namespace RevoltUltimate.API.Objects
{
    public class Game
    {
        public string name { get; private set; }
        public string platform { get; private set; }
        public string imageUrl { get; private set; }
        public string description { get; private set; }
        public string method { get; private set; }

        public int appid { get; private set; }
        public List<Achievement> achievements { get; private set; }

        public Game(string name, string platform, string imageUrl, string description, string method, int appid)
        {
            this.name = name;
            this.platform = platform;
            this.imageUrl = imageUrl;
            this.description = description;
            this.method = method;
            this.achievements = new List<Achievement>();
            this.appid = appid;
        }

        [JsonConstructor]
        public Game(
            string Name,
            string Platform,
            string ImageUrl,
            string Description,
            string Method,
            List<Achievement> Achievements,
            int AppId)
        {
            this.name = Name;
            this.platform = Platform;
            this.imageUrl = ImageUrl;
            this.description = Description;
            this.method = Method;
            this.achievements = Achievements;
            this.appid = AppId;
        }

        public void AddAchievement(Achievement achievement)
        {
            achievements.Add(achievement);
        }

        public void AddAchievements(List<Achievement> achievements)
        {
            if (!achievements.Any())
            {
                return;
            }
            this.achievements.AddRange(achievements);
        }
    }
}