using ElectronicHelper.Core;

namespace ElectronicHelper.Core.Tests;

public class DigitalTests
{
    [Theory]
    [InlineData(GateKind.And, "0001")]
    [InlineData(GateKind.Or, "0111")]
    [InlineData(GateKind.Nand, "1110")]
    [InlineData(GateKind.Nor, "1000")]
    [InlineData(GateKind.Xor, "0110")]
    [InlineData(GateKind.Xnor, "1001")]
    [InlineData(GateKind.Not, "10")]
    public void TruthTable_lists_outputs_in_counting_order(GateKind kind, string outputs)
    {
        var actual = string.Concat(LogicGates.TruthTable(kind).Select(row => row.Q ? '1' : '0'));

        Assert.Equal(outputs, actual);
    }

    [Fact]
    public void Every_gate_has_a_description_in_enum_order()
    {
        foreach (var kind in Enum.GetValues<GateKind>())
        {
            Assert.Equal(kind, LogicGates.Info(kind).Kind);
            Assert.NotEmpty(LogicGates.Info(kind).Description);
        }
    }

    [Theory]
    [InlineData("255", 10, 255UL)]
    [InlineData("ff", 16, 255UL)]
    [InlineData("0xFF", 16, 255UL)]
    [InlineData("1111 1111", 2, 255UL)]
    [InlineData("0b1010", 2, 10UL)]
    [InlineData("377", 8, 255UL)]
    [InlineData("18446744073709551615", 10, ulong.MaxValue)]
    public void NumberBase_parses_each_radix(string text, int radix, ulong expected)
    {
        Assert.True(NumberBase.TryParse(text, radix, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("12", 2)]
    [InlineData("8", 8)]
    [InlineData("FG", 16)]
    [InlineData("-1", 10)]
    [InlineData("18446744073709551616", 10)]
    [InlineData("", 10)]
    [InlineData("0x", 16)]
    public void NumberBase_rejects_bad_digits_and_overflow(string text, int radix) =>
        Assert.False(NumberBase.TryParse(text, radix, out _));

    [Fact]
    public void NumberBase_formats_each_radix()
    {
        Assert.Equal("0xFF", NumberBase.ToHex(255));
        Assert.Equal("1111 1111", NumberBase.ToBinary(255));
        Assert.Equal("0001 0000", NumberBase.ToBinary(16));
        Assert.Equal("0000", NumberBase.ToBinary(0));
        Assert.Equal("377", NumberBase.ToOctal(255));
        Assert.Equal("1,000,000", NumberBase.ToDecimal(1_000_000));
        Assert.Equal("0xFFFFFFFFFFFFFFFF", NumberBase.ToHex(ulong.MaxValue));
        Assert.Equal(64, NumberBase.ToBinary(ulong.MaxValue).Replace(" ", "").Length);
    }

    [Theory]
    [InlineData(0UL, 1)]
    [InlineData(1UL, 1)]
    [InlineData(255UL, 8)]
    [InlineData(256UL, 9)]
    public void NumberBase_counts_the_bits_needed(ulong value, int bits) => Assert.Equal(bits, NumberBase.BitLength(value));
}
