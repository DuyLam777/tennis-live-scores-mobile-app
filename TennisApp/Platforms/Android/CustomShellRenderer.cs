#if ANDROID
using Android.Content;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace TennisApp
{
    // Custom Shell renderer that fixes the missing resource issue
    public class CustomShellRenderer : ShellRenderer
    {
        public CustomShellRenderer() { }

        public CustomShellRenderer(Context context)
            : base(context) { }

        protected override IShellToolbarAppearanceTracker CreateToolbarAppearanceTracker()
        {
            return new CustomShellToolbarAppearanceTracker(this);
        }
    }

    // Custom toolbar appearance tracker that prevents the resource issue
    public class CustomShellToolbarAppearanceTracker : ShellToolbarAppearanceTracker
    {
        public CustomShellToolbarAppearanceTracker(IShellContext shellContext)
            : base(shellContext) { }

        // Override the correct method signature with fully qualified types
        public override void SetAppearance(
            AndroidX.AppCompat.Widget.Toolbar toolbar,
            IShellToolbarTracker toolbarTracker,
            ShellAppearance appearance
        )
        {
            try
            {
                base.SetAppearance(toolbar, toolbarTracker, appearance);
            }
            catch (Android.Content.Res.Resources.NotFoundException)
            {
                // Ignore the resource not found exception
                System.Diagnostics.Debug.WriteLine(
                    "Caught and handled Resources.NotFoundException"
                );
            }

            // Set an empty description to avoid resource lookup
            try
            {
                toolbar.NavigationContentDescription = "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error setting NavigationContentDescription: {ex.Message}"
                );
            }
        }
    }
}
#endif
