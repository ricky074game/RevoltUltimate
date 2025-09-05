using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RevoltUltimate.API.Fetcher;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System;
using System.Windows.Media;

namespace RevoltUltimate.Desktop
{
    public partial class GameSearchWindow : Window
    {
        private readonly IGDB _igdb;
        private Task? _searchTask;
        private bool _isWindowLoaded = false;

        public GameSearchWindow()
        {
            InitializeComponent();
            _igdb = new IGDB();
            this.Owner = Application.Current.MainWindow;
            this.Loaded += GameSearchWindow_Loaded;
        }

        private void GameSearchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isWindowLoaded = true;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Close(); // Close the window when it loses activation
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isWindowLoaded) return;

            if (_searchTask?.IsCompleted == false)
            {
                await _searchTask;
            }

            string query = SearchTextBox.Text;
            if (string.IsNullOrWhiteSpace(query) || query == "Search for a game...")
            {
                ResultsListBox.ItemsSource = null;
                HideLoadingCircle();
                return;
            }

            ShowLoadingCircle();
            _searchTask = PerformSearch(query);
            await _searchTask;
            HideLoadingCircle();
        }

        private async Task PerformSearch(string query)
        {
            var results = await _igdb.SearchGamesAsync(query);
            ResultsListBox.ItemsSource = results;
        }

        private void ResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultsListBox.SelectedItem != null)
            {
                MessageBox.Show($"'{((dynamic)ResultsListBox.SelectedItem).Name}' selected. Implement adding logic here.", "Game Selected");
                this.Close();
            }
        }

        private void ShowLoadingCircle()
        {
            LoadingCircle.Visibility = Visibility.Visible;

            var rotateAnimation = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(1)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            LoadingCircleRotation.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        private void HideLoadingCircle()
        {
            LoadingCircle.Visibility = Visibility.Collapsed;
            LoadingCircleRotation.BeginAnimation(RotateTransform.AngleProperty, null); // Stop animation
        }
    }
}