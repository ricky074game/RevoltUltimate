using RevoltUltimate.API.Update;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Desktop.Setup.Steps
{
    public partial class DownloadStep : UserControl
    {
        private bool _isDownloadStarted = false;

        public DownloadStep()
        {
            InitializeComponent();
            IsVisibleChanged += OnIsVisibleChanged;
        }

        private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue && !_isDownloadStarted)
            {
                Trace.WriteLine("STARTING DOWNLOAD");
                _isDownloadStarted = true;
                await StartDownload();
            }
        }

        private async Task StartDownload()
        {
            var downloader = new GitDownloader();
            var repoUrl = "https://github.com/ricky074game/RevoltAchievement";
            var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RevoltAchievement");

            var progressText = new Progress<string>(text => Dispatcher.Invoke(() => ProgressText.Text = text));
            var progressPercentage = new Progress<int>(percentage => Dispatcher.Invoke(() => ProgressBar.Value = percentage));

            if (Directory.Exists(localPath) && Directory.Exists(Path.Combine(localPath, ".git")))
            {
                // If the repository already exists, try to pull the latest changes
                await Task.Run(() => downloader.PullLatestChanges(localPath));
            }
            else
            {
                await downloader.CloneRepositoryAsync(repoUrl, localPath, progressText, progressPercentage);
            }

            // Automatically navigate to the next step on completion
            var parentWindow = Window.GetWindow(this) as SetupWindow;
            parentWindow?.GoToNextStep();
        }
    }
}