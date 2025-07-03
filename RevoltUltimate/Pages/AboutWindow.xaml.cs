using RevoltUltimate.API.Update;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace RevoltUltimate.Desktop.Pages
{
    public partial class AboutWindow : Window, INotifyPropertyChanged
    {
        private string _updateButtonContent;
        private ICommand _updateCommand;

        public string UpdateButtonContent
        {
            get => _updateButtonContent;
            set
            {
                _updateButtonContent = value;
                OnPropertyChanged(nameof(UpdateButtonContent));
            }
        }

        public ICommand UpdateCommand
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

            CheckForUpdateAsync();
        }

        private async void CheckForUpdateAsync()
        {
            string currentVersion = "0.0.1";
            UpdateChecker updateChecker = new UpdateChecker();
            string latestVersion = await updateChecker.GetLatestVersionAsync(currentVersion);

            if (latestVersion == currentVersion)
            {
                UpdateButtonContent = "✔ Up to Date";
                UpdateCommand = null;
            }
            else
            {
                UpdateButtonContent = "Update Available";
                UpdateCommand = new RelayCommand(() =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/ricky074game/RevoltUltimate/releases",
                        UseShellExecute = true
                    });
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Minimize_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => _execute();
    }
}