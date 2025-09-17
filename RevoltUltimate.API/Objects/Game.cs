using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace RevoltUltimate.API.Objects
{
    public class Game : ObservableObject
    {
        private string _name;
        public string name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string platform { get; set; }

        private string _imageUrl;
        public string imageUrl
        {
            get => _imageUrl;
            set { _imageUrl = value; OnPropertyChanged(); }
        }

        private string _description;
        public string description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string method { get; set; }
        public int appid { get; set; }

        private ObservableCollection<Achievement> _achievements;
        public ObservableCollection<Achievement> achievements
        {
            get => _achievements;
            set
            {
                if (_achievements != null)
                {
                    _achievements.CollectionChanged -= OnAchievementsChanged;
                    foreach (var ach in _achievements)
                    {
                        ach.PropertyChanged -= OnAchievementPropertyChanged;
                    }
                }

                _achievements = value;

                if (_achievements != null)
                {
                    _achievements.CollectionChanged += OnAchievementsChanged;
                    foreach (var ach in _achievements)
                    {
                        ach.PropertyChanged += OnAchievementPropertyChanged;
                    }
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(AchievementSummary));
            }
        }

        public string? trackedFilePath { get; set; }

        public string AchievementSummary
        {
            get
            {
                if (achievements == null || !achievements.Any())
                {
                    return "Achievements: 0/0, 0%";
                }
                int unlocked = achievements.Count(a => a.unlocked);
                int total = achievements.Count;
                string percentText = total > 0 ? $"{(unlocked * 100 / total)}%" : "0%";
                return $"Achievements: {unlocked}/{total}, {percentText}";
            }
        }

        public Game(string name, string platform, string imageUrl, string description, string method, int appid)
        {
            this._name = name;
            this.platform = platform;
            this._imageUrl = imageUrl;
            this._description = description;
            this.method = method;
            this.appid = appid;
            this.achievements = new ObservableCollection<Achievement>();
        }

        [JsonConstructor]
        public Game(string name, string platform, string imageUrl, string description, string method, int appid, ObservableCollection<Achievement> achievements, string? trackedFilePath)
        {
            this._name = name;
            this.platform = platform;
            this._imageUrl = imageUrl;
            this._description = description;
            this.method = method;
            this.appid = appid;
            this.achievements = achievements ?? new ObservableCollection<Achievement>();
            this.trackedFilePath = trackedFilePath;
        }

        private void OnAchievementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<Achievement>())
                {
                    item.PropertyChanged += OnAchievementPropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<Achievement>())
                {
                    item.PropertyChanged -= OnAchievementPropertyChanged;
                }
            }
            OnPropertyChanged(nameof(AchievementSummary));
        }

        private void OnAchievementPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Achievement.unlocked))
            {
                OnPropertyChanged(nameof(AchievementSummary));
            }
        }

        public void AddAchievement(Achievement achievement)
        {
            achievements.Add(achievement);
        }

        public void AddAchievements(IEnumerable<Achievement> achievementsToAdd)
        {
            foreach (var achievement in achievementsToAdd)
            {
                this.achievements.Add(achievement);
            }
        }
    }
}