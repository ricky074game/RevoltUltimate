namespace RevoltUltimate.Classes
{
    public class Game
    {
        private string name;
        private string platform;
        private string image_url;
        private string description;
        private string method;

        private List<Achievement> achievements;

        public Game(String name, String platform, String image_url, String description, string method)
        {
            this.name = name;
            this.platform = platform;
            this.image_url = image_url;
            this.description = description;
            this.method = method;
            this.achievements = new List<Achievement>();
        }

    }
}
