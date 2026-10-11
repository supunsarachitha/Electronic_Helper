using ElectronicHelper.Core;

namespace ElectronicHelper.Core.Tests;

public class ComponentTests
{
    [Fact]
    public void ESeries_E96_table_matches_the_defining_formula()
    {
        var expected = Enumerable.Range(0, 96).Select(i => (int)Math.Round(100 * Math.Pow(10, i / 96.0))).ToArray();

        Assert.Equal(expected, ESeries.Decade(ESeriesKind.E96));
    }

    [Theory]
    [InlineData(ESeriesKind.E6, 6)]
    [InlineData(ESeriesKind.E12, 12)]
    [InlineData(ESeriesKind.E24, 24)]
    [InlineData(ESeriesKind.E48, 48)]
    [InlineData(ESeriesKind.E96, 96)]
    public void ESeries_has_one_value_per_step(ESeriesKind kind, int count) =>
        Assert.Equal(count, ESeries.Decade(kind).Count);

    [Fact]
    public void ESeries_E12_is_the_familiar_set() =>
        Assert.Equal([100, 120, 150, 180, 220, 270, 330, 390, 470, 560, 680, 820], ESeries.Decade(ESeriesKind.E12));

    [Theory]
    [InlineData(5000, ESeriesKind.E24, 5100)]
    [InlineData(5000, ESeriesKind.E12, 4700)]
    [InlineData(150, ESeriesKind.E12, 150)]
    [InlineData(0.95, ESeriesKind.E12, 1)]
    [InlineData(9.9, ESeriesKind.E6, 10)]
    [InlineData(4.6e-9, ESeriesKind.E12, 4.7e-9)]
    public void ESeries_Nearest_crosses_decades(double value, ESeriesKind kind, double expected) =>
        Assert.Equal(expected, ESeries.Nearest(value, kind), 15);

    [Fact]
    public void ESeries_Floor_and_Ceiling_bracket_a_value_and_keep_exact_matches()
    {
        Assert.Equal(120, ESeries.Floor(140, ESeriesKind.E12), 9);
        Assert.Equal(150, ESeries.Ceiling(140, ESeriesKind.E12), 9);
        Assert.Equal(150, ESeries.Floor(150, ESeriesKind.E12), 9);
        Assert.Equal(150, ESeries.Ceiling(150, ESeriesKind.E12), 9);
        Assert.Equal(1000, ESeries.Ceiling(830, ESeriesKind.E12), 9);
    }

    [Fact]
    public void Resistor_decodes_four_bands()
    {
        var resistor = ResistorColorCode.Decode([BandColor.Yellow, BandColor.Violet, BandColor.Red, BandColor.Gold]);

        Assert.Equal(4700, resistor.Resistance);
        Assert.Equal(5, resistor.TolerancePercent);
        Assert.Equal(4465, resistor.Minimum, 6);
        Assert.Equal(4935, resistor.Maximum, 6);
    }

    [Fact]
    public void Resistor_decodes_five_and_six_bands()
    {
        var five = ResistorColorCode.Decode([BandColor.Brown, BandColor.Black, BandColor.Black, BandColor.Brown, BandColor.Brown]);
        var six = ResistorColorCode.Decode([BandColor.Orange, BandColor.Orange, BandColor.Black, BandColor.Black, BandColor.Brown, BandColor.Red]);

        Assert.Equal(1000, five.Resistance);
        Assert.Equal(1, five.TolerancePercent);
        Assert.Null(five.TempCoefficient);
        Assert.Equal(330, six.Resistance);
        Assert.Equal(50, six.TempCoefficient);
    }

    [Fact]
    public void Resistor_gold_and_silver_multipliers_give_exact_fractions()
    {
        Assert.Equal(4.7, ResistorColorCode.Decode([BandColor.Yellow, BandColor.Violet, BandColor.Gold, BandColor.Gold]).Resistance);
        Assert.Equal(0.47, ResistorColorCode.Decode([BandColor.Yellow, BandColor.Violet, BandColor.Silver, BandColor.Gold]).Resistance);
    }

    [Fact]
    public void Resistor_three_bands_default_to_20_percent() =>
        Assert.Equal(20, ResistorColorCode.Decode([BandColor.Red, BandColor.Red, BandColor.Brown]).TolerancePercent);

    [Fact]
    public void Resistor_rejects_colours_that_cannot_sit_in_a_position()
    {
        Assert.Throws<CalculationException>(() =>
            ResistorColorCode.Decode([BandColor.Gold, BandColor.Violet, BandColor.Red, BandColor.Gold]));
        Assert.Throws<CalculationException>(() =>
            ResistorColorCode.Decode([BandColor.Yellow, BandColor.Violet, BandColor.Red, BandColor.White]));
    }

    [Theory]
    [InlineData(4700, 2, new[] { BandColor.Yellow, BandColor.Violet, BandColor.Red })]
    [InlineData(1000, 2, new[] { BandColor.Brown, BandColor.Black, BandColor.Red })]
    [InlineData(1000, 3, new[] { BandColor.Brown, BandColor.Black, BandColor.Black, BandColor.Brown })]
    [InlineData(0.47, 2, new[] { BandColor.Yellow, BandColor.Violet, BandColor.Silver })]
    [InlineData(4.7, 2, new[] { BandColor.Yellow, BandColor.Violet, BandColor.Gold })]
    [InlineData(1e6, 2, new[] { BandColor.Brown, BandColor.Black, BandColor.Green })]
    [InlineData(4990, 3, new[] { BandColor.Yellow, BandColor.White, BandColor.White, BandColor.Brown })]
    [InlineData(996, 2, new[] { BandColor.Brown, BandColor.Black, BandColor.Red })]
    public void Resistor_encodes_a_value_into_bands(double ohms, int digitBands, BandColor[] expected) =>
        Assert.Equal(expected, ResistorColorCode.Encode(ohms, digitBands));

    [Fact]
    public void Resistor_encode_then_decode_returns_the_value()
    {
        foreach (var mantissa in ESeries.Decade(ESeriesKind.E24))
        foreach (var decade in new[] { 0.01, 1, 100, 10_000, 1e6 })
        {
            var ohms = mantissa * decade;
            var bands = ResistorColorCode.Encode(ohms, 2)!.Append(BandColor.Gold).ToArray();

            Assert.Equal(ohms, ResistorColorCode.Decode(bands).Resistance, 6);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(0.001)]
    [InlineData(1e13)]
    public void Resistor_encode_returns_null_for_values_that_cannot_be_marked(double ohms) =>
        Assert.Null(ResistorColorCode.Encode(ohms, 2));

    [Theory]
    [InlineData("103", 10_000)]
    [InlineData("100", 10)]
    [InlineData("4R7", 4.7)]
    [InlineData("R47", 0.47)]
    [InlineData("0R1", 0.1)]
    [InlineData("4702", 47_000)]
    [InlineData("1000", 100)]
    [InlineData("01C", 10_000)]
    [InlineData("68X", 49.9)]
    [InlineData("96A", 976)]
    [InlineData("01Y", 1)]
    [InlineData("000", 0)]
    [InlineData("4r7", 4.7)]
    public void SmdResistor_decodes_each_marking_style(string code, double expected)
    {
        Assert.True(SmdResistorCode.TryDecode(code, out var ohms, out var marking));
        Assert.Equal(expected, ohms, 9);
        Assert.NotEmpty(marking);
    }

    [Theory]
    [InlineData("")]
    [InlineData("97A")]
    [InlineData("00A")]
    [InlineData("1")]
    [InlineData("12345")]
    [InlineData("AB")]
    [InlineData("R")]
    public void SmdResistor_rejects_codes_it_does_not_know(string code) =>
        Assert.False(SmdResistorCode.TryDecode(code, out _, out _));

    [Theory]
    [InlineData("104", 100e-9, null)]
    [InlineData("472K", 4.7e-9, "±10%")]
    [InlineData("101J", 100e-12, "±5%")]
    [InlineData("47", 47e-12, null)]
    [InlineData("5", 5e-12, null)]
    [InlineData("4n7", 4.7e-9, null)]
    [InlineData("100n", 100e-9, null)]
    [InlineData("4p7", 4.7e-12, null)]
    [InlineData("1u5", 1.5e-6, null)]
    [InlineData("4R7", 4.7e-12, null)]
    [InlineData("479", 4.7e-12, null)]
    [InlineData("478", 0.47e-12, null)]
    [InlineData("105M", 1e-6, "±20%")]
    public void Capacitor_decodes_each_marking_style(string code, double expected, string? tolerance)
    {
        Assert.True(CapacitorCode.TryDecode(code, out var farads, out var decodedTolerance));
        Assert.Equal(expected, farads, tolerance: 1e-18);
        Assert.Equal(tolerance, decodedTolerance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("K")]
    [InlineData("1234")]
    [InlineData("abc")]
    [InlineData("n")]
    public void Capacitor_rejects_codes_it_does_not_know(string code) =>
        Assert.False(CapacitorCode.TryDecode(code, out _, out _));

    [Theory]
    [InlineData(10, 2.588, 5.261)]
    [InlineData(22, 0.644, 0.326)]
    [InlineData(36, 0.127, 0.0127)]
    [InlineData(0, 8.251, 53.48)]
    public void WireGauge_matches_published_tables(int awg, double diameter, double area)
    {
        Assert.Equal(diameter, WireGauge.DiameterMm(awg), 3);
        Assert.Equal(area, WireGauge.AreaSquareMm(awg), 2);
    }

    [Fact]
    public void WireGauge_names_the_aught_sizes()
    {
        Assert.Equal("4/0", WireGauge.Name(-3));
        Assert.Equal("1/0", WireGauge.Name(0));
        Assert.Equal("12", WireGauge.Name(12));
    }
}
