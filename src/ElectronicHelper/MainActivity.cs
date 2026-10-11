using Android.Content;
using Android.Content.Res;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
using ElectronicHelper.Core;
using Google.Android.Material.Chip;
using Google.Android.Material.Dialog;
using Google.Android.Material.TextField;

namespace ElectronicHelper;

/// <summary>The home screen: every tool, searchable, grouped by category, with favourites on top.</summary>
[Activity(MainLauncher = true, Exported = true, Theme = "@style/Theme.ElectronicHelper.Starting")]
public class MainActivity : BaseActivity
{
    private const string FilterKey = "filter";
    private const string FeedbackAddress = "stechbuzz@gmail.com";
    private const string PrivacyUrl = "https://supunsarachitha.github.io/Electronic_Helper/privacy.html";

    // A filter is one of these two, or a ToolCategory cast to int.
    private const int FilterAll = -1;
    private const int FilterFavorites = -2;

    private Favorites _favorites = null!;
    private ToolAdapter _adapter = null!;
    private TextInputEditText _search = null!;
    private TextView _empty = null!;
    private int _filter = FilterAll;

    private string PlayUrl => "https://play.google.com/store/apps/details?id=" + PackageName;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);
        base.OnCreate(savedInstanceState);
        SetScreen(Resource.Layout.activity_main, Resource.Id.tool_list);

        _favorites = new Favorites(this);
        _filter = savedInstanceState?.GetInt(FilterKey, FilterAll) ?? FilterAll;
        _empty = FindViewById<TextView>(Resource.Id.empty_view)!;
        Toolbar.MenuItemClick += OnMenuItemClick;

        _adapter = new ToolAdapter(Open, tool =>
        {
            _favorites.Toggle(tool.Id);
            Refresh();
        });

        // Two cards across on phones, more on tablets and unfolded foldables.
        var columns = Math.Max(2, Resources!.Configuration!.ScreenWidthDp / 190);
        var list = FindViewById<RecyclerView>(Resource.Id.tool_list)!;
        var grid = new GridLayoutManager(this, columns);
        grid.SetSpanSizeLookup(new SectionSpanLookup(_adapter, columns));
        list.SetLayoutManager(grid);
        list.SetAdapter(_adapter);

        var chips = FindViewById<ChipGroup>(Resource.Id.category_chips)!;
        AddFilter(chips, GetString(Resource.String.filter_all), FilterAll);
        AddFilter(chips, GetString(Resource.String.filter_favorites), FilterFavorites);
        foreach (var category in Enum.GetValues<ToolCategory>())
            AddFilter(chips, ToolCatalog.CategoryName(category), (int)category);

        _search = FindViewById<TextInputEditText>(Resource.Id.search)!;
        _search.Hint = this.Format(Resource.String.search_hint, ToolCatalog.All.Count);
        _search.AfterTextChanged += (_, _) => Refresh();

        Refresh();
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        base.OnSaveInstanceState(outState);
        outState.PutInt(FilterKey, _filter);
    }

    private void AddFilter(ChipGroup chips, string label, int filter) =>
        chips.AddFilterChip(LayoutInflater, label, _filter == filter, () =>
        {
            _filter = filter;
            Refresh();
        });

    private void Refresh()
    {
        var favorites = _favorites.Load();
        var query = _search.Text?.Trim() ?? "";

        var tools = ToolCatalog.All.Where(tool => _filter switch
        {
            FilterAll => true,
            FilterFavorites => favorites.Contains(tool.Id),
            _ => (int)tool.Category == _filter,
        });
        if (query.Length > 0) tools = tools.Where(tool => tool.Matches(query));

        var rows = new List<Row>();
        if (_filter == FilterAll && query.Length == 0)
        {
            var starred = ToolCatalog.All.Where(tool => favorites.Contains(tool.Id)).ToList();
            if (starred.Count > 0)
            {
                rows.Add(new SectionRow(GetString(Resource.String.section_favorites), starred.Count));
                rows.AddRange(starred.Select(tool => new ToolRow(tool)));
            }
            foreach (var group in tools.GroupBy(tool => tool.Category))
            {
                rows.Add(new SectionRow(ToolCatalog.CategoryName(group.Key), group.Count()));
                rows.AddRange(group.Select(tool => new ToolRow(tool)));
            }
        }
        else
        {
            rows.AddRange(tools.Select(tool => new ToolRow(tool)));
        }

        _adapter.Submit(rows, favorites);
        _empty.Visibility = rows.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
        _empty.SetText(_filter == FilterFavorites && query.Length == 0
            ? Resource.String.no_favorites
            : Resource.String.no_matches);
    }

    private void Open(Tool tool)
    {
        var screen = tool.Kind switch
        {
            ToolKind.Calculator => typeof(CalculatorActivity),
            ToolKind.Reference => typeof(ReferenceActivity),
            ToolKind.ResistorColorCode => typeof(ResistorActivity),
            _ => typeof(LogicGateActivity),
        };
        StartActivity(new Intent(this, screen).PutExtra(ExtraToolId, tool.Id));
    }

    private void OnMenuItemClick(object? sender, AndroidX.AppCompat.Widget.Toolbar.MenuItemClickEventArgs e)
    {
        var id = e.Item!.ItemId;
        if (id == Resource.Id.menu_share_app)
        {
            this.ShareText(this.Format(Resource.String.share_app_text, PlayUrl));
        }
        else if (id == Resource.Id.menu_rate)
        {
            RateApp();
        }
        else if (id == Resource.Id.menu_feedback)
        {
            var email = new Intent(Intent.ActionSendto, Android.Net.Uri.Parse("mailto:" + FeedbackAddress));
            email.PutExtra(Intent.ExtraSubject, this.Format(Resource.String.feedback_subject, VersionName()));
            Ui.Start(this, email);
        }
        else if (id == Resource.Id.menu_privacy)
        {
            this.OpenUrl(PrivacyUrl);
        }
        else if (id == Resource.Id.menu_about)
        {
            new MaterialAlertDialogBuilder(this)
                .SetTitle(Resource.String.app_name)!
                .SetMessage(this.Format(Resource.String.about_message, VersionName()))!
                .SetPositiveButton(Resource.String.ok, (_, _) => { })!
                .SetNeutralButton(Resource.String.menu_privacy, (_, _) => this.OpenUrl(PrivacyUrl))!
                .Show();
        }
    }

    private void RateApp()
    {
        // Prefer the Play Store app; fall back to the web listing on devices without it.
        try
        {
            StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse("market://details?id=" + PackageName)));
        }
        catch (ActivityNotFoundException)
        {
            this.OpenUrl(PlayUrl);
        }
    }

    private string VersionName() => PackageManager!.GetPackageInfo(PackageName!, 0)!.VersionName ?? "";
}

internal abstract record Row;

internal sealed record SectionRow(string Title, int Count) : Row;

internal sealed record ToolRow(Tool Tool) : Row;

internal sealed class SectionSpanLookup(ToolAdapter adapter, int columns) : GridLayoutManager.SpanSizeLookup
{
    public override int GetSpanSize(int position) => adapter.IsSection(position) ? columns : 1;
}

internal sealed class ToolAdapter(Action<Tool> open, Action<Tool> toggleFavorite) : RecyclerView.Adapter
{
    private const int SectionType = 0;
    private const int ToolType = 1;

    private IReadOnlyList<Row> _rows = [];
    private HashSet<string> _favorites = [];

    public override int ItemCount => _rows.Count;

    public void Submit(IReadOnlyList<Row> rows, HashSet<string> favorites)
    {
        _rows = rows;
        _favorites = favorites;
        NotifyDataSetChanged();
    }

    public bool IsSection(int position) => position < _rows.Count && _rows[position] is SectionRow;

    public override int GetItemViewType(int position) => IsSection(position) ? SectionType : ToolType;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var inflater = LayoutInflater.From(parent.Context)!;
        if (viewType == SectionType)
            return new SectionHolder(inflater.Inflate(Resource.Layout.item_section, parent, false)!);

        return new ToolHolder(
            inflater.Inflate(Resource.Layout.item_tool, parent, false)!,
            position => open(((ToolRow)_rows[position]).Tool),
            position => toggleFavorite(((ToolRow)_rows[position]).Tool));
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        switch (_rows[position])
        {
            case SectionRow section:
                ((SectionHolder)holder).Title.Text = section.Title;
                ((SectionHolder)holder).Count.Text = section.Count.ToString();
                break;
            case ToolRow row:
                ((ToolHolder)holder).Bind(row.Tool, _favorites.Contains(row.Tool.Id));
                break;
        }
    }

    private sealed class SectionHolder(View view) : RecyclerView.ViewHolder(view)
    {
        public TextView Title { get; } = view.FindViewById<TextView>(Resource.Id.section_title)!;

        public TextView Count { get; } = view.FindViewById<TextView>(Resource.Id.section_count)!;
    }

    private sealed class ToolHolder : RecyclerView.ViewHolder
    {
        private readonly TextView _glyph;
        private readonly TextView _title;
        private readonly TextView _subtitle;
        private readonly ImageButton _favorite;

        public ToolHolder(View view, Action<int> onOpen, Action<int> onToggleFavorite) : base(view)
        {
            _glyph = view.FindViewById<TextView>(Resource.Id.glyph)!;
            _title = view.FindViewById<TextView>(Resource.Id.title)!;
            _subtitle = view.FindViewById<TextView>(Resource.Id.subtitle)!;
            _favorite = view.FindViewById<ImageButton>(Resource.Id.favorite)!;

            view.Click += (_, _) => WhenBound(onOpen);
            _favorite.Click += (_, _) => WhenBound(onToggleFavorite);
        }

        public void Bind(Tool tool, bool isFavorite)
        {
            var context = ItemView.Context!;
            var (container, onContainer) = context.CategoryColors(tool.Category);
            _glyph.Text = tool.Glyph;
            _glyph.SetTextColor(onContainer);
            ViewCompat.SetBackgroundTintList(_glyph, ColorStateList.ValueOf(container));
            _title.Text = tool.Title;
            _subtitle.Text = tool.Subtitle;

            _favorite.SetImageResource(isFavorite ? Resource.Drawable.ic_star : Resource.Drawable.ic_star_outline);
            ImageViewCompat.SetImageTintList(_favorite, ColorStateList.ValueOf(new Android.Graphics.Color(
                context.ThemeColor(isFavorite ? Resource.Attribute.colorSecondary : Resource.Attribute.colorOnSurfaceVariant))));
            _favorite.ContentDescription = context.Format(
                isFavorite ? Resource.String.remove_favorite : Resource.String.add_favorite, tool.Title);
        }

        // A tap can arrive just after the row has been removed from the list.
        private void WhenBound(Action<int> action)
        {
            if (BindingAdapterPosition != RecyclerView.NoPosition) action(BindingAdapterPosition);
        }
    }
}
