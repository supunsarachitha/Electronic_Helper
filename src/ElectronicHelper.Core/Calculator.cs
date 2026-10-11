namespace ElectronicHelper.Core;

/// <summary>An input shown on a calculator screen.</summary>
public abstract record Field(string Key, string Label);

/// <summary>A numeric input with a unit selector. Values reach the calculator in base units.</summary>
public sealed record NumberField(
    string Key,
    string Label,
    UnitSet Units,
    string? DefaultText = null,
    bool Optional = false,
    bool Signed = false)
    : Field(Key, Label);

/// <summary>A pick-one input.</summary>
public sealed record ChoiceField(string Key, string Label, IReadOnlyList<string> Options, int DefaultIndex = 0)
    : Field(Key, Label);

/// <summary>A free-text input, used for component codes and value lists.</summary>
public sealed record TextField(string Key, string Label, string? Hint = null) : Field(Key, Label);

/// <summary>The values a user has filled in. Blank and unparseable number fields are simply absent.</summary>
public sealed class Inputs
{
    private readonly IReadOnlyDictionary<string, double> _numbers;
    private readonly IReadOnlyDictionary<string, int> _choices;
    private readonly IReadOnlyDictionary<string, string> _texts;

    public Inputs(
        IReadOnlyDictionary<string, double>? numbers = null,
        IReadOnlyDictionary<string, int>? choices = null,
        IReadOnlyDictionary<string, string>? texts = null)
    {
        _numbers = numbers ?? new Dictionary<string, double>();
        _choices = choices ?? new Dictionary<string, int>();
        _texts = texts ?? new Dictionary<string, string>();
    }

    public bool Has(string key) => _numbers.ContainsKey(key);

    public double this[string key] => _numbers[key];

    public double? Get(string key) => _numbers.TryGetValue(key, out var value) ? value : null;

    public int Choice(string key) => _choices.TryGetValue(key, out var index) ? index : 0;

    public string Text(string key) => _texts.TryGetValue(key, out var text) ? text.Trim() : "";

    public int Count(params string[] keys) => keys.Count(Has);

    public bool HasAll(params string[] keys) => keys.All(Has);
}

public sealed record ResultLine(string Label, string Value);

/// <summary>
/// What a calculator shows: result lines, a hint asking for more input, or an error.
/// </summary>
public sealed record Result(IReadOnlyList<ResultLine> Lines, string? Message = null, bool IsError = false)
{
    public static Result Hint(string message) => new([], message);

    public static Result Error(string message) => new([], message, true);

    public static Result Of(params ResultLine[] lines) => new(lines);

    public Result WithNote(string note) => this with { Message = note };
}

/// <summary>Thrown by formulas when the inputs cannot produce a meaningful answer.</summary>
public sealed class CalculationException(string message) : Exception(message);

public static class Guard
{
    public static double Positive(double value, string name) =>
        value > 0 && double.IsFinite(value) ? value : throw new CalculationException($"{name} must be greater than zero.");

    public static double NotNegative(double value, string name) =>
        value >= 0 && double.IsFinite(value) ? value : throw new CalculationException($"{name} cannot be negative.");
}

/// <summary>A calculator described as data, so one screen can render all of them.</summary>
public sealed class Calculator
{
    private readonly Func<Inputs, Result> _compute;

    public Calculator(string id, string formula, string help, IReadOnlyList<Field> fields, Func<Inputs, Result> compute)
    {
        Id = id;
        Formula = formula;
        Help = help;
        Fields = fields;
        _compute = compute;
    }

    public string Id { get; }

    /// <summary>The formula as shown to the user, e.g. "V = I × R".</summary>
    public string Formula { get; }

    public string Help { get; }

    public IReadOnlyList<Field> Fields { get; }

    public Result Calculate(Inputs inputs)
    {
        try
        {
            var result = _compute(inputs);
            return result.Lines.Any(line => line.Value == "—")
                ? Result.Error("These values are out of range.")
                : result;
        }
        catch (CalculationException e)
        {
            return Result.Error(e.Message);
        }
    }
}
