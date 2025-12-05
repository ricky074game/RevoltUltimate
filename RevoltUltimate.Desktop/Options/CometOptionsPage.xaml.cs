using RevoltUltimate.API.Comet;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RevoltUltimate.Desktop.Options
{
    public partial class CometOptionsPage : UserControl
    {
        private static readonly string[] RequiredHostEntries =
        {
            "auth.gog.com",
            "users.gog.com",
            "presence.gog.com"
        };

        private static readonly string HostsFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "drivers", "etc", "hosts");

        private const string HostsMarkerStart = "# RevoltUltimate START";
        private const string HostsMarkerEnd = "# RevoltUltimate END";

        private readonly DispatcherTimer _statusTimer;
        private GameConnectedData? _activeGame;

        public CometOptionsPage()
        {
            InitializeComponent();
            Loaded += CometOptionsPage_Loaded;
            Unloaded += CometOptionsPage_Unloaded;

            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _statusTimer.Tick += (s, e) => RefreshAllStatuses();
        }

        private void CometOptionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateHostEntriesPanel();
            RefreshAllStatuses();
            _statusTimer.Start();

            if (App.CometManager?.Service != null)
            {
                App.CometManager.Service.GameConnected += OnGameConnected;
            }
        }

        private void CometOptionsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _statusTimer.Stop();

            if (App.CometManager?.Service != null)
            {
                App.CometManager.Service.GameConnected -= OnGameConnected;
            }
        }

        private void PopulateHostEntriesPanel()
        {
            HostEntriesPanel.Children.Clear();

            foreach (var entry in RequiredHostEntries)
            {
                var entryPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

                var statusDot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(113, 113, 122)),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = entry
                };

                var entryText = new TextBlock
                {
                    Text = $"127.0.0.1    {entry}",
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 170)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12
                };

                entryPanel.Children.Add(statusDot);
                entryPanel.Children.Add(entryText);
                HostEntriesPanel.Children.Add(entryPanel);
            }
        }

        private void RefreshAllStatuses()
        {
            RefreshCometStatus();
            RefreshServiceStatus();
            RefreshHostsStatus();
            UpdateTroubleshooting();
        }

        private void RefreshCometStatus()
        {
            bool isRunning = IsCometProcessRunning();

            if (isRunning)
            {
                CometStatusDot.Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                CometStatusText.Text = "Running";
                StopCometButton.IsEnabled = true;
            }
            else
            {
                CometStatusDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                CometStatusText.Text = "Not running";
                StopCometButton.IsEnabled = false;

                if (_activeGame != null)
                {
                    _activeGame = null;
                    UpdateActiveGameDisplay();
                }
            }
        }

        private void RefreshServiceStatus()
        {
            bool isConnected = App.CometManager?.Service?.IsConnected ?? false;

            if (isConnected)
            {
                ServiceStatusDot.Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                ServiceStatusText.Text = "Connected to Comet service";
            }
            else
            {
                ServiceStatusDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                ServiceStatusText.Text = "Not connected";
            }
        }

        private void RefreshHostsStatus()
        {
            var (allPresent, presentEntries) = CheckHostsFile();

            foreach (var child in HostEntriesPanel.Children)
            {
                if (child is StackPanel panel && panel.Children[0] is Ellipse dot && dot.Tag is string entry)
                {
                    bool isPresent = presentEntries.Contains(entry);
                    dot.Fill = new SolidColorBrush(isPresent
                        ? Color.FromRgb(34, 197, 94)
                        : Color.FromRgb(113, 113, 122));
                }
            }

            if (allPresent)
            {
                HostsStatusDot.Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                HostsStatusText.Text = "All required entries are configured";
                HostsToggle.IsChecked = true;
            }
            else if (presentEntries.Any())
            {
                HostsStatusDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                HostsStatusText.Text = $"Partial configuration ({presentEntries.Count}/{RequiredHostEntries.Length} entries)";
                HostsToggle.IsChecked = false;
            }
            else
            {
                HostsStatusDot.Fill = new SolidColorBrush(Color.FromRgb(113, 113, 122));
                HostsStatusText.Text = "Not configured - GOG Galaxy redirect disabled";
                HostsToggle.IsChecked = false;
            }
        }

        private void UpdateTroubleshooting()
        {
            var issues = new List<string>();

            if (!IsCometProcessRunning())
            {
                issues.Add("• Comet process is not running");
            }

            if (!(App.CometManager?.Service?.IsConnected ?? false))
            {
                issues.Add("• Service connection is not established");
            }

            var (allPresent, _) = CheckHostsFile();
            if (!allPresent)
            {
                issues.Add("• Hosts file entries are missing or incomplete");
            }

            if (issues.Any())
            {
                TroubleshootingCard.Visibility = Visibility.Visible;
                TroubleshootingText.Text = string.Join("\n", issues);
            }
            else
            {
                TroubleshootingCard.Visibility = Visibility.Collapsed;
            }
        }

        private void OnGameConnected(GameConnectedData gameData)
        {
            Dispatcher.Invoke(() =>
            {
                _activeGame = gameData;
                UpdateActiveGameDisplay();
            });
        }

        private void UpdateActiveGameDisplay()
        {
            if (_activeGame != null)
            {
                ActiveGameTitle.Text = $"Game ID: {_activeGame.Id}";
                ActiveGameInfo.Text = "Connected and collecting achievement data";
                GameIconBorder.Background = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                AchievementInfoPanel.Visibility = Visibility.Visible;

                var game = App.CurrentUser?.Games.FirstOrDefault(g => g.appid == _activeGame.Id);
                if (game != null)
                {
                    ActiveGameTitle.Text = game.name;
                    AchievementCountText.Text = $"{game.achievements.Count(a => a.unlocked)}/{game.achievements.Count} achievements";
                }
            }
            else
            {
                ActiveGameTitle.Text = "No game detected";
                ActiveGameInfo.Text = "Waiting for a GOG game to connect...";
                GameIconBorder.Background = new SolidColorBrush(Color.FromRgb(63, 63, 70));
                AchievementInfoPanel.Visibility = Visibility.Collapsed;
            }
        }

        private static bool IsCometProcessRunning()
        {
            return Process.GetProcessesByName("comet").Length > 0;
        }

        private async void RestartComet_Click(object sender, RoutedEventArgs e)
        {
            RestartCometButton.IsEnabled = false;
            RestartCometButton.Content = "Restarting...";

            try
            {
                App.CometManager?.Stop();
                await Task.Delay(1000);

                App.CometManager?.Start(App.CurrentUser?.UserName, App.IsDebugMode);
                await Task.Delay(1500);

                if (App.CometManager != null)
                {
                    await App.CometManager.Service.StartAsync(CancellationToken.None);
                }

                RefreshAllStatuses();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restart Comet: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Trace.WriteLine($"Failed to restart Comet: {ex.Message}");
            }
            finally
            {
                RestartCometButton.Content = "Restart";
                RestartCometButton.IsEnabled = true;
            }
        }

        private void StopComet_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Are you sure you want to stop Comet? Achievement tracking for GOG games will be disabled.",
                "Confirm Stop",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    App.CometManager?.Stop();
                    _activeGame = null;
                    UpdateActiveGameDisplay();
                    RefreshAllStatuses();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to stop Comet: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private static (bool allPresent, HashSet<string> presentEntries) CheckHostsFile()
        {
            var presentEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!File.Exists(HostsFilePath))
                    return (false, presentEntries);

                string content = File.ReadAllText(HostsFilePath);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("#")) continue;

                    foreach (var entry in RequiredHostEntries)
                    {
                        if (trimmedLine.Contains("127.0.0.1") && trimmedLine.Contains(entry))
                        {
                            presentEntries.Add(entry);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error reading hosts file: {ex.Message}");
            }

            bool allPresent = RequiredHostEntries.All(e => presentEntries.Contains(e));
            return (allPresent, presentEntries);
        }

        private void HostsToggle_Click(object sender, RoutedEventArgs e)
        {
            bool enable = HostsToggle.IsChecked == true;

            if (enable)
            {
                var result = MessageBox.Show(
                    "This will add entries to your hosts file to redirect GOG Galaxy domains to localhost.\n\n" +
                    "⚠️ WARNING: Normal GOG Galaxy will stop working while these entries are active.\n\n" +
                    "Do you want to continue?",
                    "Enable Hosts Redirect",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    ModifyHostsFile(true);
                }
                else
                {
                    HostsToggle.IsChecked = false;
                }
            }
            else
            {
                ModifyHostsFile(false);
            }

            RefreshHostsStatus();
            UpdateTroubleshooting();
        }

        private void FixIssues_Click(object sender, RoutedEventArgs e)
        {
            var (allPresent, _) = CheckHostsFile();

            if (!allPresent)
            {
                var result = MessageBox.Show(
                    "This will add the required hosts file entries.\n\n" +
                    "WARNING: Normal GOG Galaxy will stop working while these entries are active.\n\n" +
                    "Do you want to continue?",
                    "Fix Hosts Configuration",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    ModifyHostsFile(true);
                }
            }

            if (!IsCometProcessRunning())
            {
                RestartComet_Click(sender, e);
            }

            RefreshAllStatuses();
        }

        private void ModifyHostsFile(bool addEntries)
        {
            try
            {
                string tempBatchFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "revolt_hosts_mod.bat");
                string tempHostsContent = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "revolt_hosts_content.txt");

                if (addEntries)
                {
                    string currentContent = File.Exists(HostsFilePath) ? File.ReadAllText(HostsFilePath) : "";

                    currentContent = RemoveExistingEntries(currentContent);
                    var newEntries = new List<string>
                    {
                        "",
                        HostsMarkerStart
                    };
                    foreach (var host in RequiredHostEntries)
                    {
                        newEntries.Add($"127.0.0.1    {host}");
                    }
                    newEntries.Add(HostsMarkerEnd);

                    string newContent = currentContent.TrimEnd() + string.Join(Environment.NewLine, newEntries) + Environment.NewLine;
                    File.WriteAllText(tempHostsContent, newContent);
                    string batchContent = $@"@echo off
copy /Y ""{tempHostsContent}"" ""{HostsFilePath}""
del ""{tempHostsContent}""
del ""%~f0""
";
                    File.WriteAllText(tempBatchFile, batchContent);
                }
                else
                {
                    string currentContent = File.Exists(HostsFilePath) ? File.ReadAllText(HostsFilePath) : "";
                    string newContent = RemoveExistingEntries(currentContent);
                    File.WriteAllText(tempHostsContent, newContent);
                    string batchContent = $@"@echo off
copy /Y ""{tempHostsContent}"" ""{HostsFilePath}""
del ""{tempHostsContent}""
del ""%~f0""
";
                    File.WriteAllText(tempBatchFile, batchContent);
                }

                var psi = new ProcessStartInfo
                {
                    FileName = tempBatchFile,
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                try
                {
                    var process = Process.Start(psi);
                    process?.WaitForExit(5000);
                    Thread.Sleep(500);
                    Trace.WriteLine(addEntries ? "Hosts entries added successfully." : "Hosts entries removed successfully.");
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    try { File.Delete(tempBatchFile); } catch { }
                    try { File.Delete(tempHostsContent); } catch { }

                    MessageBox.Show("Administrator privileges are required to modify the hosts file.",
                        "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to modify hosts file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Trace.WriteLine($"Failed to modify hosts file: {ex.Message}");
            }
        }

        private static string RemoveExistingEntries(string content)
        {
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var resultLines = new List<string>();
            bool inRevoltBlock = false;

            foreach (var line in lines)
            {
                if (line.Contains(HostsMarkerStart))
                {
                    inRevoltBlock = true;
                    continue;
                }

                if (line.Contains(HostsMarkerEnd))
                {
                    inRevoltBlock = false;
                    continue;
                }

                if (!inRevoltBlock)
                {
                    bool isOurEntry = false;
                    foreach (var host in RequiredHostEntries)
                    {
                        if (line.Contains("127.0.0.1") && line.Contains(host))
                        {
                            isOurEntry = true;
                            break;
                        }
                    }

                    if (!isOurEntry)
                    {
                        resultLines.Add(line);
                    }
                }
            }

            while (resultLines.Count > 0 && string.IsNullOrWhiteSpace(resultLines[resultLines.Count - 1]))
            {
                resultLines.RemoveAt(resultLines.Count - 1);
            }

            return string.Join(Environment.NewLine, resultLines);
        }
    }
}