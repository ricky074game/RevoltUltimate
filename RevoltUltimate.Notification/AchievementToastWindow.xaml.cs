using RevoltUltimate.Shared.Objects;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RevoltUltimate.Notification
{
    public partial class AchievementToastWindow : Window
    {
        public AchievementToastWindow()
        {
            InitializeComponent();
            AchievementSound.Play();
            AchievementSound.Stop();
            AchievementSound.Position = TimeSpan.Zero;
        }

        public void Show(Achievement achievement)
        {
            AchievementNameRun.Text = $" - {achievement.Name}";
            AchievementScoreRun.Text = achievement.Difficulty;

            var workingArea = System.Windows.SystemParameters.WorkArea;
            Left = workingArea.Left + (workingArea.Width - Width) / 2;
            Top = workingArea.Top + 20;

            base.Show();

            AchievementSound.Stop();
            AchievementSound.Position = TimeSpan.Zero;
            AchievementSound.Play();

            if (FindResource("EnterStoryboard") is Storyboard enterStoryboard)
            {
                enterStoryboard.Begin();
            }

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4.5) };
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                if (FindResource("ExitStoryboard") is Storyboard exitStoryboard)
                {
                    exitStoryboard.Completed += (s, e) =>
                    {
                        AchievementSound.Stop();
                        Close();
                    };
                    exitStoryboard.Begin();
                }
                else
                {
                    AchievementSound.Stop();
                    Close();
                }
            };
            timer.Start();
        }
    }
}