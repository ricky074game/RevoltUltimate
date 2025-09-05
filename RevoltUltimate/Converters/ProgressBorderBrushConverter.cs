using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RevoltUltimate.Desktop.Converters
{
    public class ProgressBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? new SolidColorBrush(Color.FromRgb(59, 130, 246)) : new SolidColorBrush(Color.FromRgb(74, 85, 104)); // Active: #3b82f6, Inactive: #4a5568
            }

            return new SolidColorBrush(Color.FromRgb(74, 85, 104)); // Default to inactive color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}