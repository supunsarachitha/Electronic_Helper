using System.Globalization;
using System.Text.RegularExpressions;

namespace ElectronicHelper.Core;

/// <summary>
/// Engineering-notation formatting and parsing ("4.7 kΩ", "100n", "4k7").
/// </summary>
public static partial class Eng
{
    private static readonly (double Factor, string Symbol)[] Prefixes =
    [
        (1e-15, "f"), (1e-12, "p"), (1e-9, "n"), (1e-6, "µ"), (1e-3, "m"),
        (1, ""), (1e3, "k"), (1e6, "M"), (1e9, "G"), (1e12, "T"),
    ];

    private const int UnityIndex = 5;

    private static readonly string[] UnitWords =
        ["Ω", "R", "r", "ohm", "ohms", "F", "H", "Hz", "V", "A", "W", "s"];

    /// <summary>Formats a value with an SI prefix, e.g. 4700 → "4.7 kΩ".</summary>
    public static string Format(double value, string unit, int digits = 4)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return "—";
        if (value == 0) return unit.Length == 0 ? "0" : "0 " + unit;

        var abs = Math.Abs(value);
        if (abs < 1e-15 || abs >= 1e15) return WithUnit(Number(value, digits), unit);

        var index = Math.Clamp((int)Math.Floor(Math.Log10(abs) / 3) + UnityIndex, 0, Prefixes.Length - 1);
        var mantissa = RoundSignificant(value / Prefixes[index].Factor, digits);

        // Log10 and rounding can each land one prefix off at the boundaries.
        if (Math.Abs(mantissa) >= 1000 && index < Prefixes.Length - 1)
        {
            index++;
            mantissa = RoundSignificant(value / Prefixes[index].Factor, digits);
        }
        else if (Math.Abs(mantissa) < 1 && index > 0)
        {
            index--;
            mantissa = RoundSignificant(value / Prefixes[index].Factor, digits);
        }

        return $"{Plain(mantissa)} {Prefixes[index].Symbol}{unit}";
    }

    /// <summary>Formats a plain number to a number of significant digits, without an SI prefix.</summary>
    public static string Number(double value, int digits = 4)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return "—";
        if (value == 0) return "0";

        var abs = Math.Abs(value);
        if (abs < 1e-4 || abs >= 1e12) return value.ToString("0.###e+0", CultureInfo.InvariantCulture);

        var magnitude = (int)Math.Floor(Math.Log10(abs));
        var decimals = Math.Clamp(digits - 1 - magnitude, 0, 12);
        return Math.Round((decimal)value, decimals, MidpointRounding.AwayFromZero)
            .ToString("0.############", CultureInfo.InvariantCulture);
    }

    public static string WithUnit(string number, string unit) => unit.Length == 0 ? number : $"{number} {unit}";

    public static double RoundSignificant(double value, int digits)
    {
        if (value == 0 || double.IsNaN(value) || double.IsInfinity(value)) return value;

        var magnitude = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        var decimals = digits - 1 - magnitude;
        if (decimals is >= 0 and <= 15 && Math.Abs(value) < 1e12)
            return (double)Math.Round((decimal)value, decimals, MidpointRounding.AwayFromZero);

        var scale = Math.Pow(10, decimals);
        return Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale;
    }

    /// <summary>
    /// Parses "4700", "4.7k", "4k7", "0R1", "100n", "2.2 µF", "1e3". A comma is accepted as the decimal mark.
    /// </summary>
    public static bool TryParse(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = text.Trim();

        var infix = InfixPattern().Match(s);
        if (infix.Success)
        {
            var number = $"{infix.Groups[2].Value}.{infix.Groups[4].Value}";
            if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var mantissa)) return false;
            value = mantissa * PrefixFactor(infix.Groups[3].Value[0])!.Value;
            if (infix.Groups[1].Value == "-") value = -value;
            return true;
        }

        var suffix = SuffixPattern().Match(s);
        if (!suffix.Success) return false;
        if (!double.TryParse(suffix.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return false;
        if (!TrySuffixFactor(suffix.Groups[2].Value, out var factor)) return false;

        value = parsed * factor;
        return !double.IsInfinity(value);
    }

    /// <summary>
    /// Parses a list of values separated by spaces, commas, semicolons or plus signs.
    /// Values written without a prefix or unit are multiplied by <paramref name="plainFactor"/>.
    /// </summary>
    public static bool TryParseList(string? text, out List<double> values, out string? invalidToken, double plainFactor = 1)
    {
        values = [];
        invalidToken = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        foreach (var token in text.Split([' ', ',', ';', '+', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryParse(token, out var value))
            {
                invalidToken = token;
                return false;
            }
            var isPlain = double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            values.Add(isPlain ? value * plainFactor : value);
        }
        return values.Count > 0;
    }

    private static string Plain(double value) => value.ToString("0.###############", CultureInfo.InvariantCulture);

    private static bool TrySuffixFactor(string suffix, out double factor)
    {
        factor = 1;
        if (suffix.Length == 0 || IsUnitWord(suffix)) return true;

        var prefix = PrefixFactor(suffix[0]);
        if (prefix is null) return false;
        if (suffix.Length > 1 && !IsUnitWord(suffix[1..])) return false;

        factor = prefix.Value;
        return true;
    }

    private static bool IsUnitWord(string s) =>
        UnitWords.Contains(s) || s.Equals("ohm", StringComparison.OrdinalIgnoreCase) || s.Equals("ohms", StringComparison.OrdinalIgnoreCase);

    private static double? PrefixFactor(char c) => c switch
    {
        'p' => 1e-12,
        'n' => 1e-9,
        'u' or 'µ' or 'μ' => 1e-6,
        'm' => 1e-3,
        'k' or 'K' => 1e3,
        'M' => 1e6,
        'G' => 1e9,
        'R' or 'r' or 'Ω' => 1,
        _ => null,
    };

    [GeneratedRegex(@"^([+-]?)(\d+)([pnuµμmkKMGRrΩ])(\d+)$")]
    private static partial Regex InfixPattern();

    [GeneratedRegex(@"^([+-]?(?:\d+(?:[.,]\d*)?|[.,]\d+)(?:[eE][+-]?\d+)?)\s*([A-Za-zµμΩ]*)$")]
    private static partial Regex SuffixPattern();
}
