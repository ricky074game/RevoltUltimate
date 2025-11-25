using System.Text.Json.Serialization;

namespace RevoltUltimate.API.Objects
{
    public class Achievement : ObservableObject
    {
        private string _name;
        public string name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string _description;
        public string description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        private string _imageUrl;
        public string imageUrl
        {
            get => _imageUrl;
            set { _imageUrl = value; OnPropertyChanged(); }
        }

        private bool _hidden;
        public bool hidden
        {
            get => _hidden;
            set { _hidden = value; OnPropertyChanged(); }
        }

        private int _id;
        public int id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        private bool _unlocked;
        public bool unlocked
        {
            get => _unlocked;
            set { _unlocked = value; OnPropertyChanged(); }
        }

        private string? _datetimeunlocked;
        public string? datetimeunlocked
        {
            get => _datetimeunlocked;
            set { _datetimeunlocked = value; OnPropertyChanged(); }
        }

        private int _xp;
        public int xp
        {
            get => _xp;
            set { _xp = value; OnPropertyChanged(); }
        }

        private string _apiName;
        public string apiName
        {
            get => _apiName;
            set { _apiName = value; OnPropertyChanged(); }
        }

        private bool _progress;
        public bool progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        private int _currentProgress;
        public int currentProgress
        {
            get => _currentProgress;
            set { _currentProgress = value; OnPropertyChanged(); }
        }

        private int _maxProgress;
        public int maxProgress
        {
            get => _maxProgress;
            set { _maxProgress = value; OnPropertyChanged(); }
        }

        private float _getglobalpercentage;
        public float getglobalpercentage
        {
            get => _getglobalpercentage;
            set { _getglobalpercentage = value; OnPropertyChanged(); }
        }

        public void SetUnlockedStatus(bool isUnlocked, string? dateTime)
        {
            unlocked = isUnlocked;
            datetimeunlocked = dateTime;
        }


        [JsonConstructor]
        public Achievement(string Name, string Description, string ImageUrl, bool Hidden, int Id, bool Unlocked,
            string? DateTimeUnlocked, int Difficulty, string apiName, bool progress, int currentProgress, int maxProgress, float getglobalpercentage)
        {
            _name = Name;
            _description = Description;
            _imageUrl = ImageUrl;
            _hidden = Hidden;
            _id = Id;
            _unlocked = Unlocked;
            _datetimeunlocked = DateTimeUnlocked;
            _xp = Difficulty == 0 ? CalculateDifficulty(getglobalpercentage) : Difficulty;
            _apiName = apiName;
            _progress = progress;
            _currentProgress = currentProgress;
            _maxProgress = maxProgress;
            _getglobalpercentage = getglobalpercentage;
        }

        private static int CalculateDifficulty(float globalPercentage)
        {
            return globalPercentage switch
            {
                > 90 => 10, // Very Easy (10 XP)
                > 60 => 30, // Easy (30 XP)
                > 40 => 50, // Medium (50 XP)
                > 10 => 90, // Hard (90 XP)
                _ => 200 // Legendary (200 XP)
            };
        }
    }
}