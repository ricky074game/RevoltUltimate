using RevoltUltimate.API.Update;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RevoltUltimate.Desktop.Windows
{
    public partial class AboutWindow : Window, INotifyPropertyChanged
    {
        private const string GitHubUrl = "https://github.com/ricky074game/RevoltUltimate";
        private const string IssuesUrl = "https://github.com/ricky074game/RevoltUltimate/issues";
        private const string ContributeUrl = "https://github.com/ricky074game/RevoltUltimate/blob/master/CONTRIBUTING.md";

        private string _updateButtonContent = "Check for Updates";
        private ICommand? _updateCommand;

        public ObservableCollection<Contributor> Contributors { get; } = new();

        public string UpdateButtonContent
        {
            get => _updateButtonContent;
            set
            {
                _updateButtonContent = value;
                OnPropertyChanged(nameof(UpdateButtonContent));
            }
        }

        public ICommand? UpdateCommand
        {
            get => _updateCommand;
            set
            {
                _updateCommand = value;
                OnPropertyChanged(nameof(UpdateCommand));
            }
        }

        public AboutWindow()
        {
            InitializeComponent();
            DataContext = this;
            VersionText.Text = App.CurrentVersion;

            LoadContributors();
            ContributorsPanel.ItemsSource = Contributors;

            CheckForUpdateAsync();
        }

        private void LoadContributors()
        {
            Contributors.Add(new Contributor
            {
                Username = "ricky074game",
                Role = "Lead Developer",
                RoleShort = "Lead",
                RoleColor = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                AvatarUrl = "https://github.com/ricky074game.png",
                GitHubUrl = "https://github.com/ricky074game"
            });

            // For any contributors pull requesting in the future, add them like this:
            // Contributors.Add(new Contributor
            // {
            //     Username = "contributor",
            //     Role = "Contributor",
            //     RoleShort = "Dev",
            //     RoleColor = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            //     AvatarUrl = "https://github.com/contributor.png",
            //     GitHubUrl = "https://github.com/contributor"
            // });
        }

        private async void CheckForUpdateAsync()
        {
            UpdateButtonContent = "Checking...";
            StatusText.Text = "Checking for updates...";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));

            try
            {
                var updateChecker = new UpdateChecker();
                string latestVersion = await updateChecker.GetLatestVersionAsync(App.CurrentVersion);

                if (!UpdateChecker.IsNewerVersion(App.CurrentVersion, latestVersion))
                {
                    UpdateButtonContent = "Up to Date";
                    UpdateButton.IsEnabled = false;
                    StatusText.Text = "You have the latest version";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                    UpdateCommand = null;
                }
                else
                {
                    UpdateButtonContent = $"Update to v{latestVersion}";
                    StatusText.Text = $"New version available: v{latestVersion}";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                    UpdateButton.IsEnabled = true;
                    UpdateCommand = new RelayCommand(() => OpenUrl($"{GitHubUrl}/releases/latest"));
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Update check failed: {ex.Message}");
                UpdateButtonContent = "Retry Check";
                StatusText.Text = "Could not check for updates";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                UpdateButton.IsEnabled = true;
                UpdateCommand = new RelayCommand(CheckForUpdateAsync);
            }
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void Contributor_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string url)
            {
                OpenUrl(url);
            }
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(GitHubUrl);
        }

        private void ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(IssuesUrl);
        }

        private void Contribute_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(ContributeUrl);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class Contributor
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string RoleShort { get; set; } = string.Empty;
        public SolidColorBrush RoleColor { get; set; } = Brushes.Gray;
        public string AvatarUrl { get; set; } = string.Empty;
        public string GitHubUrl { get; set; } = string.Empty;
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}