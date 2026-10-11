using System.Globalization;
using System.Text.RegularExpressions;

namespace ElectronicHelper.Core;

public enum ESeriesKind { E6, E12, E24, E48, E96 }

/// <summary>IEC 60063 preferred values.</summary>
public static class ESeries
{
    private static readonly int[] E24 =
        [100, 110, 120, 130, 150, 160, 180, 200, 220, 240, 270, 300, 330, 360, 390, 430, 470, 510, 560, 620, 680, 750, 820, 910];

    private static readonly int[] E96 =
    [
        100, 102, 105, 107, 110, 113, 115, 118, 121, 124, 127, 130, 133, 137, 140, 143,
        147, 150, 154, 158, 162, 165, 169, 174, 178, 182, 187, 191, 196, 200, 205, 210,
        215, 221, 226, 232, 237, 243, 249, 255, 261, 267, 274, 280, 287, 294, 301, 309,
        316, 324, 332, 340, 348, 357, 365, 374, 383, 392, 402, 412, 422, 432, 442, 453,
        464, 475, 487, 499, 511, 523, 536, 549, 562, 576, 590, 604, 619, 634, 649, 665,
        681, 698, 715, 732, 750, 768, 787, 806, 825, 845, 866, 887, 909, 931, 953, 976,
    ];

    /// <summary>The series' values for one decade, as three-digit integers from 100 to 976.</summary>
    public static IReadOnlyList<int> Decade(ESeriesKind kind) => kind switch
    {
        ESeriesKind.E6 => EveryNth(E24, 4),
        ESeriesKind.E12 => EveryNth(E24, 2),
        ESeriesKind.E24 => E24,
        ESeriesKind.E48 => EveryNth(E96, 2),
        _ => E96,
    };

    public static double Nearest(double value, ESeriesKind kind)
    {
        var candidates = Candidates(value, kind);
        return candidates.MinBy(candidate => Math.Abs(candidate - value));
    }

    /// <summary>The largest preferred value that does not exceed <paramref name="value"/>.</summary>
    public static double Floor(double value, ESeriesKind kind) =>
        Candidates(value, kind).Last(candidate => candidate <= value * (1 + 1e-9));

    /// <summary>The smallest preferred value that is not below <paramref name="value"/>.</summary>
    public static double Ceiling(double value, ESeriesKind kind) =>
        Candidates(value, kind).First(candidate => candidate >= value * (1 - 1e-9));

    private static List<double> Candidates(double value, ESeriesKind kind)
    {
        Guard.Positive(value, "Value");
        var decade = (int)Math.Floor(Math.Log10(value));
        var candidates = new List<double>();
        for (var d = decade - 1; d <= decade + 1; d++)
        {
            // Scale through decimal so 470 × 0.01 is exactly 4.7, not 4.7000000000000002.
            var scale = (decimal)Math.Pow(10, d - 2);
            candidates.AddRange(Decade(kind).Select(mantissa => (double)(mantissa * scale)));
        }
        return candidates;
    }

    private static int[] EveryNth(int[] source, int step) => source.Where((_, index) => index % step == 0).ToArray();
}

public enum BandColor { Black, Brown, Red, Orange, Yellow, Green, Blue, Violet, Grey, White, Gold, Silver }

/// <summary>What a colour means in each band position (IEC 60062).</summary>
public sealed record BandColorInfo(
    BandColor Color,
    string Name,
    string Hex,
    int? Digit,
    double Multiplier,
    double? TolerancePercent,
    int? TempCoefficient);

public readonly record struct DecodedResistor(double Resistance, double TolerancePercent, int? TempCoefficient)
{
    public double Minimum => Resistance * (1 - TolerancePercent / 100);

    public double Maximum => Resistance * (1 + TolerancePercent / 100);
}

public static class ResistorColorCode
{
    public static IReadOnlyList<BandColorInfo> Colors { get; } =
    [
        new(BandColor.Black, "Black", "#1B1B1B", 0, 1, null, 250),
        new(BandColor.Brown, "Brown", "#7B4A21", 1, 10, 1, 100),
        new(BandColor.Red, "Red", "#D32F2F", 2, 100, 2, 50),
        new(BandColor.Orange, "Orange", "#F57C00", 3, 1e3, 0.05, 15),
        new(BandColor.Yellow, "Yellow", "#FBC02D", 4, 1e4, 0.02, 25),
        new(BandColor.Green, "Green", "#388E3C", 5, 1e5, 0.5, 20),
        new(BandColor.Blue, "Blue", "#1976D2", 6, 1e6, 0.25, 10),
        new(BandColor.Violet, "Violet", "#7B1FA2", 7, 1e7, 0.1, 5),
        new(BandColor.Grey, "Grey", "#8E8E8E", 8, 1e8, 0.01, 1),
        new(BandColor.White, "White", "#F5F5F5", 9, 1e9, null, null),
        new(BandColor.Gold, "Gold", "#C9A227", null, 0.1, 5, null),
        new(BandColor.Silver, "Silver", "#BDBDBD", null, 0.01, 10, null),
    ];

    public static IReadOnlyList<BandColorInfo> DigitColors { get; } = Colors.Where(c => c.Digit.HasValue).ToArray();

    public static IReadOnlyList<BandColorInfo> MultiplierColors => Colors;

    public static IReadOnlyList<BandColorInfo> ToleranceColors { get; } =
        Colors.Where(c => c.TolerancePercent.HasValue).ToArray();

    public static IReadOnlyList<BandColorInfo> TempCoefficientColors { get; } =
        Colors.Where(c => c.TempCoefficient.HasValue).ToArray();

    public static BandColorInfo Info(BandColor color) => Colors[(int)color];

    /// <summary>
    /// Decodes 3 bands (two digits, multiplier, ±20%), 4 bands (adds tolerance),
    /// 5 bands (three digits) or 6 bands (adds temperature coefficient).
    /// </summary>
    public static DecodedResistor Decode(IReadOnlyList<BandColor> bands)
    {
        if (bands.Count is < 3 or > 6) throw new CalculationException("A resistor has 3 to 6 bands.");

        var digitCount = bands.Count >= 5 ? 3 : 2;
        double mantissa = 0;
        for (var i = 0; i < digitCount; i++)
        {
            var digit = Info(bands[i]).Digit
                ?? throw new CalculationException($"{Info(bands[i]).Name} cannot be a digit band.");
            mantissa = mantissa * 10 + digit;
        }

        // Scale through decimal so 47 × 0.1 is exactly 4.7.
        var resistance = (double)((decimal)mantissa * (decimal)Info(bands[digitCount]).Multiplier);

        var tolerance = 20.0;
        if (bands.Count >= 4)
        {
            var info = Info(bands[digitCount + 1]);
            tolerance = info.TolerancePercent ?? throw new CalculationException($"{info.Name} cannot be a tolerance band.");
        }

        int? tempCoefficient = null;
        if (bands.Count == 6)
        {
            var info = Info(bands[5]);
            tempCoefficient = info.TempCoefficient
                ?? throw new CalculationException($"{info.Name} cannot be a temperature coefficient band.");
        }

        return new(resistance, tolerance, tempCoefficient);
    }

    /// <summary>
    /// Finds the digit and multiplier bands for a resistance, rounding to the digits available.
    /// Returns null when the value cannot be marked (below 0.1 Ω or above 999 GΩ).
    /// </summary>
    public static IReadOnlyList<BandColor>? Encode(double ohms, int digitBands)
    {
        if (digitBands is not (2 or 3)) throw new ArgumentOutOfRangeException(nameof(digitBands));
        if (!(ohms > 0) || !double.IsFinite(ohms)) return null;

        var exponent = (int)Math.Floor(Math.Log10(ohms)) - (digitBands - 1);
        var mantissa = (long)Math.Round(ohms / Math.Pow(10, exponent), MidpointRounding.AwayFromZero);
        var limit = digitBands == 2 ? 100 : 1000;
        if (mantissa >= limit)
        {
            mantissa /= 10;
            exponent++;
        }
        if (exponent is < -2 or > 9) return null;

        var bands = new List<BandColor>();
        foreach (var digit in mantissa.ToString(CultureInfo.InvariantCulture).PadLeft(digitBands, '0'))
            bands.Add((BandColor)(digit - '0'));

        bands.Add(exponent switch
        {
            -1 => BandColor.Gold,
            -2 => BandColor.Silver,
            _ => (BandColor)exponent,
        });
        return bands;
    }
}

/// <summary>Markings printed on surface-mount resistors.</summary>
public static partial class SmdResistorCode
{
    public static bool TryDecode(string? code, out double ohms, out string marking)
    {
        ohms = 0;
        marking = "";
        if (string.IsNullOrWhiteSpace(code)) return false;
        var s = code.Trim().ToUpperInvariant();

        if (ZeroPattern().IsMatch(s))
        {
            marking = "Zero-ohm jumper";
            return true;
        }

        if (DecimalPattern().IsMatch(s) && s.Any(char.IsDigit))
        {
            marking = "R marks the decimal point";
            return double.TryParse(s.Replace('R', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out ohms);
        }

        var eia96 = Eia96Pattern().Match(s);
        if (eia96.Success)
        {
            var index = int.Parse(eia96.Groups[1].Value, CultureInfo.InvariantCulture);
            if (index is < 1 or > 96) return false;
            ohms = (double)(ESeries.Decade(ESeriesKind.E96)[index - 1] * Eia96Multiplier(eia96.Groups[2].Value[0]));
            marking = "EIA-96 (1% parts)";
            return true;
        }

        if (DigitsPattern().IsMatch(s))
        {
            var mantissa = int.Parse(s[..^1], CultureInfo.InvariantCulture);
            ohms = mantissa * Math.Pow(10, s[^1] - '0');
            marking = s.Length == 3 ? "3-digit code" : "4-digit code (1% parts)";
            return true;
        }

        return false;
    }

    private static decimal Eia96Multiplier(char letter) => letter switch
    {
        'Z' => 0.001m,
        'Y' => 0.01m,
        'X' or 'S' => 0.1m,
        'A' => 1m,
        'B' or 'H' => 10m,
        'C' => 100m,
        'D' => 1_000m,
        'E' => 10_000m,
        _ => 100_000m,
    };

    [GeneratedRegex(@"^0{1,4}$")]
    private static partial Regex ZeroPattern();

    [GeneratedRegex(@"^\d*R\d*$")]
    private static partial Regex DecimalPattern();

    [GeneratedRegex(@"^(\d{2})([ZYXSABHCDEF])$")]
    private static partial Regex Eia96Pattern();

    [GeneratedRegex(@"^\d{3,4}$")]
    private static partial Regex DigitsPattern();
}

/// <summary>Markings printed on ceramic and film capacitors.</summary>
public static partial class CapacitorCode
{
    public static IReadOnlyList<(char Letter, string Tolerance)> ToleranceLetters { get; } =
    [
        ('B', "±0.1 pF"), ('C', "±0.25 pF"), ('D', "±0.5 pF"), ('F', "±1%"), ('G', "±2%"),
        ('J', "±5%"), ('K', "±10%"), ('M', "±20%"), ('Z', "+80% / −20%"),
    ];

    /// <summary>Decodes "104", "472K", "47", "4n7", "100n" or "4R7" into farads.</summary>
    public static bool TryDecode(string? code, out double farads, out string? tolerance)
    {
        farads = 0;
        tolerance = null;
        if (string.IsNullOrWhiteSpace(code)) return false;
        var s = code.Trim();

        var letter = s[^1];
        var toleranceText = ToleranceLetters.FirstOrDefault(t => t.Letter == letter).Tolerance;
        if (toleranceText is not null && s.Length > 1)
        {
            tolerance = toleranceText;
            s = s[..^1];
        }

        var prefixed = PrefixPattern().Match(s);
        if (prefixed.Success && (prefixed.Groups[1].Length > 0 || prefixed.Groups[3].Length > 0))
        {
            var number = $"{Zero(prefixed.Groups[1].Value)}.{Zero(prefixed.Groups[3].Value)}";
            var factor = prefixed.Groups[2].Value[0] switch
            {
                'p' => 1e-12m,
                'n' => 1e-9m,
                'R' => 1e-12m,
                _ => 1e-6m,
            };
            farads = (double)(decimal.Parse(number, CultureInfo.InvariantCulture) * factor);
            return true;
        }

        if (!DigitsPattern().IsMatch(s)) return false;

        if (s.Length < 3)
        {
            farads = (double)(int.Parse(s, CultureInfo.InvariantCulture) * 1e-12m);
            return true;
        }

        // Third digit is the power of ten, except 8 and 9 which divide by 100 and 10.
        var multiplier = (s[2] - '0') switch
        {
            8 => 0.01m,
            9 => 0.1m,
            var power => (decimal)Math.Pow(10, power),
        };
        farads = (double)(int.Parse(s[..2], CultureInfo.InvariantCulture) * multiplier * 1e-12m);
        return true;
    }

    private static string Zero(string digits) => digits.Length == 0 ? "0" : digits;

    [GeneratedRegex(@"^(\d*)([pnuµμR])(\d*)$")]
    private static partial Regex PrefixPattern();

    [GeneratedRegex(@"^\d{1,3}$")]
    private static partial Regex DigitsPattern();
}

/// <summary>American Wire Gauge geometry. Gauges 0 to −3 are 1/0 to 4/0.</summary>
public static class WireGauge
{
    public static double DiameterMm(int awg) => 0.127 * Math.Pow(92, (36 - awg) / 39.0);

    public static double AreaSquareMm(int awg)
    {
        var diameter = DiameterMm(awg);
        return Math.PI / 4 * diameter * diameter;
    }

    public static double OhmsPerKm(int awg) => Formulas.CopperWireResistance(AreaSquareMm(awg), 1000);

    public static string Name(int awg) => awg switch
    {
        0 => "1/0",
        -1 => "2/0",
        -2 => "3/0",
        -3 => "4/0",
        _ => awg.ToString(CultureInfo.InvariantCulture),
    };
}
