using System.Collections.Generic;

namespace RevoltUltimate.Shared.Objects
{
    public class Game
    {
        public string Name { get; private set; }
        public string Platform { get; private set; }
        public string ImageUrl { get; private set; }
        public string Description { get; private set; }
        public string Method { get; private set; }
        public List<Achievement> Achievements { get; private set; }

        public Game(string name, string platform, string imageUrl, string description, string method)
        {
            this.Name = name;
            this.Platform = platform;
            this.ImageUrl = imageUrl;
            this.Description = description;
            this.Method = method;
            this.Achievements = new List<Achievement>();
        }

        public void AddAchievement(Achievement achievement)
        {
            this.Achievements.Add(achievement);
        }
    }
}