using Android.Graphics.Drawables;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using ElectronicHelper.Core;
using Google.Android.Material.Card;
using Google.Android.Material.TextField;
using Color = Android.Graphics.Color;

namespace ElectronicHelper;

/// <summary>Renders any page from <see cref="ReferenceCatalog"/> as a scrolling list of text, tables and cards.</summary>
[Activity]
public class ReferenceActivity : BaseActivity
{
    private ReferenceDoc _doc = null!;
    private ReferenceAdapter _adapter = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var id = Intent?.GetStringExtra(ExtraToolId) ?? "";
        if (ToolCatalog.Find(id) is not { } tool || ReferenceCatalog.Find(id) is not { } doc)
        {
            Finish();
            return;
        }
        _doc = doc;

        SetScreen(Resource.Layout.activity_reference, Resource.Id.list);
        Toolbar.Title = tool.Title;

        _adapter = new ReferenceAdapter(this);
        var list = FindViewById<RecyclerView>(Resource.Id.list)!;
        list.SetLayoutManager(new LinearLayoutManager(this));
        list.SetAdapter(_adapter);

        var searchLayout = FindViewById<TextInputLayout>(Resource.Id.search_layout)!;
        searchLayout.Visibility = doc.Searchable ? ViewStates.Visible : ViewStates.Gone;
        var search = FindViewById<TextInputEditText>(Resource.Id.search)!;
        search.AfterTextChanged += (_, _) => Show(search.Text?.Trim() ?? "");

        Show(search.Text?.Trim() ?? "");
    }

    private void Show(string query)
    {
        var items = new List<Item>();
        var searching = query.Length > 0;

        if (!searching && _doc.Intro is { } intro) items.Add(new TextItem(intro, TextRole.Intro));

        foreach (var section in _doc.Sections)
        {
            var rows = section.Table is { } table
                ? Enumerable.Range(0, table.Rows.Count)
                    .Where(row => !searching || table.Rows[row].Any(cell => cell.Contains(query, StringComparison.OrdinalIgnoreCase)))
                    .ToList()
                : [];
            if (searching && rows.Count == 0) continue;

            if (section.Heading is { } heading) items.Add(new TextItem(heading, TextRole.Heading));
            if (!searching && section.Text is { } text) items.Add(new TextItem(text, TextRole.Body));
            if (section.Table is null) continue;

            if (section.Table.Style == TableStyle.Cards)
            {
                items.AddRange(rows.Select(row => new CardItem(section.Table, row)));
            }
            else
            {
                items.Add(new GridItem(section.Table, null, false));
                items.AddRange(rows.Select((row, index) => new GridItem(section.Table, row, index % 2 == 1)));
            }
        }

        if (searching && items.Count == 0) items.Add(new TextItem(GetString(Resource.String.no_rows), TextRole.Body));
        if (!searching && _doc.Footnote is { } footnote) items.Add(new TextItem(footnote, TextRole.Footnote));

        _adapter.Submit(items);
    }
}

internal enum TextRole { Intro, Heading, Body, Footnote }

internal abstract record Item;

internal sealed record TextItem(string Text, TextRole Role) : Item;

/// <summary>A grid table's header (when <see cref="Row"/> is null) or one of its rows.</summary>
internal sealed record GridItem(RefTable Table, int? Row, bool Shaded) : Item;

internal sealed record CardItem(RefTable Table, int Row) : Item;

internal sealed class ReferenceAdapter(Activity activity) : RecyclerView.Adapter
{
    private const int TextType = 0;
    private const int GridType = 1;
    private const int CardType = 2;

    private IReadOnlyList<Item> _items = [];

    public override int ItemCount => _items.Count;

    public void Submit(IReadOnlyList<Item> items)
    {
        _items = items;
        NotifyDataSetChanged();
    }

    public override int GetItemViewType(int position) => _items[position] switch
    {
        TextItem => TextType,
        GridItem => GridType,
        _ => CardType,
    };

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var matchWidth = new RecyclerView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        switch (viewType)
        {
            case TextType:
                return new Holder(new TextView(activity) { LayoutParameters = matchWidth });

            case GridType:
                return new Holder(new LinearLayout(activity)
                {
                    LayoutParameters = matchWidth,
                    Orientation = Orientation.Horizontal,
                });

            default:
            {
                var margin = activity.Dp(16);
                matchWidth.SetMargins(margin, activity.Dp(4), margin, activity.Dp(4));
                var card = new MaterialCardView(activity) { LayoutParameters = matchWidth };
                var content = new LinearLayout(activity) { Orientation = Orientation.Vertical };
                content.SetPadding(margin, activity.Dp(12), margin, activity.Dp(12));
                card.AddView(content);

                // One handler for the life of the view: adding one per bind would stack up as cards are recycled.
                var holder = new Holder(card);
                card.Click += (_, _) =>
                {
                    if (holder.Link is { } link) activity.OpenUrl(link);
                };
                return holder;
            }
        }
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        switch (_items[position])
        {
            case TextItem text:
                BindText((TextView)holder.ItemView, text);
                break;
            case GridItem grid:
                BindGrid((LinearLayout)holder.ItemView, grid);
                break;
            case CardItem card:
                ((Holder)holder).Link = card.Table.Links?[card.Row];
                BindCard((MaterialCardView)holder.ItemView, card);
                break;
        }
    }

    private void BindText(TextView view, TextItem item)
    {
        var edge = activity.Dp(16);
        var (style, color, top, bottom) = item.Role switch
        {
            TextRole.Heading => (Resource.Attribute.textAppearanceTitleMedium, Resource.Attribute.colorPrimary, 24, 8),
            TextRole.Intro => (Resource.Attribute.textAppearanceBodyLarge, Resource.Attribute.colorOnSurface, 8, 8),
            TextRole.Footnote => (Resource.Attribute.textAppearanceBodySmall, Resource.Attribute.colorOnSurfaceVariant, 24, 8),
            _ => (Resource.Attribute.textAppearanceBodyMedium, Resource.Attribute.colorOnSurfaceVariant, 4, 8),
        };
        view.Text = item.Text;
        view.SetTextStyle(style);
        view.SetTextColor(new Color(activity.ThemeColor(color)));
        view.SetPadding(edge, activity.Dp(top), edge, activity.Dp(bottom));
        if (OperatingSystem.IsAndroidVersionAtLeast(28)) view.AccessibilityHeading = item.Role == TextRole.Heading;
    }

    private void BindGrid(LinearLayout row, GridItem item)
    {
        var table = item.Table;
        var isHeader = item.Row is null;
        var cells = isHeader ? table.Headers : table.Rows[item.Row!.Value];

        row.RemoveAllViews();
        row.SetPadding(activity.Dp(16), activity.Dp(isHeader ? 8 : 10), activity.Dp(16), activity.Dp(isHeader ? 8 : 10));
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetBackgroundColor(item.Shaded
            ? new Color(activity.ThemeColor(Resource.Attribute.colorSurfaceContainerLow))
            : Color.Transparent);

        if (table.Swatches is not null)
            row.AddView(Swatch(isHeader ? null : table.Swatches[item.Row!.Value]));

        for (var i = 0; i < cells.Count; i++)
        {
            var cell = new TextView(activity) { Text = cells[i] };
            cell.SetTextStyle(isHeader ? Resource.Attribute.textAppearanceLabelLarge : Resource.Attribute.textAppearanceBodyMedium);
            cell.SetTextColor(new Color(activity.ThemeColor(
                isHeader ? Resource.Attribute.colorOnSurfaceVariant : Resource.Attribute.colorOnSurface)));
            cell.SetPadding(0, 0, activity.Dp(8), 0);
            row.AddView(cell, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, table.Weights?[i] ?? 1));
        }

        row.ContentDescription = isHeader
            ? null
            : string.Join(", ", cells.Select((cell, i) => $"{table.Headers[i]}: {cell}"));
    }

    /// <summary>A colour chip, or an empty spacer of the same size when there is no colour.</summary>
    private View Swatch(string? hex)
    {
        var size = activity.Dp(18);
        var view = new View(activity)
        {
            LayoutParameters = new LinearLayout.LayoutParams(size, size) { MarginEnd = activity.Dp(12) },
        };
        if (hex is null) return view;

        var chip = new GradientDrawable();
        chip.SetShape(ShapeType.Oval);
        chip.SetColor(Color.ParseColor(hex));
        chip.SetStroke(activity.Dp(1), new Color(activity.ThemeColor(Resource.Attribute.colorOutline)));
        view.Background = chip;
        return view;
    }

    private void BindCard(MaterialCardView card, CardItem item)
    {
        var table = item.Table;
        var cells = table.Rows[item.Row];
        var content = (LinearLayout)card.GetChildAt(0)!;
        content.RemoveAllViews();

        var title = new TextView(activity) { Text = cells[0] };
        title.SetTextStyle(Resource.Attribute.textAppearanceTitleMedium);
        title.SetTextColor(new Color(activity.ThemeColor(Resource.Attribute.colorOnSurface)));
        content.AddView(title);

        // Two columns read as a title and a description; more columns read as labelled values.
        if (cells.Count == 2)
        {
            content.AddView(CardText(cells[1], Resource.Attribute.colorOnSurfaceVariant, topMargin: 4));
        }
        else
        {
            for (var i = 1; i < cells.Count; i++)
            {
                var line = new LinearLayout(activity) { Orientation = Orientation.Horizontal };
                line.SetPadding(0, activity.Dp(4), 0, 0);
                line.AddView(
                    CardText(table.Headers[i], Resource.Attribute.colorOnSurfaceVariant),
                    new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 2));
                line.AddView(
                    CardText(cells[i], Resource.Attribute.colorOnSurface),
                    new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 3));
                content.AddView(line);
            }
        }

        var link = table.Links?[item.Row];
        card.Clickable = link is not null;
        card.Focusable = link is not null;
        card.ContentDescription = null;
        if (link is not null)
        {
            var host = new Uri(link).Host.Replace("www.", "");
            var hint = CardText(host + "  ↗", Resource.Attribute.colorPrimary, topMargin: 8);
            hint.SetTextStyle(Resource.Attribute.textAppearanceLabelLarge);
            hint.SetTextColor(new Color(activity.ThemeColor(Resource.Attribute.colorPrimary)));
            content.AddView(hint);
            card.ContentDescription = $"{cells[0]}. {cells[1]} {activity.Format(Resource.String.opens_in_browser, host)}";
        }
    }

    private TextView CardText(string text, int colorAttribute, int topMargin = 0)
    {
        var view = new TextView(activity) { Text = text };
        view.SetTextStyle(Resource.Attribute.textAppearanceBodyMedium);
        view.SetTextColor(new Color(activity.ThemeColor(colorAttribute)));
        view.SetPadding(0, activity.Dp(topMargin), activity.Dp(8), 0);
        return view;
    }

    private sealed class Holder(View view) : RecyclerView.ViewHolder(view)
    {
        /// <summary>The URL the card currently bound to this holder opens, if any.</summary>
        public string? Link { get; set; }
    }
}
