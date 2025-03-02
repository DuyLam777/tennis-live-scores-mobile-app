using System.Globalization;
using Microsoft.Maui.Controls;

namespace TennisApp.Converters
{
    public class BooleanToLayoutOptionsConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            return (value is bool isSent && isSent) ? LayoutOptions.End : LayoutOptions.Start;
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            throw new NotImplementedException();
        }
    }
}
