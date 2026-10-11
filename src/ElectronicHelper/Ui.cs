using Android.Content;
using Android.Util;
using Android.Views;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using AndroidX.Core.Widget;
using ElectronicHelper.Core;
using Google.Android.Material.Chip;
using Google.Android.Material.Snackbar;
using Color = Android.Graphics.Color;

namespace ElectronicHelper;

/// <summary>Small helpers shared by every screen.</summary>
internal static class Ui
{
    public static int Dp(this Context context, float dp) =>
        (int)MathF.Round(dp * context.Resources!.DisplayMetrics!.Density);

    /// <summary>Resolves a theme colour attribute such as <c>colorPrimary</c>.</summary>
    public static int ThemeColor(this Context context, int attribute)
    {
        var value = new TypedValue();
        context.Theme!.ResolveAttribute(attribute, value, true);
        return value.ResourceId != 0 ? ContextCompat.GetColor(context, value.ResourceId) : value.Data;
    }

    /// <summary>Applies a theme text style attribute such as <c>textAppearanceTitleMedium</c>.</summary>
    public static void SetTextStyle(this TextView view, int attribute)
    {
        var value = new TypedValue();
        view.Context!.Theme!.ResolveAttribute(attribute, value, true);
        TextViewCompat.SetTextAppearance(view, value.ResourceId);
    }

    /// <summary>
    /// Fills in a string resource's placeholders. Wraps the arguments for Java, which the generated
    /// <c>GetString</c> overload would otherwise need doing at every call.
    /// </summary>
    public static string Format(this Context context, int resource, params object[] arguments) =>
        context.GetString(resource, arguments.Select(ToJava).ToArray());

    private static Java.Lang.Object ToJava(object argument) => argument switch
    {
        int number => Java.Lang.Integer.ValueOf(number)!,
        _ => new Java.Lang.String(argument.ToString() ?? ""),
    };

    /// <summary>The background and foreground colours of a category's icon tile.</summary>
    public static (Color Container, Color OnContainer) CategoryColors(this Context context, ToolCategory category)
    {
        var (container, onContainer) = category switch
        {
            ToolCategory.Basics => (Resource.Color.cat_basics_container, Resource.Color.cat_basics_on_container),
            ToolCategory.Components => (Resource.Color.cat_components_container, Resource.Color.cat_components_on_container),
            ToolCategory.Signals => (Resource.Color.cat_signals_container, Resource.Color.cat_signals_on_container),
            ToolCategory.Power => (Resource.Color.cat_power_container, Resource.Color.cat_power_on_container),
            ToolCategory.Digital => (Resource.Color.cat_digital_container, Resource.Color.cat_digital_on_container),
            ToolCategory.Connectors => (Resource.Color.cat_connectors_container, Resource.Color.cat_connectors_on_container),
            _ => (Resource.Color.cat_learn_container, Resource.Color.cat_learn_on_container),
        };
        return (new Color(ContextCompat.GetColor(context, container)), new Color(ContextCompat.GetColor(context, onContainer)));
    }

    public static Chip AddFilterChip(this ChipGroup group, LayoutInflater inflater, string text, bool isChecked, Action onChecked)
    {
        var chip = (Chip)inflater.Inflate(Resource.Layout.chip_filter, group, false)!;
        chip.Id = View.GenerateViewId();
        chip.Text = text;
        chip.Checked = isChecked;
        chip.CheckedChange += (_, e) =>
        {
            if (e.IsChecked) onChecked();
        };
        group.AddView(chip);
        return chip;
    }

    /// <summary>Fills a result card. Tapping a line copies its value.</summary>
    public static void ShowResult(
        Activity activity, LinearLayout lines, TextView message, IReadOnlyList<ResultLine> results, string? note, bool isError)
    {
        lines.RemoveAllViews();
        foreach (var line in results)
        {
            if (lines.ChildCount > 0) lines.AddView(Divider(activity));

            var row = activity.LayoutInflater.Inflate(Resource.Layout.item_result, lines, false)!;
            row.FindViewById<TextView>(Resource.Id.result_label)!.Text = line.Label;
            row.FindViewById<TextView>(Resource.Id.result_value)!.Text = line.Value;
            row.ContentDescription = $"{line.Label}: {line.Value}";
            row.Click += (_, _) => Copy(activity, row, line.Value);
            lines.AddView(row);
        }

        message.Text = note;
        message.Visibility = string.IsNullOrEmpty(note) ? ViewStates.Gone : ViewStates.Visible;
        message.SetTextColor(new Color(activity.ThemeColor(
            isError ? Resource.Attribute.colorError : Resource.Attribute.colorOnSurfaceVariant)));
    }

    private static View Divider(Context context) => new(context)
    {
        LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, context.Dp(1)),
        Background = new Android.Graphics.Drawables.ColorDrawable(new Color(context.ThemeColor(Resource.Attribute.colorOutlineVariant))),
    };

    public static void OpenUrl(this Context context, string url) =>
        Start(context, new Intent(Intent.ActionView, Android.Net.Uri.Parse(url)));

    public static void ShareText(this Context context, string text)
    {
        var send = new Intent(Intent.ActionSend);
        send.SetType("text/plain");
        send.PutExtra(Intent.ExtraText, text);
        Start(context, Intent.CreateChooser(send, (string?)null)!);
    }

    public static void Start(Context context, Intent intent)
    {
        try
        {
            context.StartActivity(intent);
        }
        catch (ActivityNotFoundException)
        {
            Toast.MakeText(context, Resource.String.no_app_found, ToastLength.Short)!.Show();
        }
    }

    private static void Copy(Context context, View anchor, string text)
    {
        var clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService)!;
        clipboard.PrimaryClip = ClipData.NewPlainText(text, text);

        // Android 13 and later show their own confirmation.
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            Snackbar.Make(anchor, context.Format(Resource.String.copied, text), Snackbar.LengthShort).Show();
    }
}

internal sealed class InsetsListener(Func<View, WindowInsetsCompat, WindowInsetsCompat> apply)
    : Java.Lang.Object, IOnApplyWindowInsetsListener
{
    public WindowInsetsCompat? OnApplyWindowInsets(View? view, WindowInsetsCompat? insets) =>
        view is null || insets is null ? insets : apply(view, insets);
}

/// <summary>The tools a user has starred, kept in app-private preferences.</summary>
internal sealed class Favorites(Context context)
{
    private const string Key = "favorites";

    private readonly ISharedPreferences _preferences =
        context.GetSharedPreferences("electronic_helper", FileCreationMode.Private)!;

    public HashSet<string> Load() => new(_preferences.GetStringSet(Key, null) ?? Array.Empty<string>());

    public void Toggle(string id)
    {
        var ids = Load();
        if (!ids.Add(id)) ids.Remove(id);
        _preferences.Edit()!.PutStringSet(Key, ids)!.Apply();
    }
}
