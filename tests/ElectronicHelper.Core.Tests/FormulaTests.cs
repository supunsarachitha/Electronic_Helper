using ElectronicHelper.Core;

namespace ElectronicHelper.Core.Tests;

public class FormulaTests
{
    // 12 V across 4 Ω draws 3 A and dissipates 36 W. Every pair of inputs must agree.
    [Theory]
    [InlineData(12.0, 3.0, null, null)]
    [InlineData(12.0, null, 4.0, null)]
    [InlineData(12.0, null, null, 36.0)]
    [InlineData(null, 3.0, 4.0, null)]
    [InlineData(null, 3.0, null, 36.0)]
    [InlineData(null, null, 4.0, 36.0)]
    public void OhmsLaw_solves_from_any_two_values(double? v, double? i, double? r, double? p)
    {
        var solved = Formulas.OhmsLaw(v, i, r, p);

        Assert.Equal(12, solved.Voltage, 9);
        Assert.Equal(3, solved.Current, 9);
        Assert.Equal(4, solved.Resistance, 9);
        Assert.Equal(36, solved.Power, 9);
    }

    [Fact]
    public void OhmsLaw_divides_voltage_by_resistance_for_current()
    {
        // Version 1.9 multiplied here, reporting 1200 A for 12 V across 100 Ω.
        Assert.Equal(0.12, Formulas.OhmsLaw(12, null, 100, null).Current, 9);
    }

    [Fact]
    public void OhmsLaw_divides_power_by_current_for_voltage()
    {
        // Version 1.9 multiplied here, reporting 200 V for 100 W at 2 A.
        Assert.Equal(50, Formulas.OhmsLaw(null, 2, null, 100).Voltage, 9);
    }

    [Theory]
    [InlineData(12.0, 0.0, null, null)]
    [InlineData(12.0, null, 0.0, null)]
    [InlineData(-5.0, 1.0, null, null)]
    public void OhmsLaw_rejects_zero_and_negative_values(double? v, double? i, double? r, double? p) =>
        Assert.Throws<CalculationException>(() => Formulas.OhmsLaw(v, i, r, p));

    [Theory]
    [InlineData(12.0, null, null, null)]
    [InlineData(12.0, 3.0, 4.0, null)]
    public void OhmsLaw_needs_exactly_two_values(double? v, double? i, double? r, double? p) =>
        Assert.Throws<CalculationException>(() => Formulas.OhmsLaw(v, i, r, p));

    [Fact]
    public void Resistors_add_in_series_and_combine_by_reciprocals_in_parallel()
    {
        Assert.Equal(600, Formulas.Sum([100, 200, 300]), 9);
        Assert.Equal(50, Formulas.ReciprocalSum([100, 100]), 9);
        Assert.Equal(687.5, Formulas.ReciprocalSum([1000, 2200]), 9);
    }

    [Fact]
    public void VoltageDivider_halves_the_supply_with_equal_resistors()
    {
        var divider = Formulas.VoltageDivider(10, 1000, 1000);

        Assert.Equal(5, divider.OutputVoltage, 9);
        Assert.Equal(0.005, divider.Current, 9);
    }

    [Fact]
    public void VoltageDivider_output_sags_when_loaded()
    {
        // A 1 kΩ load in parallel with the 1 kΩ lower resistor makes 500 Ω: 10 V × 500 ÷ 1500.
        Assert.Equal(10.0 / 3, Formulas.VoltageDivider(10, 1000, 1000, 1000).OutputVoltage, 9);
    }

    [Fact]
    public void LedResistor_drops_the_voltage_the_leds_do_not()
    {
        Assert.Equal(150, Formulas.LedResistor(5, 2, 0.02), 9);
        Assert.Equal(300, Formulas.LedResistor(12, 2, 0.02, 3), 9);
    }

    [Fact]
    public void LedResistor_rejects_a_supply_too_low_to_light_the_leds() =>
        Assert.Throws<CalculationException>(() => Formulas.LedResistor(5, 3.2, 0.02, 2));

    [Fact]
    public void RcCutoff_matches_a_known_filter()
    {
        // 1 kΩ and 1 µF: 1 ÷ (2π × 0.001) = 159.15 Hz.
        Assert.Equal(159.155, Formulas.RcCutoff(1000, 1e-6), 3);
    }

    [Fact]
    public void Reactances_match_known_values()
    {
        Assert.Equal(159.155, Formulas.CapacitiveReactance(1000, 1e-6), 3);
        Assert.Equal(62.832, Formulas.InductiveReactance(1000, 0.01), 3);
    }

    [Fact]
    public void Resonance_round_trips_between_frequency_and_components()
    {
        var frequency = Formulas.ResonantFrequency(1e-3, 1e-6);

        Assert.Equal(5032.92, frequency, 2);
        Assert.Equal(1e-3, Formulas.ResonantPartner(frequency, 1e-6), 12);
        Assert.Equal(1e-6, Formulas.ResonantPartner(frequency, 1e-3), 12);
    }

    [Fact]
    public void Timer555Astable_matches_the_datasheet_formula()
    {
        // R1 = 1 kΩ, R2 = 10 kΩ, C = 10 µF: f = 1.44 ÷ (21 kΩ × 10 µF) ≈ 6.87 Hz.
        var timer = Formulas.Timer555Astable(1000, 10_000, 10e-6);

        Assert.Equal(6.87, timer.Frequency, 2);
        Assert.Equal(11.0 / 21, timer.DutyCycle, 9);
        Assert.Equal(timer.Period, timer.High + timer.Low, 12);
    }

    [Fact]
    public void Timer555Monostable_is_about_1_1_times_RC() =>
        Assert.Equal(1.0986, Formulas.Timer555Monostable(100_000, 10e-6), 4);

    [Fact]
    public void Lm317_gives_5_volts_with_240_and_720_ohms() =>
        Assert.Equal(5, Formulas.Lm317Output(240, 720), 9);

    [Fact]
    public void Heatsink_subtracts_the_fixed_thermal_resistances()
    {
        // 10 W with a 100 °C budget allows 10 °C/W in total; 1.5 + 0.5 are already used.
        Assert.Equal(8, Formulas.HeatsinkThermalResistance(10, 125, 25, 1.5, 0.5), 9);
    }

    [Fact]
    public void PcbTraceWidth_matches_the_IPC_2221_chart()
    {
        // 1 A, 10 °C rise, 1 oz copper: 11.8 mil outside, about 2.6 times wider inside.
        var external = Formulas.PcbTraceWidth(1, 10, 1, externalLayer: true);
        var @internal = Formulas.PcbTraceWidth(1, 10, 1, externalLayer: false);

        Assert.Equal(11.8, external, 1);
        Assert.Equal(Math.Pow(2, 1 / 0.725), @internal / external, 6);
    }

    [Fact]
    public void CopperWireResistance_matches_the_AWG_tables()
    {
        // 1 mm² of copper is 17.24 Ω per kilometre.
        Assert.Equal(17.24, Formulas.CopperWireResistance(1, 1000), 6);
    }

    [Fact]
    public void Decibels_convert_both_ways()
    {
        Assert.Equal(3.0103, Formulas.DecibelsFromPowerRatio(2), 4);
        Assert.Equal(6.0206, Formulas.DecibelsFromVoltageRatio(2), 4);
        Assert.Equal(100, Formulas.PowerRatioFromDecibels(20), 9);
        Assert.Equal(10, Formulas.VoltageRatioFromDecibels(20), 9);
        Assert.Equal(0, Formulas.DbmFromWatts(1e-3), 9);
        Assert.Equal(1, Formulas.WattsFromDbm(30), 9);
    }

    [Fact]
    public void BatteryLife_scales_with_usable_capacity() =>
        Assert.Equal(80, Formulas.BatteryLifeHours(2, 0.02, 0.8), 9);

    [Fact]
    public void Wavelength_of_100_MHz_is_about_3_metres()
    {
        Assert.Equal(2.998, Formulas.Wavelength(100e6), 3);
        Assert.Equal(1.979, Formulas.Wavelength(100e6, 0.66), 3);
    }
}
