namespace RevoltUltimate.API.Objects
{
    public class Achievement(string name, string description, string imageUrl, bool hidden, int id, bool unlocked, string dateTimeUnlocked, string difficulty)
    {
        public string Name { get; private set; } = name;
        public string Description { get; private set; } = description;
        public string ImageUrl { get; private set; } = imageUrl;
        public bool Hidden { get; private set; } = hidden;
        public int Id { get; private set; } = id;
        public bool Unlocked { get; private set; } = unlocked;
        public string DateTimeUnlocked { get; private set; } = dateTimeUnlocked;
        public string Difficulty { get; private set; } = difficulty;

        public void SetUnlockedStatus(bool isUnlocked, string dateTime)
        {
            this.Unlocked = isUnlocked;
            this.DateTimeUnlocked = isUnlocked ? dateTime : string.Empty;
        }
    }
}