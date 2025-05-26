using System.Globalization;
using System.Windows.Data;

namespace RevoltUltimate.Converters
{
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 &&
                values[0] is double progress &&
                values[1] is double maxXp &&
                values[2] is double maxWidth &&
                maxXp > 0)
            {
                return (progress / maxXp) * maxWidth;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}