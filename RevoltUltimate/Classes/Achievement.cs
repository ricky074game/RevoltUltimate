namespace RevoltUltimate.Classes
{
    public class Achievement
    {
        private string name;
        private string description;
        private string image_url;
        private bool hidden;
        private int id;
        private bool unlocked;
        private string datetimeunlocked;
        private string difficulty;

        public Achievement(string name, string description, string image_url, bool hidden, int id, bool unlocked, string datetimeunlocked, string difficulty)
        {
            this.name = name;
            this.description = description;
            this.image_url = image_url;
            this.hidden = hidden;
            this.id = id;
            this.unlocked = unlocked;
            this.datetimeunlocked = datetimeunlocked;
            this.difficulty = difficulty;
        }
    }
}
