using System;

namespace TennisApp.Utils;

public static class ColorHelpers
{
    public static Color GetResourceColor(string resourceKey)
    {
        if (Application.Current?.Resources != null)
        {
            if (Application.Current.Resources.TryGetValue(resourceKey, out var resourceValue))  
            {
                if (resourceValue is Color directColor)
                {
                    return directColor;
                }

                if (resourceValue is SolidColorBrush brush)
                {
                    return brush.Color;
                }
            }
        }
        return Colors.Transparent;
    }
    public static bool TryGetResourceColor(string resourceKey, out Color color)
    {
        color = Colors.Transparent;

        if (Application.Current?.Resources == null)
            return false;

        if (Application.Current.Resources.TryGetValue(resourceKey, out var resourceValue))
        {
            if (resourceValue is Color directColor)
            {
                color = directColor;
                return true;
            }

            if (resourceValue is SolidColorBrush brush)
            {
                color = brush.Color;
                return true;
            }
        }
        return false;
    }
}
