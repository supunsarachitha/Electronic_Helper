using Android.Graphics;
using Android.Views;
using ElectronicHelper.Core;
using Google.Android.Material.Chip;
using Google.Android.Material.MaterialSwitch;
using Color = Android.Graphics.Color;

namespace ElectronicHelper;

/// <summary>An interactive logic gate: pick a gate, flip its inputs, and watch the output and truth table.</summary>
[Activity]
public class LogicGateActivity : BaseActivity
{
    private const string GateKey = "gate";

    private GateKind _kind = GateKind.And;
    private GateView _gate = null!;
    private MaterialSwitch _switchA = null!;
    private MaterialSwitch _switchB = null!;
    private TextView _output = null!;
    private TextView _expression = null!;
    private TextView _description = null!;
    private TableLayout _table = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetScreen(Resource.Layout.activity_logic_gate, Resource.Id.scroll);
        Toolbar.Title = ToolCatalog.Find("logic-gates")?.Title;

        _kind = (GateKind)(savedInstanceState?.GetInt(GateKey, 0) ?? 0);
        _gate = FindViewById<GateView>(Resource.Id.gate_view)!;
        _switchA = FindViewById<MaterialSwitch>(Resource.Id.switch_a)!;
        _switchB = FindViewById<MaterialSwitch>(Resource.Id.switch_b)!;
        _output = FindViewById<TextView>(Resource.Id.gate_output)!;
        _expression = FindViewById<TextView>(Resource.Id.gate_expression)!;
        _description = FindViewById<TextView>(Resource.Id.gate_description)!;
        _table = FindViewById<TableLayout>(Resource.Id.truth_table)!;

        var chips = FindViewById<ChipGroup>(Resource.Id.gate_chips)!;
        foreach (var gate in LogicGates.All)
        {
            chips.AddFilterChip(LayoutInflater, gate.Name, gate.Kind == _kind, () =>
            {
                _kind = gate.Kind;
                Show();
            });
        }

        _switchA.CheckedChange += (_, _) => Show();
        _switchB.CheckedChange += (_, _) => Show();
        Show();
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        base.OnSaveInstanceState(outState);
        outState.PutInt(GateKey, (int)_kind);
    }

    private void Show()
    {
        var info = LogicGates.Info(_kind);
        var a = _switchA.Checked;
        var b = info.InputCount == 2 && _switchB.Checked;
        var output = LogicGates.Evaluate(_kind, a, b);

        _switchB.Visibility = info.InputCount == 2 ? ViewStates.Visible : ViewStates.Gone;
        _gate.Set(_kind, a, b);
        _output.Text = this.Format(
            Resource.String.gate_output, GetString(output ? Resource.String.gate_high : Resource.String.gate_low));
        _expression.Text = info.Expression;
        _description.Text = info.Description;

        ShowTruthTable(info, a, b);
    }

    private void ShowTruthTable(GateInfo info, bool a, bool b)
    {
        _table.RemoveAllViews();
        var twoInputs = info.InputCount == 2;

        _table.AddView(TableRow(twoInputs ? ["A", "B", "Q"] : ["A", "Q"], header: true, current: false));
        foreach (var row in LogicGates.TruthTable(_kind))
        {
            string[] cells = twoInputs ? [Bit(row.A), Bit(row.B), Bit(row.Q)] : [Bit(row.A), Bit(row.Q)];
            var current = row.A == a && (!twoInputs || row.B == b);
            _table.AddView(TableRow(cells, header: false, current));
        }
    }

    private static string Bit(bool level) => level ? "1" : "0";

    /// <summary>One truth-table row. The row matching the switches is highlighted.</summary>
    private TableRow TableRow(string[] cells, bool header, bool current)
    {
        var row = new TableRow(this);
        if (current) row.SetBackgroundColor(new Color(this.ThemeColor(Resource.Attribute.colorSecondaryContainer)));

        var textColor = current
            ? Resource.Attribute.colorOnSecondaryContainer
            : header ? Resource.Attribute.colorOnSurfaceVariant : Resource.Attribute.colorOnSurface;

        foreach (var text in cells)
        {
            var cell = new TextView(this) { Text = text, Gravity = GravityFlags.Center };
            cell.SetTextStyle(header ? Resource.Attribute.textAppearanceLabelLarge : Resource.Attribute.textAppearanceBodyLarge);
            cell.SetTextColor(new Color(this.ThemeColor(textColor)));
            cell.SetTypeface(cell.Typeface, current ? TypefaceStyle.Bold : TypefaceStyle.Normal);
            cell.SetPadding(0, this.Dp(10), 0, this.Dp(10));
            row.AddView(cell);
        }
        return row;
    }
}
