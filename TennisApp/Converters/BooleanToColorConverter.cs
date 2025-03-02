using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace TennisApp.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is bool boolValue)
            {
                var app = Application.Current;
                if (
                    app != null
                    && app.Resources.TryGetValue("Primary", out var sentColor)
                    && app.Resources.TryGetValue("Secondary", out var receivedColor)
                )
                {
                    return boolValue ? (Color)sentColor : (Color)receivedColor;
                }
                throw new InvalidOperationException("Colors not found in resources");
            }
            throw new InvalidOperationException("Value must be a boolean");
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
