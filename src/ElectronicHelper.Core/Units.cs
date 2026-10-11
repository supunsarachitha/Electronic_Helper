namespace ElectronicHelper.Core;

/// <summary>A selectable unit and the factor that converts it to the base unit.</summary>
public sealed record UnitOption(string Symbol, double Factor);

/// <summary>The units a number field can be entered in.</summary>
public sealed record UnitSet(IReadOnlyList<UnitOption> Options, int DefaultIndex = 0)
{
    public UnitSet WithDefault(string symbol)
    {
        for (var i = 0; i < Options.Count; i++)
            if (Options[i].Symbol == symbol) return this with { DefaultIndex = i };
        throw new ArgumentException($"Unknown unit '{symbol}'.", nameof(symbol));
    }
}

public static class Units
{
    public static readonly UnitSet Resistance = Set(1, ("mΩ", 1e-3), ("Ω", 1), ("kΩ", 1e3), ("MΩ", 1e6));
    public static readonly UnitSet Capacitance = Set(2, ("pF", 1e-12), ("nF", 1e-9), ("µF", 1e-6), ("mF", 1e-3), ("F", 1));
    public static readonly UnitSet Inductance = Set(2, ("nH", 1e-9), ("µH", 1e-6), ("mH", 1e-3), ("H", 1));
    public static readonly UnitSet Voltage = Set(1, ("mV", 1e-3), ("V", 1), ("kV", 1e3));
    public static readonly UnitSet Current = Set(2, ("µA", 1e-6), ("mA", 1e-3), ("A", 1));
    public static readonly UnitSet Power = Set(1, ("mW", 1e-3), ("W", 1), ("kW", 1e3));
    public static readonly UnitSet Frequency = Set(0, ("Hz", 1), ("kHz", 1e3), ("MHz", 1e6), ("GHz", 1e9));
    public static readonly UnitSet Time = Set(3, ("ns", 1e-9), ("µs", 1e-6), ("ms", 1e-3), ("s", 1));
    public static readonly UnitSet Length = Set(2, ("mm", 1e-3), ("cm", 1e-2), ("m", 1), ("in", 0.0254), ("ft", 0.3048));
    public static readonly UnitSet Capacity = Set(0, ("mAh", 1e-3), ("Ah", 1));
    public static readonly UnitSet Celsius = Set(0, ("°C", 1));
    public static readonly UnitSet ThermalResistance = Set(0, ("°C/W", 1));
    public static readonly UnitSet Percent = Set(0, ("%", 1));
    public static readonly UnitSet Decibel = Set(0, ("dB", 1));
    public static readonly UnitSet DecibelMilliwatt = Set(0, ("dBm", 1));
    public static readonly UnitSet None = Set(0, ("", 1));

    private static UnitSet Set(int defaultIndex, params (string Symbol, double Factor)[] options) =>
        new(options.Select(o => new UnitOption(o.Symbol, o.Factor)).ToArray(), defaultIndex);
}
