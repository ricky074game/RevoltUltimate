using System.Collections.ObjectModel;

namespace RevoltUltimate.API.Objects
{
    public class User : ObservableObject
    {
        public string UserName { get; set; }

        private int _xp;
        public int Xp
        {
            get => _xp;
            set
            {
                _xp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Level));
            }
        }

        public ObservableCollection<Game> Games { get; set; }

        public int Level => GetLevel();

        public User()
        {
            Games = new ObservableCollection<Game>();
        }

        public User(string userName, int xp, ObservableCollection<Game> games)
        {
            UserName = userName;
            Xp = xp;
            Games = games ?? new ObservableCollection<Game>();
        }

        public int GetLevel()
        {
            return (int)(0.1 * Math.Sqrt(Xp));
        }

        public int GetXpForCurrentLevel()
        {
            int level = GetLevel();
            return Xp - (int)(Math.Pow(level / 0.1, 2));
        }

        public int GetXpForNextLevel()
        {
            int level = GetLevel();
            int nextLevelXp = (int)(Math.Pow((level + 1) / 0.1, 2));
            int currentLevelXp = (int)(Math.Pow(level / 0.1, 2));
            return nextLevelXp - currentLevelXp;
        }
    }
}