using Android.App;
using Android.Content.Res; // Added for ColorStateList
using Android.OS;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Graphics;

namespace TennisApp;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
            nameof(Entry),
            (handler, view) =>
            {
                if (
                    view is Entry entry
                    && handler.PlatformView is AndroidX.AppCompat.Widget.AppCompatEditText editText
                )
                {
                    // Remove underline
                    editText.BackgroundTintList = ColorStateList.ValueOf(
                        Android.Graphics.Color.Transparent
                    );

                    // Get application reference
                    var mauiApp =
                        IPlatformApplication.Current?.Application
                        as Microsoft.Maui.Controls.Application;

                    // Get colors with proper non-generic TryGetValue
                    var primaryColor = Colors.White;
                    if (
                        mauiApp?.Resources != null
                        && mauiApp.Resources.TryGetValue("Primary", out var primary)
                    )
                    {
                        primaryColor = (Color)primary;
                    }

                    var overlayColor = Colors.Gray;
                    if (
                        mauiApp?.Resources != null
                        && mauiApp.Resources.TryGetValue("Overlay", out var overlay)
                    )
                    {
                        overlayColor = (Color)overlay;
                    }

                    // Android 10+ specific styling
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                    {
#pragma warning disable CA1416
                        editText.TextCursorDrawable?.SetTint(primaryColor.ToAndroid());
                        editText.TextSelectHandleLeft?.SetTint(primaryColor.ToAndroid());
                        editText.TextSelectHandleRight?.SetTint(primaryColor.ToAndroid());
                        editText.TextSelectHandle?.SetTint(primaryColor.ToAndroid());
#pragma warning restore CA1416
                    }

                    // Set highlight color
                    editText.SetHighlightColor(overlayColor.ToAndroid());
                }
            }
        );
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
