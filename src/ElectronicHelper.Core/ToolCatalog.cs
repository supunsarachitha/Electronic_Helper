namespace ElectronicHelper.Core;

public enum ToolCategory { Basics, Components, Signals, Power, Digital, Connectors, Learn }

public enum ToolKind
{
    /// <summary>A formula calculator from <see cref="CalculatorCatalog"/>.</summary>
    Calculator,

    /// <summary>A reference page from <see cref="ReferenceCatalog"/>.</summary>
    Reference,

    ResistorColorCode,
    LogicGates,
}

/// <summary>
/// An entry on the home screen. <see cref="Glyph"/> is a one-to-three character badge shown in place of an icon.
/// </summary>
public sealed record Tool(
    string Id,
    string Title,
    string Subtitle,
    string Glyph,
    ToolCategory Category,
    ToolKind Kind,
    string Keywords = "")
{
    public bool Matches(string query) =>
        Title.Contains(query, StringComparison.OrdinalIgnoreCase)
        || Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase)
        || Keywords.Contains(query, StringComparison.OrdinalIgnoreCase);
}

public static class ToolCatalog
{
    public static IReadOnlyList<Tool> All { get; } =
    [
        // Circuit basics
        Calc("ohms-law", "Ohm's law and power", "Any two of V, I, R and P", "Ω", ToolCategory.Basics, "voltage current resistance watt"),
        Calc("voltage-divider", "Voltage divider", "Output of two resistors, with a load", "V÷", ToolCategory.Basics, "potential divider"),
        Calc("resistor-network", "Resistors in series and parallel", "Total of any number of resistors", "ΣR", ToolCategory.Basics, "combine equivalent"),
        Calc("capacitor-network", "Capacitors in series and parallel", "Total of any number of capacitors", "ΣC", ToolCategory.Basics, "combine equivalent"),
        Calc("led-resistor", "LED series resistor", "Resistor, standard value and wattage", "LED", ToolCategory.Basics, "current limiting"),
        Calc("capacitor-energy", "Capacitor charge and energy", "Q = CV and E = ½CV²", "½C", ToolCategory.Basics, "joule coulomb stored"),
        Ref("formulas", "Formula sheet", "The equations used most often", "ƒ", ToolCategory.Basics, "equations cheat sheet"),
        Ref("si-prefixes", "SI prefixes and units", "pico to tera, and pF / nF / µF", "µ", ToolCategory.Basics, "micro nano kilo mega convert"),

        // Components
        new("resistor-color-code", "Resistor colour code", "Read 4, 5 and 6 band resistors", "|||", ToolCategory.Components, ToolKind.ResistorColorCode, "color bands stripes"),
        Calc("smd-resistor", "SMD resistor code", "103, 4R7, 4702 and EIA-96", "103", ToolCategory.Components, "surface mount marking chip"),
        Calc("capacitor-code", "Capacitor code", "104, 472K, 4n7", "104", ToolCategory.Components, "ceramic marking"),
        Calc("standard-value", "Standard value finder", "Nearest E6 to E96 value", "E24", ToolCategory.Components, "preferred e12 e96 series"),
        Ref("resistor-colours", "Resistor colour chart", "Digits, multipliers and tolerances", "0–9", ToolCategory.Components, "color table"),
        Ref("capacitor-codes", "Capacitor code chart", "Values, tolerance and voltage codes", "nF", ToolCategory.Components, "ceramic x7r c0g"),
        Ref("led-voltages", "LED voltages", "Forward voltage by colour", "Vf", ToolCategory.Components, "colour forward anode cathode"),
        Ref("transistors", "Transistors", "Ratings and pin order of common parts", "BJT", ToolCategory.Components, "npn pnp mosfet 2n2222 bc547 pinout"),
        Ref("ic-pinouts", "IC pinouts", "555, op-amps, regulators and 7400", "IC", ToolCategory.Components, "555 lm358 741 7805 lm317 pin"),
        Ref("switch-types", "Switch types", "SPST, SPDT, DPDT and more", "SW", ToolCategory.Components, "toggle momentary pole throw"),

        // Signals, filters and timing
        Calc("rc-time-constant", "RC time constant", "Charge time of a resistor and capacitor", "τ", ToolCategory.Signals, "tau charge discharge"),
        Calc("rc-filter", "RC filter", "Cut-off of a low-pass or high-pass filter", "fc", ToolCategory.Signals, "low pass high pass cutoff"),
        Calc("reactance", "Reactance", "Xc and Xl at a frequency", "X", ToolCategory.Signals, "impedance capacitive inductive"),
        Calc("lc-resonance", "LC resonance", "Any two of L, C and frequency", "LC", ToolCategory.Signals, "resonant tank tuned"),
        Calc("timer-555-astable", "555 astable", "Frequency and duty cycle", "555", ToolCategory.Signals, "oscillator timer ne555"),
        Calc("timer-555-monostable", "555 monostable", "One-shot pulse width", "1×", ToolCategory.Signals, "timer one shot ne555 delay"),
        Calc("rms-peak", "RMS and peak", "Sine wave peak, peak-to-peak and RMS", "~", ToolCategory.Signals, "ac sine amplitude"),
        Calc("frequency-wavelength", "Frequency and wavelength", "Period, wavelength and antenna lengths", "λ", ToolCategory.Signals, "antenna quarter wave radio"),
        Calc("opamp-gain", "Op-amp gain", "Inverting and non-inverting stages", "Av", ToolCategory.Signals, "amplifier operational"),
        Calc("decibel", "Decibels", "dB to power and voltage ratios", "dB", ToolCategory.Signals, "gain loss ratio"),
        Calc("dbm", "dBm and power", "dBm to milliwatts and back", "dBm", ToolCategory.Signals, "rf power level"),

        // Power, thermal and wiring
        Calc("battery-life", "Battery life", "Run time from capacity and load", "mAh", ToolCategory.Power, "runtime capacity"),
        Calc("lm317", "LM317 regulator", "Output voltage from R1 and R2", "317", ToolCategory.Power, "adjustable regulator"),
        Calc("linear-regulator", "Linear regulator heat", "Dissipation and efficiency", "78", ToolCategory.Power, "7805 ldo dissipation"),
        Calc("heatsink", "Heat sink", "Thermal resistance needed", "°C", ToolCategory.Power, "thermal cooling junction"),
        Calc("wire-voltage-drop", "Wire voltage drop", "Loss in a copper cable", "ΔV", ToolCategory.Power, "cable awg gauge loss"),
        Calc("pcb-trace-width", "PCB trace width", "IPC-2221 width for a current", "PCB", ToolCategory.Power, "track copper current"),
        Ref("awg-table", "Wire gauge table", "AWG diameter, area and resistance", "AWG", ToolCategory.Power, "cable wire size mm2"),
        Ref("batteries", "Batteries", "Cell voltages and capacities", "+ −", ToolCategory.Power, "lithium nimh alkaline 18650 cr2032"),

        // Digital and boards
        new("logic-gates", "Logic gates", "Try each gate and see its truth table", "&", ToolCategory.Digital, ToolKind.LogicGates, "and or not nand nor xor xnor boolean"),
        Calc("number-base", "Number base converter", "Decimal, hex, binary and octal", "0x", ToolCategory.Digital, "hexadecimal binary convert"),
        Calc("adc", "ADC resolution", "Step size and reading to voltage", "ADC", ToolCategory.Digital, "analog digital converter bits lsb"),
        Ref("arduino-boards", "Arduino boards", "Specifications side by side", "∞", ToolCategory.Digital, "uno nano mega leonardo due"),
        Ref("arduino-uno-pins", "Arduino UNO pins", "What every pin can do", "UNO", ToolCategory.Digital, "pinout pwm spi i2c"),
        Ref("raspberry-pi-gpio", "Raspberry Pi GPIO", "The 40-pin header", "Pi", ToolCategory.Digital, "pinout header gpio"),

        // Connectors and mains
        Ref("usb-connectors", "USB connectors", "Pinouts and power limits", "USB", ToolCategory.Connectors, "type-c micro pinout power delivery"),
        Ref("rf-connectors", "Coaxial connectors and cables", "BNC, SMA, N-type, RG-58 and more", "RF", ToolCategory.Connectors, "coax antenna sma bnc"),
        Ref("plug-types", "Mains plug types", "Types A to O", "A–O", ToolCategory.Connectors, "wall socket outlet"),
        Ref("mains-by-country", "Mains voltage by country", "Voltage, frequency and plugs", "230", ToolCategory.Connectors, "travel socket world"),

        // Learn
        Ref("learning-resources", "Learning resources", "Free textbooks, courses and guides", "Aa", ToolCategory.Learn, "books tutorials ebooks courses"),
    ];

    private static readonly Dictionary<string, Tool> ById = All.ToDictionary(tool => tool.Id);

    public static Tool? Find(string id) => ById.GetValueOrDefault(id);

    public static string CategoryName(ToolCategory category) => category switch
    {
        ToolCategory.Basics => "Circuit basics",
        ToolCategory.Components => "Components",
        ToolCategory.Signals => "Signals and timing",
        ToolCategory.Power => "Power and wiring",
        ToolCategory.Digital => "Digital and boards",
        ToolCategory.Connectors => "Connectors and mains",
        ToolCategory.Learn => "Learn",
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };

    private static Tool Calc(string id, string title, string subtitle, string glyph, ToolCategory category, string keywords = "") =>
        new(id, title, subtitle, glyph, category, ToolKind.Calculator, keywords);

    private static Tool Ref(string id, string title, string subtitle, string glyph, ToolCategory category, string keywords = "") =>
        new(id, title, subtitle, glyph, category, ToolKind.Reference, keywords);
}
