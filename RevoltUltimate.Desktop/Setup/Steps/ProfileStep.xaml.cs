using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RevoltUltimate.Desktop.Setup.Steps
{
    public partial class ProfileStep : UserControl
    {
        public static readonly RoutedEvent GoToNextStepEvent = EventManager.RegisterRoutedEvent(
            "GoToNextStep", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ProfileStep));

        public event RoutedEventHandler GoToNextStep
        {
            add { AddHandler(GoToNextStepEvent, value); }
            remove { RemoveHandler(GoToNextStepEvent, value); }
        }

        public string Username => UsernameInput.Text;
        public ImageSource ProfilePictureSource
        {
            get => PfpPreview.Source;
            set
            {
                PfpPreview.Source = value;
                PfpPreview.Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;
                PfpPlaceholder.Visibility = value == null ? Visibility.Visible : Visibility.Collapsed;

                if (value != null)
                {
                    PfpPreviewContainer.BorderThickness = new Thickness(3);
                    PfpPreviewContainer.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // #3b82f6
                }
            }
        }

        public ProfileStep()
        {
            InitializeComponent();
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(GoToNextStepEvent));
        }
    }
}