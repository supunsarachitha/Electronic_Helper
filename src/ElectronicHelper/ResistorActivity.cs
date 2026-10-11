using Android.Views;
using ElectronicHelper.Core;
using Google.Android.Material.Chip;
using Google.Android.Material.TextField;

namespace ElectronicHelper;

/// <summary>Reads a resistor's colour bands, or finds the bands for a value you type.</summary>
[Activity]
public class ResistorActivity : BaseActivity
{
    private const string BandsKey = "bands";

    private enum Role { Digit, Multiplier, Tolerance, TempCoefficient }

    private List<BandColor> _bands = [BandColor.Yellow, BandColor.Violet, BandColor.Red, BandColor.Gold];
    private ResistorView _resistor = null!;
    private LinearLayout _fields = null!;
    private LinearLayout _resultLines = null!;
    private TextView _resultMessage = null!;
    private TextInputLayout _valueLayout = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetScreen(Resource.Layout.activity_resistor, Resource.Id.scroll);
        Toolbar.Title = ToolCatalog.Find("resistor-color-code")?.Title;

        if (savedInstanceState?.GetIntArray(BandsKey) is { Length: >= 4 and <= 6 } saved)
            _bands = saved.Select(band => (BandColor)band).ToList();

        _resistor = FindViewById<ResistorView>(Resource.Id.resistor_view)!;
        _fields = FindViewById<LinearLayout>(Resource.Id.fields)!;
        _resultLines = FindViewById<LinearLayout>(Resource.Id.result_lines)!;
        _resultMessage = FindViewById<TextView>(Resource.Id.result_message)!;
        _valueLayout = FindViewById<TextInputLayout>(Resource.Id.value_layout)!;

        var chips = FindViewById<ChipGroup>(Resource.Id.band_count_chips)!;
        foreach (var count in new[] { 4, 5, 6 })
        {
            chips.AddFilterChip(
                LayoutInflater,
                this.Format(Resource.String.resistor_band_count, count),
                _bands.Count == count,
                () => SetBandCount(count));
        }

        var valueInput = FindViewById<TextInputEditText>(Resource.Id.value_input)!;
        valueInput.AfterTextChanged += (_, _) => SetFromValue(valueInput.Text?.Trim() ?? "");

        BuildFields();
        ShowResult();
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        base.OnSaveInstanceState(outState);
        outState.PutIntArray(BandsKey, _bands.Select(band => (int)band).ToArray());
    }

    private static int DigitCount(int bandCount) => bandCount >= 5 ? 3 : 2;

    private static Role RoleOf(int position, int bandCount)
    {
        var digits = DigitCount(bandCount);
        if (position < digits) return Role.Digit;
        if (position == digits) return Role.Multiplier;
        return position == digits + 1 ? Role.Tolerance : Role.TempCoefficient;
    }

    /// <summary>Switches between 4, 5 and 6 bands, keeping the resistance and tolerance where they can be kept.</summary>
    private void SetBandCount(int count)
    {
        if (count == _bands.Count) return;

        var current = ResistorColorCode.Decode(_bands);
        var tolerance = _bands[DigitCount(_bands.Count) + 1];
        var value = ResistorColorCode.Encode(current.Resistance, DigitCount(count))
            ?? (count >= 5
                ? [BandColor.Brown, BandColor.Black, BandColor.Black, BandColor.Brown]
                : [BandColor.Brown, BandColor.Black, BandColor.Red]);

        _bands = [.. value, tolerance];
        if (count == 6) _bands.Add(BandColor.Brown);

        BuildFields();
        ShowResult();
    }

    private void SetFromValue(string text)
    {
        _valueLayout.Error = null;
        if (text.Length == 0) return;
        if (!Eng.TryParse(text, out var ohms))
        {
            _valueLayout.Error = GetString(Resource.String.not_a_number);
            return;
        }

        var digits = DigitCount(_bands.Count);
        if (ResistorColorCode.Encode(ohms, digits) is not { } value)
        {
            _valueLayout.Error = GetString(Resource.String.resistor_value_unmarkable);
            return;
        }

        for (var i = 0; i < value.Count; i++) _bands[i] = value[i];
        BuildFields();
        ShowResult();
    }

    /// <summary>Creates one colour picker per band, offering only the colours that position can take.</summary>
    private void BuildFields()
    {
        _fields.RemoveAllViews();
        for (var i = 0; i < _bands.Count; i++)
        {
            var position = i;
            var role = RoleOf(position, _bands.Count);
            var options = role switch
            {
                Role.Digit => ResistorColorCode.DigitColors,
                Role.Multiplier => ResistorColorCode.MultiplierColors,
                Role.Tolerance => ResistorColorCode.ToleranceColors,
                _ => ResistorColorCode.TempCoefficientColors,
            };

            var layout = (TextInputLayout)LayoutInflater.Inflate(Resource.Layout.field_choice, _fields, false)!;
            layout.Hint = role switch
            {
                Role.Digit => this.Format(Resource.String.resistor_digit, position + 1),
                Role.Multiplier => GetString(Resource.String.resistor_multiplier),
                Role.Tolerance => GetString(Resource.String.resistor_tolerance),
                _ => GetString(Resource.String.resistor_tempco),
            };

            var dropdown = layout.FindViewById<MaterialAutoCompleteTextView>(Resource.Id.choice)!;
            var labels = options.Select(option => Label(option, role)).ToArray();
            dropdown.SetSimpleItems(labels);

            var selected = Math.Max(0, options.ToList().FindIndex(option => option.Color == _bands[position]));
            _bands[position] = options[selected].Color;
            dropdown.SetText(labels[selected], false);
            dropdown.ItemClick += (_, e) =>
            {
                _bands[position] = options[e.Position].Color;
                ShowResult();
            };

            _fields.AddView(layout);
        }
    }

    private static string Label(BandColorInfo color, Role role) => role switch
    {
        Role.Digit => $"{color.Name}  ·  {color.Digit}",
        Role.Multiplier => $"{color.Name}  ·  × {(color.Multiplier < 1 ? Eng.Number(color.Multiplier) : Eng.Format(color.Multiplier, "").TrimEnd())}",
        Role.Tolerance => $"{color.Name}  ·  ±{Eng.Number(color.TolerancePercent ?? 0)}%",
        _ => $"{color.Name}  ·  {color.TempCoefficient} ppm/°C",
    };

    private void ShowResult()
    {
        _resistor.SetBands(_bands);

        var decoded = ResistorColorCode.Decode(_bands);
        var lines = new List<ResultLine>
        {
            new(GetString(Resource.String.resistor_result_resistance),
                $"{Eng.Format(decoded.Resistance, "Ω")} ±{Eng.Number(decoded.TolerancePercent)}%"),
            new(GetString(Resource.String.resistor_result_range),
                $"{Eng.Format(decoded.Minimum, "Ω", 5)} – {Eng.Format(decoded.Maximum, "Ω", 5)}"),
        };
        if (decoded.TempCoefficient is { } tempCoefficient)
            lines.Add(new(GetString(Resource.String.resistor_result_tempco), $"{tempCoefficient} ppm/°C"));

        Ui.ShowResult(this, _resultLines, _resultMessage, lines, null, false);
    }
}
