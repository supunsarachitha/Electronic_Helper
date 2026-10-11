using ElectronicHelper.Core;

namespace ElectronicHelper.Core.Tests;

public class CatalogTests
{
    private static Inputs Numbers(params (string Key, double Value)[] values) =>
        new(values.ToDictionary(v => v.Key, v => v.Value));

    private static Inputs Text(string key, string text, params (string Key, int Index)[] choices) =>
        new(texts: new Dictionary<string, string> { [key] = text }, choices: choices.ToDictionary(c => c.Key, c => c.Index));

    private static string Line(Result result, string label) => result.Lines.Single(line => line.Label == label).Value;

    private static Calculator Get(string id) => CalculatorCatalog.Find(id) ?? throw new InvalidOperationException(id);

    [Fact]
    public void Every_tool_opens_something_that_exists()
    {
        foreach (var tool in ToolCatalog.All)
        {
            switch (tool.Kind)
            {
                case ToolKind.Calculator:
                    Assert.True(CalculatorCatalog.Find(tool.Id) is not null, $"No calculator for '{tool.Id}'.");
                    break;
                case ToolKind.Reference:
                    Assert.True(ReferenceCatalog.Find(tool.Id) is not null, $"No reference page for '{tool.Id}'.");
                    break;
            }
        }
    }

    [Fact]
    public void Every_calculator_and_reference_page_is_reachable_from_a_tool()
    {
        var toolIds = ToolCatalog.All.Select(tool => tool.Id).ToHashSet();

        Assert.All(CalculatorCatalog.All, calculator => Assert.Contains(calculator.Id, toolIds));
        Assert.All(ReferenceCatalog.All, doc => Assert.Contains(doc.Id, toolIds));
    }

    [Fact]
    public void Tool_ids_are_unique_and_glyphs_fit_the_badge()
    {
        Assert.Equal(ToolCatalog.All.Count, ToolCatalog.All.Select(tool => tool.Id).Distinct().Count());
        Assert.All(ToolCatalog.All, tool => Assert.InRange(tool.Glyph.Length, 1, 3));
    }

    [Fact]
    public void Every_calculator_asks_for_input_instead_of_failing_when_empty()
    {
        foreach (var calculator in CalculatorCatalog.All)
        {
            var result = calculator.Calculate(new Inputs());

            Assert.False(result.IsError, $"'{calculator.Id}' reports an error with no input.");
            Assert.Empty(result.Lines);
            Assert.False(string.IsNullOrEmpty(result.Message), $"'{calculator.Id}' gives no hint with no input.");
        }
    }

    [Fact]
    public void Every_calculator_survives_zeros_in_every_number_field()
    {
        foreach (var calculator in CalculatorCatalog.All)
        {
            var zeros = calculator.Fields.OfType<NumberField>().ToDictionary(field => field.Key, _ => 0.0);

            var result = calculator.Calculate(new Inputs(zeros));

            Assert.DoesNotContain(result.Lines, line => line.Value.Contains("NaN") || line.Value.Contains('∞'));
        }
    }

    [Fact]
    public void Field_keys_are_unique_within_each_calculator() =>
        Assert.All(CalculatorCatalog.All, calculator =>
            Assert.Equal(calculator.Fields.Count, calculator.Fields.Select(field => field.Key).Distinct().Count()));

    [Fact]
    public void Reference_tables_are_rectangular_and_their_extras_line_up()
    {
        foreach (var doc in ReferenceCatalog.All)
        foreach (var table in doc.Sections.Select(section => section.Table).OfType<RefTable>())
        {
            Assert.All(table.Rows, row => Assert.Equal(table.Headers.Count, row.Count));
            if (table.Weights is not null) Assert.Equal(table.Headers.Count, table.Weights.Count);
            if (table.Swatches is not null) Assert.Equal(table.Rows.Count, table.Swatches.Count);
            if (table.Links is not null) Assert.Equal(table.Rows.Count, table.Links.Count);
        }
    }

    [Fact]
    public void Reference_links_are_https() =>
        Assert.All(
            ReferenceCatalog.All.SelectMany(doc => doc.Sections).SelectMany(section => section.Table?.Links ?? []),
            link => Assert.StartsWith("https://", link));

    [Fact]
    public void Mains_table_is_alphabetical_and_has_no_duplicates()
    {
        var countries = ReferenceCatalog.Find("mains-by-country")!.Sections[0].Table!.Rows.Select(row => row[0]).ToArray();

        Assert.Equal(countries.OrderBy(name => name, StringComparer.Ordinal), countries);
        Assert.Equal(countries.Length, countries.Distinct().Count());
    }

    [Fact]
    public void OhmsLaw_shows_all_four_quantities()
    {
        var result = Get("ohms-law").Calculate(Numbers(("v", 12), ("r", 100)));

        Assert.Equal("120 mA", Line(result, "Current"));
        Assert.Equal("1.44 W", Line(result, "Power"));
    }

    [Fact]
    public void OhmsLaw_asks_to_clear_a_field_when_three_are_filled()
    {
        var result = Get("ohms-law").Calculate(Numbers(("v", 12), ("r", 100), ("i", 1)));

        Assert.Empty(result.Lines);
        Assert.False(result.IsError);
    }

    [Fact]
    public void OhmsLaw_reports_zero_resistance_as_an_error_rather_than_crashing()
    {
        var result = Get("ohms-law").Calculate(Numbers(("v", 12), ("r", 0)));

        Assert.True(result.IsError);
        Assert.Equal("Resistance must be greater than zero.", result.Message);
    }

    [Fact]
    public void LedResistor_rounds_up_to_a_standard_value_so_current_stays_within_the_limit()
    {
        // 9 V supply, 2 V LED, 20 mA needs 350 Ω; the next E12 value up is 390 Ω.
        // That resistor dissipates 126 mW, so a 2× margin calls for ½ W.
        var result = Get("led-resistor").Calculate(Numbers(("vs", 9), ("vf", 2), ("i", 0.02), ("n", 1)));

        Assert.Equal("350 Ω", Line(result, "Resistor needed"));
        Assert.Equal("390 Ω", Line(result, "Nearest standard value"));
        Assert.Equal("17.95 mA", Line(result, "Current with that resistor"));
        Assert.Equal("125.6 mW", Line(result, "Power in the resistor"));
        Assert.Equal("½ W", Line(result, "Resistor rating to use"));
    }

    [Fact]
    public void ResistorNetwork_reads_a_list_with_prefixes()
    {
        var result = Get("resistor-network").Calculate(Text("values", "1k 1k 2k"));

        Assert.Equal("4 kΩ", Line(result, "In series"));
        Assert.Equal("400 Ω", Line(result, "In parallel"));
    }

    [Fact]
    public void CapacitorNetwork_applies_the_chosen_unit_to_plain_numbers()
    {
        var result = Get("capacitor-network").Calculate(Text("values", "100 100", ("unit", 1)));

        Assert.Equal("200 nF", Line(result, "In parallel"));
        Assert.Equal("50 nF", Line(result, "In series"));
    }

    [Fact]
    public void RcFilter_solves_for_whichever_value_is_missing()
    {
        var filter = Get("rc-filter");

        Assert.Equal("159.2 Hz", Line(filter.Calculate(Numbers(("r", 1000), ("c", 1e-6))), "Cut-off frequency"));
        Assert.Equal("1 kΩ", Line(filter.Calculate(Numbers(("c", 1e-6), ("f", 159.1549))), "Resistance"));
        Assert.Equal("1 µF", Line(filter.Calculate(Numbers(("r", 1000), ("f", 159.1549))), "Capacitance"));
    }

    [Fact]
    public void LcResonance_solves_for_whichever_value_is_missing()
    {
        var resonance = Get("lc-resonance");

        Assert.Equal("5.033 kHz", Line(resonance.Calculate(Numbers(("l", 1e-3), ("c", 1e-6))), "Resonant frequency"));
        Assert.Equal("1 mH", Line(resonance.Calculate(Numbers(("c", 1e-6), ("f", 5032.92))), "Inductance"));
    }

    [Fact]
    public void Lm317_finds_the_resistor_for_a_target_voltage()
    {
        var result = Get("lm317").Calculate(Numbers(("r1", 240), ("vout", 5)));

        Assert.Equal("720 Ω", Line(result, "R2"));
        Assert.True(Get("lm317").Calculate(Numbers(("r1", 240), ("vout", 1))).IsError);
    }

    [Fact]
    public void RmsPeak_converts_from_any_one_value()
    {
        var converter = Get("rms-peak");

        Assert.Equal("325.3 V", Line(converter.Calculate(Numbers(("rms", 230))), "Peak"));
        Assert.Equal("230 V", Line(converter.Calculate(Numbers(("pp", 650.538))), "RMS"));
    }

    [Fact]
    public void FrequencyWavelength_converts_from_each_input()
    {
        var converter = Get("frequency-wavelength");

        Assert.Equal("2.998 m", Line(converter.Calculate(Numbers(("f", 100e6), ("vf", 1))), "Wavelength"));
        Assert.Equal("1 kHz", Line(converter.Calculate(Numbers(("t", 1e-3), ("vf", 1))), "Frequency"));
        Assert.Equal("100 MHz", Line(converter.Calculate(Numbers(("w", 2.99792458), ("vf", 1))), "Frequency"));
    }

    [Fact]
    public void WireVoltageDrop_counts_the_return_path()
    {
        // 10 m of 1 mm² each way is 20 m of copper: 0.3448 Ω, so 10 A drops 3.448 V.
        var sizeIndex = ((ChoiceField)Get("wire-voltage-drop").Fields[0]).Options.ToList().IndexOf("1 mm²");
        var inputs = new Inputs(
            new Dictionary<string, double> { ["len"] = 10, ["i"] = 10, ["vs"] = 12 },
            new Dictionary<string, int> { ["size"] = sizeIndex });

        var result = Get("wire-voltage-drop").Calculate(inputs);

        Assert.Equal("3.448 V", Line(result, "Voltage drop"));
        Assert.Equal("28.73%", Line(result, "Drop as a share of supply"));
    }

    [Fact]
    public void NumberBase_converts_hex_input()
    {
        var result = Get("number-base").Calculate(Text("value", "FF", ("base", 1)));

        Assert.Equal("255", Line(result, "Decimal"));
        Assert.Equal("1111 1111", Line(result, "Binary"));
    }

    [Fact]
    public void Adc_turns_a_reading_into_a_voltage()
    {
        var result = Get("adc").Calculate(Numbers(("bits", 10), ("vref", 5), ("reading", 512)));

        Assert.Equal("1,024", Line(result, "Number of levels"));
        Assert.Equal("2.5 V", Line(result, "Input voltage"));
        Assert.True(Get("adc").Calculate(Numbers(("bits", 10), ("vref", 5), ("reading", 1024))).IsError);
    }

    [Fact]
    public void Heatsink_says_so_when_no_heat_sink_is_enough()
    {
        var result = Get("heatsink").Calculate(Numbers(("p", 100), ("tj", 125), ("ta", 25), ("jc", 1.5), ("cs", 0.5)));

        Assert.True(result.IsError);
    }

    [Fact]
    public void Decibel_and_dbm_accept_negative_levels()
    {
        Assert.Equal("0.5012 ×", Line(Get("decibel").Calculate(Numbers(("db", -3))), "Power ratio"));
        Assert.Equal("1 µW", Line(Get("dbm").Calculate(Numbers(("dbm", -30))), "Power"));
    }

    [Fact]
    public void StandardValue_reports_how_far_off_the_nearest_value_is()
    {
        var result = Get("standard-value").Calculate(Text("value", "5k", ("series", 2)));

        Assert.Equal("5.1 k", Line(result, "Nearest standard value"));
        Assert.Equal("+2%", Line(result, "Difference"));
        Assert.Equal("4.7 k", Line(result, "Next lower"));
    }

    [Fact]
    public void Tool_search_matches_title_subtitle_and_keywords()
    {
        var ohmsLaw = ToolCatalog.Find("ohms-law")!;

        Assert.True(ohmsLaw.Matches("ohm"));
        Assert.True(ohmsLaw.Matches("WATT"));
        Assert.False(ohmsLaw.Matches("arduino"));
    }
}
