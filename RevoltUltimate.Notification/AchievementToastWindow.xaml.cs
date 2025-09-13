using RevoltUltimate.API.Contracts;
using RevoltUltimate.API.Objects;
using System.Media;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RevoltUltimate.Notification
{
    public partial class AchievementToastWindow : Window, IAchievementNotifier
    {
        private static readonly Queue<Achievement> _achievementQueue = new();
        private static readonly object _queueLock = new object();
        private static bool _isNotificationVisible = false;
        private static readonly SoundPlayer AchievementSound = new(Properties.Resources.achievement);

        private Achievement _currentAchievement;

        static AchievementToastWindow()
        {

        }

        public AchievementToastWindow()
        {
            InitializeComponent();
        }

        private AchievementToastWindow(Achievement achievement) : this()
        {
            _currentAchievement = achievement;
            this.DataContext = achievement;
            this.Closed += OnDisplayedToastClosed;
        }

        public void ShowAchievement(Achievement achievement)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_queueLock)
                {
                    _achievementQueue.Enqueue(achievement);
                }
                TryDisplayNext();
            });
        }

        private static void TryDisplayNext()
        {
            lock (_queueLock)
            {
                if (!_isNotificationVisible && _achievementQueue.Count > 0)
                {
                    var achievementToDisplay = _achievementQueue.Dequeue();
                    _isNotificationVisible = true;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var toastWindow = new AchievementToastWindow(achievementToDisplay);
                        toastWindow.ShowAndAnimate();
                    });
                }
            }
        }

        private void ShowAndAnimate()
        {
            var workingArea = SystemParameters.WorkArea;
            this.Left = workingArea.Left + (workingArea.Width - this.Width) / 2;
            this.Top = workingArea.Top + 20;

            base.Show();

            AchievementSound.Play();

            if (FindResource("EnterStoryboard") is Storyboard enterStoryboard)
            {
                enterStoryboard.Begin(this);
            }

            var displayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4.5) };
            displayTimer.Tick += (sender, args) =>
            {
                displayTimer.Stop();
                StartExitSequence();
            };
            displayTimer.Start();
        }

        private void StartExitSequence()
        {
            if (FindResource("ExitStoryboard") is Storyboard exitStoryboard)
            {
                exitStoryboard.Completed += (s, e) => this.Close();
                exitStoryboard.Begin(this);
            }
            else
            {
                this.Close();
            }
        }

        private void OnDisplayedToastClosed(object sender, EventArgs e)
        {
            lock (_queueLock)
            {
                _isNotificationVisible = false;
            }
            TryDisplayNext();
        }
    }
}