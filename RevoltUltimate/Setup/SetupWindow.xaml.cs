using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using RevoltUltimate.API.Objects;
using System.Collections.Generic;

namespace RevoltUltimate.Desktop.Setup
{
    /// <summary>
    /// Interaction logic for SetupWindow.xaml
    /// </summary>
    public partial class SetupWindow : Window
    {
        private int currentStep = 1;
        private Border activePlatformBox = null;
        private const string SetupCompleteEventName = "RevoltUltimateSetupCompleteEvent";
        private System.Threading.EventWaitHandle setupCompleteEvent;

        public SetupWindow()
        {
            InitializeComponent();
            Progress1.DataContext = new ProgressViewModel { IsActive = true };
            Progress2.DataContext = new ProgressViewModel { IsActive = false };
            Progress3.DataContext = new ProgressViewModel { IsActive = false };

            // Set up profile picture container click event
            PfpPreviewContainer.MouseDown += (s, e) => SelectProfilePicture();
            
            try
            {
                // Try to open the event if it exists
                if (System.Threading.EventWaitHandle.TryOpenExisting(SetupCompleteEventName, out setupCompleteEvent))
                {
                    setupCompleteEvent.Reset();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error with setup event: {ex.Message}", "Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Minimize_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void GoToStep2(object sender, RoutedEventArgs e)
        {
            TransitionToStep(2);
        }

        private void GoToStep3(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameInput.Text))
            {
                MessageBox.Show("Please enter a username.", "Username Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            TransitionToStep(3);
        }

        private void TransitionToStep(int step)
        {
            // Hide current step
            var currentStepElement = (UIElement)FindName($"Step{currentStep}");
            currentStepElement.Visibility = Visibility.Collapsed;
            
            // Show new step
            var newStepElement = (UIElement)FindName($"Step{step}");
            newStepElement.Visibility = Visibility.Visible;
            
            // Update progress circles
            var previousProgress = (Border)FindName($"Progress{currentStep}");
            var newProgress = (Border)FindName($"Progress{step}");
            
            ((ProgressViewModel)previousProgress.DataContext).IsActive = true;
            ((ProgressViewModel)newProgress.DataContext).IsActive = true;
            
            // Update current step
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

                    PfpPreview.Source = bitmap;
                    PfpPreview.Visibility = Visibility.Visible;
                    PfpPlaceholder.Visibility = Visibility.Collapsed;
                    
                    // Update border style - WPF doesn't have BorderStyle, so we'll adjust BorderThickness and BorderBrush instead
                    PfpPreviewContainer.BorderThickness = new Thickness(3);
                    PfpPreviewContainer.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // #3b82f6
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
            
            // If the platform is already "linked", do nothing
            if (sender is Border platform)
            {
                var platformViewModel = platform.DataContext as PlatformViewModel;
                if (platformViewModel != null && platformViewModel.IsCompleted)
                    return;
                
                activePlatformBox = platform;
                ContextMenu.IsOpen = true;
            }
        }

        private void HideContextMenu(object sender, MouseButtonEventArgs e)
        {
            ContextMenu.IsOpen = false;
        }

        private void LinkAccount(object sender, RoutedEventArgs e)
        {
            if (activePlatformBox != null)
            {
                // If the platform doesn't have a view model, create one
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

        private void FinishSetup(object sender, RoutedEventArgs e)
        {
            // Save user data
            SaveUserData();
            
            // Show completion screen
            TransitionToStep(4);
            
            // Signal setup completion
            if (setupCompleteEvent != null)
            {
                setupCompleteEvent.Set();
            }
            else
            {
                try
                {
                    // Create the event if it doesn't exist
                    setupCompleteEvent = new System.Threading.EventWaitHandle(
                        false, 
                        System.Threading.EventResetMode.ManualReset, 
                        SetupCompleteEventName);
                    
                    setupCompleteEvent.Set();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error signaling setup completion: {ex.Message}", "Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            
            // Close the window after a delay
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, args) => {
                timer.Stop();
                this.Close();
            };
            timer.Start();
        }

        private void SaveUserData()
        {
            var user = new User
            {
                UserName = UsernameInput.Text,
                Xp = 0,
                Games = new List<Game>()
            };
            
            // Ensure directory exists
            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevoltUltimate");
            
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }
            
            // Save user data
            var userFilePath = Path.Combine(appDataFolder, "user.json");
            var json = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(userFilePath, json);
            
            // Save profile picture if selected
            if (PfpPreview.Source != null)
            {
                var profilePicPath = Path.Combine(appDataFolder, "profile.jpg");
                SaveImageToFile(PfpPreview.Source as BitmapSource, profilePicPath);
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
