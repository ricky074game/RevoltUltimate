using Microsoft.Win32;
using RevoltUltimate.API.Contracts;
using RevoltUltimate.API.Objects;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Desktop.Options
{
    public partial class AchievementOptionsPage : UserControl
    {
        public AchievementOptionsPage()
        {
            InitializeComponent();
            LoadDllsIntoListBox();
        }

        private string GetToastsFolderPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Toasts");
        }

        private void LoadDllsIntoListBox()
        {
            string? previouslySelectedDll = DllFilesListBox.SelectedValue as string ?? App.Settings.CustomAnimationDllPath;

            var fileInfos = new List<FileInfo>();
            var uniqueFullPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string toastsFolderPath = GetToastsFolderPath();
            if (Directory.Exists(toastsFolderPath))
            {
                DirectoryInfo toastsDir = new DirectoryInfo(toastsFolderPath);
                foreach (FileInfo file in toastsDir.GetFiles("*.dll"))
                {
                    if (uniqueFullPaths.Add(file.FullName))
                    {
                        fileInfos.Add(file);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(toastsFolderPath);
            }

            string? savedDllPath = App.Settings.CustomAnimationDllPath;
            if (!string.IsNullOrEmpty(savedDllPath) && File.Exists(savedDllPath))
            {
                if (uniqueFullPaths.Add(savedDllPath))
                {
                    fileInfos.Add(new FileInfo(savedDllPath));
                }
            }

            DllFilesListBox.ItemsSource = fileInfos.OrderBy(f => f.Name).ToList();
            DllFilesListBox.DisplayMemberPath = "Name";
            DllFilesListBox.SelectedValuePath = "FullName";

            if (!string.IsNullOrEmpty(previouslySelectedDll) && uniqueFullPaths.Contains(previouslySelectedDll))
            {
                DllFilesListBox.SelectedValue = previouslySelectedDll;
            }
            if (DllFilesListBox.SelectedValue == null && !string.IsNullOrEmpty(App.Settings.CustomAnimationDllPath))
            {
                App.Settings.CustomAnimationDllPath = null;
            }
            else if (DllFilesListBox.SelectedValue is string currentSelection)
            {
                App.Settings.CustomAnimationDllPath = currentSelection;
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "DLL files (*.dll)|*.dll|All files (*.*)|*.*",
                Title = "Select Custom Achievement Notifier DLL"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                App.Settings.CustomAnimationDllPath = openFileDialog.FileName;
                LoadDllsIntoListBox();
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            string? dllPath = DllFilesListBox.SelectedValue as string; // Get full path

            if (string.IsNullOrEmpty(dllPath))
            {
                MessageBox.Show("Please select a DLL file from the list first.", "No DLL Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(dllPath))
            {
                MessageBox.Show($"The selected DLL file was not found:\n{dllPath}", "DLL Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var testAchievement = new Achievement(
                Name: "Test Achievement",
                Description: "This is a test notification!",
                ImageUrl: "pack://application:,,,/RevoltUltimate.Core;component/Resources/default_achievement_icon.png",
                Hidden: false,
                Id: 999,
                Unlocked: true,
                DateTimeUnlocked: DateTime.Now.ToString("o"),
                Difficulty: 0,
                "Test",
                false,
                1,
                1
            );

            AchievementWindow.ShowNotification(testAchievement, dllPath);
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDllsIntoListBox();
        }

        private void DllFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DllFilesListBox.SelectedValue is string selectedDllPath)
            {
                App.Settings.CustomAnimationDllPath = selectedDllPath;
            }
            else
            {
                App.Settings.CustomAnimationDllPath = null;
            }
        }
    }
}