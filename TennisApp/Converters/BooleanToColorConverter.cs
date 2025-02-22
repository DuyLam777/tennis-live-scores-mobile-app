using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace TennisApp.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Colors.Blue : Colors.Green; // Blue for sent, Green for received
            }
            throw new InvalidOperationException("Value must be a boolean");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}