namespace RevoltUltimate.Classes
{
    public class User
    {
        public string UserName { get; set; }
        public int Xp { get; set; }
        public List<Game> Games { get; set; }
        public int Level => GetLevel();

        public User() { }

        public User(string userName, int xp, List<Game> games)
        {
            UserName = userName;
            Xp = xp;
            Games = games;
        }

        public int GetLevel()
        {
            int level = 1;
            int xpForNextLevel = 100;
            int remainingXp = Xp;

            while (remainingXp >= xpForNextLevel)
            {
                remainingXp -= xpForNextLevel;
                level++;
                xpForNextLevel += 50;
            }

            return level;
        }

        public int GetXpForCurrentLevel()
        {
            int xp = Xp;
            int xpForNextLevel = 100;
            while (xp >= xpForNextLevel)
            {
                xp -= xpForNextLevel;
                xpForNextLevel += 50;
            }
            return xp;
        }

        public int GetXpForNextLevel()
        {
            int level = 1;
            int xpForNextLevel = 100;
            int xp = Xp;
            while (xp >= xpForNextLevel)
            {
                xp -= xpForNextLevel;
                level++;
                xpForNextLevel += 50;
            }
            return xpForNextLevel;
        }
    }
}
