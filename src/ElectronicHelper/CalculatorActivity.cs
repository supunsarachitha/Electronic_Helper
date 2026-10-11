using System.Globalization;
using System.Text;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using ElectronicHelper.Core;
using Google.Android.Material.TextField;

namespace ElectronicHelper;

/// <summary>
/// Renders any calculator from <see cref="CalculatorCatalog"/>: one input per field, results updated as you type.
/// </summary>
[Activity]
public class CalculatorActivity : BaseActivity
{
    private const string TextsKey = "texts";
    private const string IndicesKey = "indices";

    private readonly List<FieldBinding> _bindings = [];
    private Tool _tool = null!;
    private Calculator _calculator = null!;
    private LinearLayout _resultLines = null!;
    private TextView _resultMessage = null!;
    private CalculationResult? _lastResult;
    private bool _loading;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var id = Intent?.GetStringExtra(ExtraToolId) ?? "";
        if (ToolCatalog.Find(id) is not { } tool || CalculatorCatalog.Find(id) is not { } calculator)
        {
            Finish();
            return;
        }
        _tool = tool;
        _calculator = calculator;

        SetScreen(Resource.Layout.activity_calculator, Resource.Id.scroll);
        Toolbar.Title = tool.Title;
        Toolbar.MenuItemClick += OnMenuItemClick;

        FindViewById<TextView>(Resource.Id.formula)!.Text = calculator.Formula;
        FindViewById<TextView>(Resource.Id.help)!.Text = calculator.Help;
        _resultLines = FindViewById<LinearLayout>(Resource.Id.result_lines)!;
        _resultMessage = FindViewById<TextView>(Resource.Id.result_message)!;

        var container = FindViewById<LinearLayout>(Resource.Id.fields)!;
        foreach (var field in calculator.Fields)
            _bindings.Add(AddField(container, field));
        if (_bindings.LastOrDefault(binding => binding.Input is not null) is { Input: { } last })
            last.ImeOptions = ImeAction.Done;

        Load(savedInstanceState?.GetStringArray(TextsKey), savedInstanceState?.GetIntArray(IndicesKey));
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        base.OnSaveInstanceState(outState);

        // The inputs share layout IDs, so the framework cannot restore them; do it by position.
        outState.PutStringArray(TextsKey, _bindings.Select(binding => binding.Input?.Text ?? "").ToArray());
        outState.PutIntArray(IndicesKey, _bindings.Select(binding => binding.Index).ToArray());
    }

    private FieldBinding AddField(LinearLayout container, Field field)
    {
        var binding = new FieldBinding(field);
        switch (field)
        {
            case NumberField number:
            {
                var view = LayoutInflater.Inflate(Resource.Layout.field_number, container, false)!;
                binding.Layout = view.FindViewById<TextInputLayout>(Resource.Id.input_layout)!;
                binding.Input = view.FindViewById<TextInputEditText>(Resource.Id.input)!;
                binding.Layout.Hint = number.Optional
                    ? this.Format(Resource.String.optional_suffix, number.Label)
                    : number.Label;
                if (number.Signed)
                    binding.Input.InputType = InputTypes.ClassNumber | InputTypes.NumberFlagDecimal | InputTypes.NumberFlagSigned;

                var unitLayout = view.FindViewById<TextInputLayout>(Resource.Id.unit_layout)!;
                if (number.Units.Options.Count > 1)
                {
                    binding.Dropdown = view.FindViewById<MaterialAutoCompleteTextView>(Resource.Id.unit)!;
                    binding.Options = number.Units.Options.Select(option => option.Symbol).ToArray();
                }
                else
                {
                    unitLayout.Visibility = ViewStates.Gone;
                    var symbol = number.Units.Options[0].Symbol;
                    if (symbol.Length > 0) binding.Layout.SuffixText = symbol;
                }
                container.AddView(view);
                break;
            }
            case ChoiceField choice:
            {
                var view = (TextInputLayout)LayoutInflater.Inflate(Resource.Layout.field_choice, container, false)!;
                view.Hint = choice.Label;
                binding.Dropdown = view.FindViewById<MaterialAutoCompleteTextView>(Resource.Id.choice)!;
                binding.Options = choice.Options.ToArray();
                container.AddView(view);
                break;
            }
            case TextField text:
            {
                var view = (TextInputLayout)LayoutInflater.Inflate(Resource.Layout.field_text, container, false)!;
                view.Hint = text.Label;
                view.PlaceholderText = text.Hint;
                binding.Layout = view;
                binding.Input = view.FindViewById<TextInputEditText>(Resource.Id.input)!;
                container.AddView(view);
                break;
            }
        }

        if (binding.Input is { } input) input.AfterTextChanged += (_, _) => Recalculate();
        if (binding.Dropdown is { } dropdown)
        {
            dropdown.SetSimpleItems(binding.Options);
            dropdown.ItemClick += (_, e) =>
            {
                binding.Index = e.Position;
                Recalculate();
            };
        }
        return binding;
    }

    /// <summary>Fills the inputs from saved state, or from each field's defaults when there is none.</summary>
    private void Load(string[]? texts, int[]? indices)
    {
        _loading = true;
        for (var i = 0; i < _bindings.Count; i++)
        {
            var binding = _bindings[i];
            binding.Index = indices is not null && i < indices.Length ? indices[i] : binding.DefaultIndex;
            binding.Dropdown?.SetText(binding.Options[binding.Index], false);
            if (binding.Input is { } input)
                input.Text = texts is not null && i < texts.Length ? texts[i] : binding.DefaultText;
        }
        _loading = false;
        Recalculate();
    }

    private void Recalculate()
    {
        if (_loading) return;

        var numbers = new Dictionary<string, double>();
        var choices = new Dictionary<string, int>();
        var texts = new Dictionary<string, string>();

        foreach (var binding in _bindings)
        {
            switch (binding.Field)
            {
                case NumberField number:
                {
                    var text = binding.Input!.Text?.Trim() ?? "";
                    var valid = TryParseNumber(text, out var value);
                    binding.Layout!.Error = text.Length > 0 && !valid ? GetString(Resource.String.not_a_number) : null;
                    if (valid) numbers[number.Key] = value * number.Units.Options[binding.Index].Factor;
                    break;
                }
                case ChoiceField choice:
                    choices[choice.Key] = binding.Index;
                    break;
                case TextField field:
                    texts[field.Key] = binding.Input!.Text ?? "";
                    break;
            }
        }

        _lastResult = _calculator.Calculate(new Inputs(numbers, choices, texts));
        Ui.ShowResult(this, _resultLines, _resultMessage, _lastResult.Lines, _lastResult.Message, _lastResult.IsError);
    }

    // Some keyboards type a comma for the decimal mark whatever the app asks for.
    private static bool TryParseNumber(string text, out double value) =>
        double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        && double.IsFinite(value);

    private void OnMenuItemClick(object? sender, AndroidX.AppCompat.Widget.Toolbar.MenuItemClickEventArgs e)
    {
        if (e.Item!.ItemId == Resource.Id.menu_reset)
        {
            Load(null, null);
        }
        else if (e.Item.ItemId == Resource.Id.menu_share_result)
        {
            if (_lastResult is { Lines.Count: > 0 } result) this.ShareText(Describe(result));
            else Toast.MakeText(this, Resource.String.nothing_to_share, ToastLength.Short)!.Show();
        }
    }

    private string Describe(CalculationResult result)
    {
        var text = new StringBuilder().AppendLine(_tool.Title).AppendLine();
        foreach (var binding in _bindings)
        {
            var value = binding.Field switch
            {
                ChoiceField => binding.Options[binding.Index],
                NumberField when binding.Input!.Text is { Length: > 0 } number =>
                    $"{number} {((NumberField)binding.Field).Units.Options[binding.Index].Symbol}".TrimEnd(),
                TextField when binding.Input!.Text is { Length: > 0 } entered => entered,
                _ => null,
            };
            if (value is not null) text.AppendLine($"{binding.Field.Label}: {value}");
        }
        text.AppendLine();
        foreach (var line in result.Lines) text.AppendLine($"{line.Label}: {line.Value}");
        return text.ToString().TrimEnd();
    }

    /// <summary>The views that stand for one field, and the option currently picked in its dropdown.</summary>
    private sealed class FieldBinding(Field definition)
    {
        public Field Field => definition;

        public TextInputLayout? Layout { get; set; }

        public EditText? Input { get; set; }

        public MaterialAutoCompleteTextView? Dropdown { get; set; }

        public string[] Options { get; set; } = [];

        public int Index { get; set; }

        public int DefaultIndex => definition switch
        {
            NumberField number => number.Units.DefaultIndex,
            ChoiceField choice => choice.DefaultIndex,
            _ => 0,
        };

        public string DefaultText => definition is NumberField { DefaultText: { } text } ? text : "";
    }
}
