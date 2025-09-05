namespace RevoltUltimate.API.Objects
{
    public class Achievement
    {
        public string name { get; private set; }
        public string description { get; private set; }
        public string imageUrl { get; private set; }
        public bool hidden { get; private set; }
        public int id { get; private set; }
        public bool unlocked { get; private set; }
        public string datetimeunlocked { get; private set; }
        public int difficulty { get; private set; }
        public bool achievementxp { get; private set; }
        public string apiName { get; private set; }

        public bool progress { get; private set; }

        public int currentProgress { get; private set; }
        public int maxProgress { get; private set; }

        public void SetUnlockedStatus(bool isUnlocked, string dateTime)
        {
            this.unlocked = isUnlocked;
            this.datetimeunlocked = isUnlocked ? dateTime : string.Empty;
        }


        public Achievement(string Name, string Description, string ImageUrl, bool Hidden, int Id, bool Unlocked,
            string DateTimeUnlocked, int Difficulty, string apiName, bool progress, int currentProgress, int maxProgress)
        {
            name = Name;
            description = Description;
            imageUrl = ImageUrl;
            hidden = Hidden;
            id = Id;
            unlocked = Unlocked;
            datetimeunlocked = DateTimeUnlocked;
            difficulty = Difficulty;
            achievementxp = false;
            this.apiName = apiName;
            this.progress = progress;
            this.currentProgress = currentProgress;
            this.maxProgress = maxProgress;
        }
    }
}