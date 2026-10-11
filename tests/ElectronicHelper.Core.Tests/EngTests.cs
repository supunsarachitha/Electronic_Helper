using ElectronicHelper.Core;

namespace ElectronicHelper.Core.Tests;

public class EngTests
{
    [Theory]
    [InlineData(4700, "Ω", "4.7 kΩ")]
    [InlineData(1000, "Ω", "1 kΩ")]
    [InlineData(999.96, "Ω", "1 kΩ")]
    [InlineData(1e-6, "F", "1 µF")]
    [InlineData(1e-7, "F", "100 nF")]
    [InlineData(2.2e-11, "F", "22 pF")]
    [InlineData(0.02, "A", "20 mA")]
    [InlineData(1_000_000, "Hz", "1 MHz")]
    [InlineData(12, "V", "12 V")]
    [InlineData(0, "V", "0 V")]
    [InlineData(-3300, "V", "-3.3 kV")]
    [InlineData(1.23456, "V", "1.235 V")]
    public void Format_picks_the_prefix_that_keeps_the_number_between_1_and_1000(double value, string unit, string expected) =>
        Assert.Equal(expected, Eng.Format(value, unit));

    [Fact]
    public void Format_shows_a_dash_for_values_that_are_not_numbers()
    {
        Assert.Equal("—", Eng.Format(double.NaN, "V"));
        Assert.Equal("—", Eng.Format(double.PositiveInfinity, "V"));
    }

    [Theory]
    [InlineData(3.14159, "3.142")]
    [InlineData(123456.7, "123457")]
    [InlineData(0.012345, "0.01235")]
    [InlineData(50, "50")]
    [InlineData(66.6666, "66.67")]
    public void Number_rounds_to_significant_digits_without_dropping_whole_digits(double value, string expected) =>
        Assert.Equal(expected, Eng.Number(value));

    [Theory]
    [InlineData("4700", 4700)]
    [InlineData("4.7k", 4700)]
    [InlineData("4k7", 4700)]
    [InlineData("4K7", 4700)]
    [InlineData("0R1", 0.1)]
    [InlineData("47R", 47)]
    [InlineData("2M2", 2.2e6)]
    [InlineData("1M", 1e6)]
    [InlineData("1m", 1e-3)]
    [InlineData("100n", 1e-7)]
    [InlineData("2.2 µF", 2.2e-6)]
    [InlineData("10uF", 1e-5)]
    [InlineData("22pF", 2.2e-11)]
    [InlineData("10kΩ", 1e4)]
    [InlineData("1e3", 1000)]
    [InlineData("1,5k", 1500)]
    [InlineData(" 330 ", 330)]
    [InlineData("5V", 5)]
    [InlineData("2.4GHz", 2.4e9)]
    public void TryParse_reads_prefixes_units_and_infix_notation(string text, double expected)
    {
        Assert.True(Eng.TryParse(text, out var value));
        Assert.Equal(expected, value, 9);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("4x7")]
    [InlineData("10 bananas")]
    [InlineData(null)]
    public void TryParse_rejects_text_that_is_not_a_value(string? text) => Assert.False(Eng.TryParse(text, out _));

    [Fact]
    public void TryParseList_splits_on_spaces_commas_and_plus_signs()
    {
        Assert.True(Eng.TryParseList("100, 220 + 4k7;1M", out var values, out _));
        Assert.Equal([100, 220, 4700, 1e6], values);
    }

    [Fact]
    public void TryParseList_reports_the_token_it_could_not_read()
    {
        Assert.False(Eng.TryParseList("100 oops 220", out _, out var invalid));
        Assert.Equal("oops", invalid);
    }

    [Fact]
    public void TryParseList_scales_only_the_values_written_without_a_prefix()
    {
        Assert.True(Eng.TryParseList("100 4.7u", out var values, out _, plainFactor: 1e-9));
        Assert.Equal(1e-7, values[0], 15);
        Assert.Equal(4.7e-6, values[1], 15);
    }
}
