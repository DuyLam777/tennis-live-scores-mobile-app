using System.Globalization;

namespace TennisApp.Converters;

public class DateTimeToTimeSpanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return new TimeSpan(dateTime.Hour, dateTime.Minute, dateTime.Second);
        }
        return new TimeSpan();
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is TimeSpan timeSpan && targetType == typeof(DateTime))
        {
            // Get the current date from the bound property
            var currentDate = DateTime.Now;
            if (parameter is DateTime date)
            {
                currentDate = date;
            }

            // Create a new DateTime with the date portion of the current value and the time from the TimeSpan
            return new DateTime(
                currentDate.Year,
                currentDate.Month,
                currentDate.Day,
                timeSpan.Hours,
                timeSpan.Minutes,
                timeSpan.Seconds
            );
        }
        return DateTime.Now;
    }
}
