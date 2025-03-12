using System.Globalization;

namespace TennisApp.Converters;

public class BoolToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isTrue)
        {
            if (parameter is string param && param.Contains(','))
            {
                var strings = param.Split(',');
                if (strings.Length >= 2)
                {
                    return isTrue ? strings[0] : strings[1];
                }
            }
            return isTrue ? "Yes" : "No";
        }
        return string.Empty;
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
