using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RevoltUltimate.Desktop.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value is bool b)
            {
                flag = b;
            }
            else if (value is bool?)
            {
                bool? nullable = (bool?)value;
                flag = nullable.HasValue ? nullable.Value : false;
            }

            // Handling for string ErrorMessage visibility
            if (value is string strValue && parameter is string strParam && strParam == "NotNullOrEmpty")
            {
                return !string.IsNullOrEmpty(strValue) ? Visibility.Visible : Visibility.Collapsed;
            }

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
