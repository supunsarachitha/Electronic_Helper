using Android.Content.Res;
using Android.Views;
using AndroidX.Activity;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using Google.Android.Material.AppBar;

namespace ElectronicHelper;

/// <summary>
/// Sets up what every screen shares: drawing behind the system bars, the toolbar, and back navigation.
/// </summary>
public abstract class BaseActivity : AppCompatActivity
{
    public const string ExtraToolId = "tool_id";

    protected MaterialToolbar Toolbar { get; private set; } = null!;

    /// <summary>
    /// Inflates a layout that follows the app's screen shape: a <c>root</c> holding an <c>app_bar</c>
    /// with a <c>toolbar</c>, and one scrolling view.
    /// </summary>
    protected void SetScreen(int layout, int scrollingViewId)
    {
        // Android 15 and later draw every app edge to edge; this opts in on older versions too.
        EdgeToEdge.Enable(this);
        SetContentView(layout);

        // The bars are transparent, so their icons must contrast with the app's own background.
        var night = (Resources!.Configuration!.UiMode & UiMode.NightMask) == UiMode.NightYes;
        var bars = WindowCompat.GetInsetsController(Window!, Window!.DecorView);
        bars!.AppearanceLightStatusBars = !night;
        bars.AppearanceLightNavigationBars = !night;

        Toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar)!;
        Toolbar.NavigationClick += (_, _) => OnBackPressedDispatcher.OnBackPressed();

        var appBar = FindViewById(Resource.Id.app_bar)!;
        var scrolling = FindViewById(scrollingViewId)!;
        var (left, top, right, bottom) =
            (scrolling.PaddingLeft, scrolling.PaddingTop, scrolling.PaddingRight, scrolling.PaddingBottom);

        ViewCompat.SetOnApplyWindowInsetsListener(FindViewById(Resource.Id.root)!, new InsetsListener((_, insets) =>
        {
            var bars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout())!;
            var keyboard = insets.GetInsets(WindowInsetsCompat.Type.Ime())!;

            appBar.SetPadding(bars.Left, bars.Top, bars.Right, 0);
            scrolling.SetPadding(
                left + bars.Left, top, right + bars.Right, bottom + Math.Max(bars.Bottom, keyboard.Bottom));
            return insets;
        }));
    }
}
