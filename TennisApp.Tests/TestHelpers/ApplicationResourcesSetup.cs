using Microsoft.Maui.Controls;

namespace TennisApp.Tests.TestHelpers
{
    public static class ApplicationResourcesSetup
    {
        public static void Initialize()
        {
            if (Application.Current == null)
            {
                Application.Current = new Application();
            }

            if (Application.Current.Resources == null)
            {
                Application.Current.Resources = new ResourceDictionary();
            }

            // Colors
            Application.Current.Resources["Background"] = Colors.White;
            Application.Current.Resources["Primary"] = Colors.Blue;
            Application.Current.Resources["Surface"] = Colors.Gray;
            Application.Current.Resources["Overlay"] = Colors.Black;
            Application.Current.Resources["White"] = Colors.White;
            Application.Current.Resources["Gray300"] = Colors.Gray;
            Application.Current.Resources["Gray500"] = Colors.DarkGray;

            // Styles
            var headlineStyle = new Style(typeof(Label))
            {
                Setters =
                {
                    new Setter { Property = Label.FontSizeProperty, Value = 24 },
                    new Setter { Property = Label.TextColorProperty, Value = Colors.White },
                },
            };
            Application.Current.Resources["Headline"] = headlineStyle;
            Application.Current.Resources["SecondaryButton"] = new Style(typeof(Button))
            {
                Setters =
                {
                    new Setter { Property = Button.BackgroundColorProperty, Value = Colors.Gray },
                    new Setter { Property = Button.TextColorProperty, Value = Colors.White },
                    new Setter { Property = Button.PaddingProperty, Value = new Thickness(10) },
                    new Setter { Property = Button.CornerRadiusProperty, Value = 8 },
                },
            };

            var subHeadlineStyle = new Style(typeof(Label))
            {
                Setters =
                {
                    new Setter { Property = Label.FontSizeProperty, Value = 18 },
                    new Setter { Property = Label.TextColorProperty, Value = Colors.White },
                },
            };
            Application.Current.Resources["SubHeadline"] = subHeadlineStyle;

            var primaryButtonStyle = new Style(typeof(Button))
            {
                Setters =
                {
                    new Setter { Property = Button.BackgroundColorProperty, Value = Colors.Blue },
                    new Setter { Property = Button.TextColorProperty, Value = Colors.White },
                    new Setter { Property = Button.PaddingProperty, Value = new Thickness(10) },
                    new Setter { Property = Button.CornerRadiusProperty, Value = 8 },
                },
            };
            Application.Current.Resources["PrimaryButton"] = primaryButtonStyle;
        }
    }
}
