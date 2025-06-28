using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Desktop.Setup.Steps
{
    public partial class WelcomeStep : UserControl
    {
        public static readonly RoutedEvent GoToNextStepEvent = EventManager.RegisterRoutedEvent(
            "GoToNextStep", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WelcomeStep));

        public event RoutedEventHandler GoToNextStep
        {
            add { AddHandler(GoToNextStepEvent, value); }
            remove { RemoveHandler(GoToNextStepEvent, value); }
        }

        public WelcomeStep()
        {
            InitializeComponent();
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(GoToNextStepEvent));
        }
    }
}