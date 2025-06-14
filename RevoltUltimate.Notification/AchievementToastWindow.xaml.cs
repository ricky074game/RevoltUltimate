using RevoltUltimate.API.Contracts; // For IAchievementNotifier
using RevoltUltimate.API.Objects;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RevoltUltimate.Notification
{
    public partial class AchievementToastWindow : Window, IAchievementNotifier
    {
        private static readonly Queue<Achievement> _achievementQueue = new Queue<Achievement>();
        private static readonly object _queueLock = new object();
        private static bool _isNotificationVisible = false;

        private Achievement _currentAchievement;

        public AchievementToastWindow()
        {
            InitializeComponent();
        }

        private AchievementToastWindow(Achievement achievement) : this()
        {
            _currentAchievement = achievement;
            this.Closed += OnDisplayedToastClosed;
        }


        public void ShowAchievement(Achievement achievement)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => EnqueueAndAttemptDisplay(achievement));
            }
            else
            {
                EnqueueAndAttemptDisplay(achievement);
            }
        }

        private void EnqueueAndAttemptDisplay(Achievement achievement)
        {
            lock (_queueLock)
            {
                _achievementQueue.Enqueue(achievement);
            }
            TryDisplayNext();
        }

        private static void TryDisplayNext()
        {
            Achievement achievementToDisplay = null;
            bool canDisplay = false;

            lock (_queueLock)
            {
                if (!_isNotificationVisible && _achievementQueue.Count > 0)
                {
                    achievementToDisplay = _achievementQueue.Dequeue();
                    _isNotificationVisible = true;
                    canDisplay = true;
                }
            }

            if (canDisplay && achievementToDisplay != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AchievementToastWindow toastWindow = new AchievementToastWindow(achievementToDisplay);
                    toastWindow.ShowAndAnimate();
                });
            }
        }

        private void ShowAndAnimate()
        {
            AchievementNameRun.Text = $" - {_currentAchievement.name}";
            AchievementScoreRun.Text = _currentAchievement.difficulty;

            var workingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = workingArea.Left + (workingArea.Width - this.Width) / 2;
            this.Top = workingArea.Top + 20;

            base.Show();

            AchievementSound.Stop();
            AchievementSound.Position = TimeSpan.Zero;
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
                exitStoryboard.Completed += (s, e) =>
                {
                    AchievementSound.Stop();
                    this.Close();
                };
                exitStoryboard.Begin(this);
            }
            else
            {
                AchievementSound.Stop();
                this.Close();
            }
        }

        private void OnDisplayedToastClosed(object sender, EventArgs e)
        {
            this.Closed -= OnDisplayedToastClosed;

            lock (_queueLock)
            {
                _isNotificationVisible = false;
            }
            TryDisplayNext();
        }
    }
}