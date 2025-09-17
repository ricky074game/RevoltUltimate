using RevoltUltimate.Desktop.Options;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RevoltUltimate.Desktop.Windows
{
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();
            LoadCategories();
            if (CategoryListBox.Items.Count > 0)
                CategoryListBox.SelectedIndex = 0;
            this.Owner = Application.Current.MainWindow;

        }

        private void LoadCategories()
        {
            CategoryListBox.Items.Add(new OptionsCategory { Name = "General", Content = new GeneralOptionsPage() });
            CategoryListBox.Items.Add(new OptionsCategory { Name = "User Profile", Content = new UserProfileOptionsPage() });
            CategoryListBox.Items.Add(new OptionsCategory { Name = "Achievements", Content = new AchievementOptionsPage() });
            CategoryListBox.Items.Add(new OptionsCategory { Name = "Accounts", Content = new AccountOptionsPage() });
        }
        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryListBox.SelectedItem is OptionsCategory selectedCategory)
            {
                OptionsContentControl.Content = null;
                OptionsContentControl.Content = selectedCategory.Content;
            }
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class OptionsCategory
    {
        public string? Name { get; set; }
        public UserControl? Content { get; set; } // The UserControl for this category's options
    }
}
