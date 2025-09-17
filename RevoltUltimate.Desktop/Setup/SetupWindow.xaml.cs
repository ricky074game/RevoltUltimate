using System.Collections.ObjectModel;
using Microsoft.Win32;
using Newtonsoft.Json;
using RevoltUltimate.API.Objects;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace RevoltUltimate.Desktop.Setup
{
    public partial class SetupWindow : Window
    {
        private int currentStep = 1;
        private Border activePlatformBox = null;
        private const string SetupCompleteEventName = "RevoltUltimateSetupCompleteEvent";
        private EventWaitHandle setupCompleteEvent;
        private ApplicationSettings appSettings = new ApplicationSettings();


        public SetupWindow()
        {
            InitializeComponent();
            Progress1.DataContext = new ProgressViewModel { IsActive = true };
            Progress2.DataContext = new ProgressViewModel { IsActive = false };
            Progress3.DataContext = new ProgressViewModel { IsActive = false };
            Progress4.DataContext = new ProgressViewModel { IsActive = false };
            Progress5.DataContext = new ProgressViewModel { IsActive = false };

            Step3.PfpPreviewContainer.MouseDown += (s, e) => SelectProfilePicture();
            Step4.Settings = this.appSettings;

            try
            {
                setupCompleteEvent = new EventWaitHandle(false, EventResetMode.ManualReset, SetupCompleteEventName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error with setup event: {ex.Message}", "Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void GoToNextStep()
        {
            if (currentStep < 5)
            {
                TransitionToStep(currentStep + 1);
            }
            if (currentStep == 5)
            {
                FinishSetup();
            }
        }

        public void GoToPreviousStep()
        {
            if (currentStep > 1)
            {
                TransitionToStep(currentStep - 1);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Minimize_Click(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TransitionToStep(int step)
        {
            var currentStepElement = (UIElement)FindName($"Step{currentStep}");
            if (currentStepElement != null)
                currentStepElement.Visibility = Visibility.Collapsed;

            var newStepElement = (UIElement)FindName($"Step{step}");
            if (newStepElement != null)
                newStepElement.Visibility = Visibility.Visible;

            var previousProgress = (Border)FindName($"Progress{currentStep}");
            if (previousProgress != null && previousProgress.DataContext is ProgressViewModel prevVm)
            {
                prevVm.IsActive = false;
            }

            var newProgress = (Border)FindName($"Progress{step}");
            if (newProgress != null && newProgress.DataContext is ProgressViewModel newVm)
            {
                newVm.IsActive = true;
            }

            currentStep = step;
        }

        private void SelectProfilePicture()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Profile Picture",
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    Step3.ProfilePictureSource = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Image Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowContextMenu(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (sender is Border platform)
            {
                var platformViewModel = platform.DataContext as PlatformViewModel;
                if (platformViewModel != null && platformViewModel.IsCompleted)
                    return;

                activePlatformBox = platform;
                ContextMenu.IsOpen = true;
            }
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            GoToNextStep();
        }

        private void HideContextMenu(object sender, MouseButtonEventArgs e)
        {
            ContextMenu.IsOpen = false;
        }

        private void LinkAccount(object sender, RoutedEventArgs e)
        {
            if (activePlatformBox != null)
            {
                if (activePlatformBox.DataContext == null)
                {
                    activePlatformBox.DataContext = new PlatformViewModel { IsCompleted = true };
                }
                else if (activePlatformBox.DataContext is PlatformViewModel model)
                {
                    model.IsCompleted = true;
                }
            }

            ContextMenu.IsOpen = false;
        }

        private void FinishSetup()
        {
            SaveUserData();

            setupCompleteEvent?.Set();

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                this.Close();
            };
            timer.Start();
        }

        private void SaveUserData()
        {
            var user = new User
            {
                UserName = Step3.Username,
                Xp = 0,
                Games = new ObservableCollection<Game>()
            };

            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevoltUltimate");

            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            var userFilePath = Path.Combine(appDataFolder, "user.json");
            var userJson = JsonConvert.SerializeObject(user, Formatting.Indented);
            File.WriteAllText(userFilePath, userJson);
            var defaultFolders = new List<string>
            {
                Environment.ExpandEnvironmentVariables(@"%PUBLIC%\Documents\Steam\CODEX"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\Goldberg SteamEmu Saves"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\EMPRESS"),
                Environment.ExpandEnvironmentVariables(@"%PUBLIC%\EMPRESS"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\Steam\Codex"),
                Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\Steam"),
                Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\SKIDROW"),
                Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Documents\SkidRow"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\SmartSteamEmu"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\CreamAPI"),
                Environment.ExpandEnvironmentVariables(@"%APPDATA%\GSE Saves")
            };
            this.appSettings.WatchedFolders = new List<string>();
            this.appSettings.WatchedFolders.AddRange(defaultFolders);
            var settingsFilePath = Path.Combine(appDataFolder, "settings.json");
            var settingsJson = JsonConvert.SerializeObject(this.appSettings, Formatting.Indented);
            File.WriteAllText(settingsFilePath, settingsJson);

            if (Step3.ProfilePictureSource != null)
            {
                var profilePicPath = Path.Combine(appDataFolder, "profile.jpg");
                SaveImageToFile(Step3.ProfilePictureSource as BitmapSource, profilePicPath);
            }
        }

        private void SaveImageToFile(BitmapSource image, string filePath)
        {
            if (image == null) return;

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
        }
    }

    // View Models for data binding
    public class ProgressViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isActive;

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsActive)));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }

    public class PlatformViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isCompleted;

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsCompleted)));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }
}