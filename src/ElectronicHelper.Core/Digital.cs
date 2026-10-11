using System.Globalization;
using System.Numerics;

namespace ElectronicHelper.Core;

public enum GateKind { And, Or, Not, Nand, Nor, Xor, Xnor }

public sealed record GateInfo(GateKind Kind, string Name, string Expression, string Description, int InputCount);

public static class LogicGates
{
    public static IReadOnlyList<GateInfo> All { get; } =
    [
        new(GateKind.And, "AND", "Q = A · B",
            "The output is high only when every input is high.", 2),
        new(GateKind.Or, "OR", "Q = A + B",
            "The output is high when at least one input is high.", 2),
        new(GateKind.Not, "NOT", "Q = ¬A",
            "An inverter. The output is always the opposite of the input.", 1),
        new(GateKind.Nand, "NAND", "Q = ¬(A · B)",
            "An AND gate followed by an inverter. The output is low only when every input is high. Any logic function can be built from NAND gates alone.", 2),
        new(GateKind.Nor, "NOR", "Q = ¬(A + B)",
            "An OR gate followed by an inverter. The output is high only when every input is low. Any logic function can be built from NOR gates alone.", 2),
        new(GateKind.Xor, "XOR", "Q = A ⊕ B",
            "Exclusive OR. The output is high when the inputs differ.", 2),
        new(GateKind.Xnor, "XNOR", "Q = ¬(A ⊕ B)",
            "Exclusive NOR. The output is high when the inputs are the same, which makes it an equality detector.", 2),
    ];

    public static GateInfo Info(GateKind kind) => All[(int)kind];

    public static bool Evaluate(GateKind kind, bool a, bool b) => kind switch
    {
        GateKind.And => a && b,
        GateKind.Or => a || b,
        GateKind.Not => !a,
        GateKind.Nand => !(a && b),
        GateKind.Nor => !(a || b),
        GateKind.Xor => a != b,
        GateKind.Xnor => a == b,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>Rows in counting order: 00, 01, 10, 11 (or 0, 1 for a single input).</summary>
    public static IReadOnlyList<(bool A, bool B, bool Q)> TruthTable(GateKind kind)
    {
        if (Info(kind).InputCount == 1)
            return [(false, false, Evaluate(kind, false, false)), (true, false, Evaluate(kind, true, false))];

        return
        [
            (false, false, Evaluate(kind, false, false)),
            (false, true, Evaluate(kind, false, true)),
            (true, false, Evaluate(kind, true, false)),
            (true, true, Evaluate(kind, true, true)),
        ];
    }
}

public static class NumberBase
{
    /// <summary>Parses an unsigned integer of up to 64 bits. Spaces, underscores and 0x/0b/0o prefixes are ignored.</summary>
    public static bool TryParse(string? text, int radix, out ulong value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var s = text.Replace(" ", "").Replace("_", "").ToUpperInvariant();
        var prefix = radix switch { 16 => "0X", 2 => "0B", 8 => "0O", _ => null };
        if (prefix is not null && s.StartsWith(prefix, StringComparison.Ordinal)) s = s[2..];
        if (s.Length == 0) return false;

        BigInteger total = 0;
        foreach (var c in s)
        {
            var digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => -1,
            };
            if (digit < 0 || digit >= radix) return false;
            total = total * radix + digit;
            if (total > ulong.MaxValue) return false;
        }

        value = (ulong)total;
        return true;
    }

    public static string ToDecimal(ulong value) => value.ToString("N0", CultureInfo.InvariantCulture);

    public static string ToHex(ulong value) => "0x" + value.ToString("X", CultureInfo.InvariantCulture);

    public static string ToOctal(ulong value) => Convert.ToString((long)value, 8);

    /// <summary>Binary in groups of four bits, padded to a whole nibble.</summary>
    public static string ToBinary(ulong value)
    {
        var bits = Convert.ToString((long)value, 2);
        bits = bits.PadLeft((bits.Length + 3) / 4 * 4, '0');
        return string.Join(' ', Enumerable.Range(0, bits.Length / 4).Select(i => bits.Substring(i * 4, 4)));
    }

    public static int BitLength(ulong value) => value == 0 ? 1 : 64 - BitOperations.LeadingZeroCount(value);
}
