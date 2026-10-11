namespace ElectronicHelper.Core;

public readonly record struct OhmsLawValues(double Voltage, double Current, double Resistance, double Power);

public readonly record struct DividerValues(double OutputVoltage, double Current, double LowerResistance);

public readonly record struct AstableValues(double High, double Low, double Period, double Frequency, double DutyCycle);

/// <summary>The maths behind the calculators. All arguments and results are in base SI units.</summary>
public static class Formulas
{
    public const double SpeedOfLight = 299_792_458;

    /// <summary>Resistivity of annealed copper at 20 °C, in Ω·m.</summary>
    public const double CopperResistivity = 1.724e-8;

    /// <summary>LM317 reference voltage between the output and adjust pins.</summary>
    public const double Lm317Reference = 1.25;

    private const double TwoPi = 2 * Math.PI;

    /// <summary>Solves voltage, current, resistance and power from any two of them.</summary>
    public static OhmsLawValues OhmsLaw(double? voltage, double? current, double? resistance, double? power)
    {
        var known = new[] { voltage, current, resistance, power }.Count(value => value.HasValue);
        if (known != 2) throw new CalculationException("Enter exactly two values.");

        if (voltage is { } v) Guard.Positive(v, "Voltage");
        if (current is { } i) Guard.Positive(i, "Current");
        if (resistance is { } r) Guard.Positive(r, "Resistance");
        if (power is { } p) Guard.Positive(p, "Power");

        return (voltage, current, resistance, power) switch
        {
            ({ } volts, { } amps, _, _) => new(volts, amps, volts / amps, volts * amps),
            ({ } volts, _, { } ohms, _) => new(volts, volts / ohms, ohms, volts * volts / ohms),
            ({ } volts, _, _, { } watts) => new(volts, watts / volts, volts * volts / watts, watts),
            (_, { } amps, { } ohms, _) => new(amps * ohms, amps, ohms, amps * amps * ohms),
            (_, { } amps, _, { } watts) => new(watts / amps, amps, watts / (amps * amps), watts),
            (_, _, { } ohms, { } watts) => new(Math.Sqrt(watts * ohms), Math.Sqrt(watts / ohms), ohms, watts),
            _ => throw new CalculationException("Enter exactly two values."),
        };
    }

    /// <summary>Sum of values: resistors in series, capacitors in parallel.</summary>
    public static double Sum(IEnumerable<double> values) => values.Sum(value => Guard.Positive(value, "Each value"));

    /// <summary>Reciprocal of summed reciprocals: resistors in parallel, capacitors in series.</summary>
    public static double ReciprocalSum(IEnumerable<double> values) =>
        1 / values.Sum(value => 1 / Guard.Positive(value, "Each value"));

    public static DividerValues VoltageDivider(double supply, double upper, double lower, double? load = null)
    {
        Guard.Positive(supply, "Supply voltage");
        Guard.Positive(upper, "R1");
        Guard.Positive(lower, "R2");

        var effectiveLower = load is { } rl ? ReciprocalSum([lower, Guard.Positive(rl, "Load resistance")]) : lower;
        var current = supply / (upper + effectiveLower);
        return new(current * effectiveLower, current, effectiveLower);
    }

    public static double LedResistor(double supply, double forwardVoltage, double current, int count = 1)
    {
        Guard.Positive(supply, "Supply voltage");
        Guard.Positive(forwardVoltage, "Forward voltage");
        Guard.Positive(current, "LED current");
        if (count < 1) throw new CalculationException("There must be at least one LED.");

        var drop = supply - forwardVoltage * count;
        if (drop <= 0)
            throw new CalculationException("The supply voltage must be higher than the total LED forward voltage.");
        return drop / current;
    }

    public static double RcCutoff(double resistance, double capacitance) =>
        1 / (TwoPi * Guard.Positive(resistance, "Resistance") * Guard.Positive(capacitance, "Capacitance"));

    public static double CapacitiveReactance(double frequency, double capacitance) =>
        1 / (TwoPi * Guard.Positive(frequency, "Frequency") * Guard.Positive(capacitance, "Capacitance"));

    public static double InductiveReactance(double frequency, double inductance) =>
        TwoPi * Guard.Positive(frequency, "Frequency") * Guard.Positive(inductance, "Inductance");

    public static double ResonantFrequency(double inductance, double capacitance) =>
        1 / (TwoPi * Math.Sqrt(Guard.Positive(inductance, "Inductance") * Guard.Positive(capacitance, "Capacitance")));

    /// <summary>The inductance (or capacitance) that resonates with the other at a frequency.</summary>
    public static double ResonantPartner(double frequency, double other)
    {
        var omega = TwoPi * Guard.Positive(frequency, "Frequency");
        return 1 / (omega * omega * Guard.Positive(other, "The other component"));
    }

    public static AstableValues Timer555Astable(double r1, double r2, double capacitance)
    {
        Guard.Positive(r1, "R1");
        Guard.Positive(r2, "R2");
        Guard.Positive(capacitance, "Capacitance");

        var high = Math.Log(2) * (r1 + r2) * capacitance;
        var low = Math.Log(2) * r2 * capacitance;
        var period = high + low;
        return new(high, low, period, 1 / period, high / period);
    }

    public static double Timer555Monostable(double resistance, double capacitance) =>
        Math.Log(3) * Guard.Positive(resistance, "Resistance") * Guard.Positive(capacitance, "Capacitance");

    public static double CapacitorEnergy(double capacitance, double voltage) => 0.5 * capacitance * voltage * voltage;

    public static double Lm317Output(double r1, double r2) =>
        Lm317Reference * (1 + Guard.Positive(r2, "R2") / Guard.Positive(r1, "R1"));

    /// <summary>Largest heat sink thermal resistance that keeps the junction within its limit.</summary>
    public static double HeatsinkThermalResistance(
        double power, double maxJunction, double ambient, double junctionToCase, double caseToSink)
    {
        Guard.Positive(power, "Power");
        Guard.NotNegative(junctionToCase, "Junction-to-case resistance");
        Guard.NotNegative(caseToSink, "Case-to-sink resistance");
        if (maxJunction <= ambient)
            throw new CalculationException("The junction limit must be above the ambient temperature.");

        return (maxJunction - ambient) / power - junctionToCase - caseToSink;
    }

    /// <summary>IPC-2221 copper cross-section for a current and temperature rise, in square mils.</summary>
    public static double PcbTraceArea(double current, double temperatureRise, bool externalLayer)
    {
        Guard.Positive(current, "Current");
        Guard.Positive(temperatureRise, "Temperature rise");
        var k = externalLayer ? 0.048 : 0.024;
        return Math.Pow(current / (k * Math.Pow(temperatureRise, 0.44)), 1 / 0.725);
    }

    /// <summary>IPC-2221 trace width in mils. One ounce of copper is 1.378 mil thick.</summary>
    public static double PcbTraceWidth(double current, double temperatureRise, double copperOunces, bool externalLayer) =>
        PcbTraceArea(current, temperatureRise, externalLayer) / (Guard.Positive(copperOunces, "Copper weight") * 1.378);

    public static double CopperWireResistance(double areaSquareMm, double length) =>
        CopperResistivity * Guard.Positive(length, "Length") / (Guard.Positive(areaSquareMm, "Wire size") * 1e-6);

    public static double DecibelsFromPowerRatio(double ratio) => 10 * Math.Log10(Guard.Positive(ratio, "Power ratio"));

    public static double DecibelsFromVoltageRatio(double ratio) => 20 * Math.Log10(Guard.Positive(ratio, "Voltage ratio"));

    public static double PowerRatioFromDecibels(double decibels) => Math.Pow(10, decibels / 10);

    public static double VoltageRatioFromDecibels(double decibels) => Math.Pow(10, decibels / 20);

    public static double DbmFromWatts(double watts) => 10 * Math.Log10(Guard.Positive(watts, "Power") / 1e-3);

    public static double WattsFromDbm(double dbm) => 1e-3 * Math.Pow(10, dbm / 10);

    public static double BatteryLifeHours(double capacityAmpHours, double current, double usableFraction) =>
        Guard.Positive(capacityAmpHours, "Capacity") * usableFraction / Guard.Positive(current, "Load current");

    public static double Wavelength(double frequency, double velocityFactor = 1) =>
        SpeedOfLight * velocityFactor / Guard.Positive(frequency, "Frequency");
}
