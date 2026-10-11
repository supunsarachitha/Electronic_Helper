namespace ElectronicHelper.Core;

/// <summary>Every formula calculator in the app, described as data.</summary>
public static class CalculatorCatalog
{
    private const string ExactlyTwo = "Clear a field: this needs exactly two values.";
    private const string ExactlyOne = "Clear a field: this needs exactly one value.";

    private static readonly (string Label, double AreaSquareMm)[] WireSizes =
    [
        .. new[] { 30, 28, 26, 24, 22, 20, 18, 16, 14, 12, 10, 8, 6, 4, 2, 0 }
            .Select(awg => ($"AWG {WireGauge.Name(awg)}", WireGauge.AreaSquareMm(awg))),
        .. new[] { 0.5, 0.75, 1, 1.5, 2.5, 4, 6, 10, 16, 25 }
            .Select(area => ($"{Eng.Number(area)} mm²", area)),
    ];

    private static readonly double[] CopperWeights = [0.5, 1, 2, 3];

    private static readonly (double Watts, string Label)[] ResistorRatings =
        [(0.125, "⅛ W"), (0.25, "¼ W"), (0.5, "½ W"), (1, "1 W"), (2, "2 W"), (5, "5 W"), (10, "10 W")];

    public static IReadOnlyList<Calculator> All { get; } =
    [
        OhmsLaw(), VoltageDivider(), ResistorNetwork(), CapacitorNetwork(), LedResistor(), CapacitorEnergy(),
        SmdResistor(), CapacitorMarking(), StandardValue(),
        RcTimeConstant(), RcFilter(), Reactance(), LcResonance(), Timer555Astable(), Timer555Monostable(),
        RmsPeak(), FrequencyWavelength(), OpAmpGain(), Decibel(), Dbm(),
        BatteryLife(), Lm317(), LinearRegulator(), Heatsink(), WireVoltageDrop(), PcbTraceWidth(),
        NumberBaseConverter(), Adc(),
    ];

    private static readonly Dictionary<string, Calculator> ById = All.ToDictionary(calculator => calculator.Id);

    public static Calculator? Find(string id) => ById.GetValueOrDefault(id);

    private static Calculator OhmsLaw() => new(
        "ohms-law",
        "V = I × R        P = V × I",
        "Enter any two of voltage, current, resistance and power. The other two are worked out for you.",
        [
            new NumberField("v", "Voltage (V)", Units.Voltage),
            new NumberField("i", "Current (I)", Units.Current),
            new NumberField("r", "Resistance (R)", Units.Resistance),
            new NumberField("p", "Power (P)", Units.Power),
        ],
        x =>
        {
            var count = x.Count("v", "i", "r", "p");
            if (count < 2) return Result.Hint("Enter any two values.");
            if (count > 2) return Result.Hint(ExactlyTwo);

            var s = Formulas.OhmsLaw(x.Get("v"), x.Get("i"), x.Get("r"), x.Get("p"));
            return Result.Of(
                new("Voltage", Eng.Format(s.Voltage, "V")),
                new("Current", Eng.Format(s.Current, "A")),
                new("Resistance", Eng.Format(s.Resistance, "Ω")),
                new("Power", Eng.Format(s.Power, "W")));
        });

    private static Calculator VoltageDivider() => new(
        "voltage-divider",
        "Vout = Vin × R2 ÷ (R1 + R2)",
        "R1 connects the supply to the output; R2 connects the output to ground. Add a load resistance to see how much it pulls the output down.",
        [
            new NumberField("vin", "Supply voltage (Vin)", Units.Voltage),
            new NumberField("r1", "R1 (top)", Units.Resistance.WithDefault("kΩ")),
            new NumberField("r2", "R2 (bottom)", Units.Resistance.WithDefault("kΩ")),
            new NumberField("rl", "Load resistance", Units.Resistance.WithDefault("kΩ"), Optional: true),
        ],
        x =>
        {
            if (!x.HasAll("vin", "r1", "r2")) return Result.Hint("Enter the supply voltage and both resistors.");

            var loaded = Formulas.VoltageDivider(x["vin"], x["r1"], x["r2"], x.Get("rl"));
            var lines = new List<ResultLine>
            {
                new("Output voltage", Eng.Format(loaded.OutputVoltage, "V")),
                new("Current drawn", Eng.Format(loaded.Current, "A")),
                new("Power in R1", Eng.Format(loaded.Current * loaded.Current * x["r1"], "W")),
                new("Power in R2", Eng.Format(loaded.OutputVoltage * loaded.OutputVoltage / x["r2"], "W")),
            };
            if (x.Has("rl"))
            {
                var unloaded = Formulas.VoltageDivider(x["vin"], x["r1"], x["r2"]);
                lines.Insert(1, new("Output with no load", Eng.Format(unloaded.OutputVoltage, "V")));
            }
            return new Result(lines);
        });

    private static Calculator ResistorNetwork() => new(
        "resistor-network",
        "Series: R = R1 + R2 + …\nParallel: 1/R = 1/R1 + 1/R2 + …",
        "Type the resistor values separated by spaces. Prefixes work: 4.7k, 4k7, 1M, 0R1.",
        [new TextField("values", "Resistor values (Ω)", "100 220 4.7k")],
        x =>
        {
            var text = x.Text("values");
            if (text.Length == 0) return Result.Hint("Enter two or more resistor values.");
            if (!Eng.TryParseList(text, out var values, out var invalid))
                return Result.Error($"\"{invalid}\" is not a value.");

            return Result.Of(
                new("In series", Eng.Format(Formulas.Sum(values), "Ω")),
                new("In parallel", Eng.Format(Formulas.ReciprocalSum(values), "Ω")),
                new("Resistors counted", values.Count.ToString()));
        });

    private static Calculator CapacitorNetwork() => new(
        "capacitor-network",
        "Parallel: C = C1 + C2 + …\nSeries: 1/C = 1/C1 + 1/C2 + …",
        "Type the capacitor values separated by spaces. Add a prefix (100n, 4.7u, 22p), or leave it off and choose the unit below.",
        [
            new TextField("values", "Capacitor values", "100n 4.7u 22p"),
            new ChoiceField("unit", "Values without a prefix are in", ["pF", "nF", "µF"], 2),
        ],
        x =>
        {
            var text = x.Text("values");
            if (text.Length == 0) return Result.Hint("Enter two or more capacitor values.");

            var plainFactor = x.Choice("unit") switch { 0 => 1e-12, 1 => 1e-9, _ => 1e-6 };
            if (!Eng.TryParseList(text, out var values, out var invalid, plainFactor))
                return Result.Error($"\"{invalid}\" is not a value.");

            return Result.Of(
                new("In parallel", Eng.Format(Formulas.Sum(values), "F")),
                new("In series", Eng.Format(Formulas.ReciprocalSum(values), "F")),
                new("Capacitors counted", values.Count.ToString()));
        });

    private static Calculator LedResistor() => new(
        "led-resistor",
        "R = (Vs − n × Vf) ÷ I",
        "Finds the series resistor that limits LED current. Red LEDs drop about 2 V; blue and white about 3.2 V. See the LED voltage table for others.",
        [
            new NumberField("vs", "Supply voltage (Vs)", Units.Voltage),
            new NumberField("vf", "LED forward voltage (Vf)", Units.Voltage, "2"),
            new NumberField("i", "LED current (I)", Units.Current.WithDefault("mA"), "20"),
            new NumberField("n", "LEDs in series (n)", Units.None, "1"),
        ],
        x =>
        {
            if (!x.HasAll("vs", "vf", "i", "n")) return Result.Hint("Enter the supply voltage, LED voltage and current.");
            if (x["n"] < 1 || x["n"] != Math.Floor(x["n"]))
                return Result.Error("The number of LEDs must be a whole number of at least 1.");

            var count = (int)x["n"];
            var exact = Formulas.LedResistor(x["vs"], x["vf"], x["i"], count);
            var standard = ESeries.Ceiling(exact, ESeriesKind.E12);
            var current = (x["vs"] - x["vf"] * count) / standard;
            var power = current * current * standard;
            var rating = ResistorRatings.FirstOrDefault(r => r.Watts >= power * 2).Label ?? "more than 10 W";

            return Result.Of(
                new("Resistor needed", Eng.Format(exact, "Ω")),
                new("Nearest standard value", Eng.Format(standard, "Ω")),
                new("Current with that resistor", Eng.Format(current, "A")),
                new("Power in the resistor", Eng.Format(power, "W")),
                new("Resistor rating to use", rating));
        });

    private static Calculator CapacitorEnergy() => new(
        "capacitor-energy",
        "Q = C × V        E = ½ × C × V²",
        "The charge and energy stored in a capacitor at a given voltage.",
        [
            new NumberField("c", "Capacitance (C)", Units.Capacitance),
            new NumberField("v", "Voltage (V)", Units.Voltage),
        ],
        x =>
        {
            if (!x.HasAll("c", "v")) return Result.Hint("Enter the capacitance and voltage.");
            var c = Guard.Positive(x["c"], "Capacitance");
            var v = Guard.Positive(x["v"], "Voltage");
            return Result.Of(
                new("Charge", Eng.Format(c * v, "C")),
                new("Energy", Eng.Format(Formulas.CapacitorEnergy(c, v), "J")));
        });

    private static Calculator SmdResistor() => new(
        "smd-resistor",
        "103 → 10 × 10³ = 10 kΩ",
        "Decodes the marking on a surface-mount resistor: 3-digit (103), 4-digit (4702), R as a decimal point (4R7) and EIA-96 (01C).",
        [new TextField("code", "Marking", "103")],
        x =>
        {
            var code = x.Text("code");
            if (code.Length == 0) return Result.Hint("Enter the code printed on the resistor.");
            if (!SmdResistorCode.TryDecode(code, out var ohms, out var marking))
                return Result.Error("That is not a marking this decoder recognises.");

            return Result.Of(
                new("Resistance", Eng.Format(ohms, "Ω")),
                new("Marking type", marking));
        });

    private static Calculator CapacitorMarking() => new(
        "capacitor-code",
        "104 → 10 × 10⁴ pF = 100 nF",
        "Decodes the marking on a ceramic or film capacitor: 3-digit (104), with a tolerance letter (472K), plain picofarads (47) and prefix style (4n7).",
        [new TextField("code", "Marking", "104")],
        x =>
        {
            var code = x.Text("code");
            if (code.Length == 0) return Result.Hint("Enter the code printed on the capacitor.");
            if (!CapacitorCode.TryDecode(code, out var farads, out var tolerance))
                return Result.Error("That is not a marking this decoder recognises.");

            var lines = new List<ResultLine>
            {
                new("Capacitance", Eng.Format(farads, "F")),
                new("In picofarads", Eng.Number(farads * 1e12, 6) + " pF"),
                new("In nanofarads", Eng.Number(farads * 1e9, 6) + " nF"),
                new("In microfarads", Eng.Number(farads * 1e6, 6) + " µF"),
            };
            if (tolerance is not null) lines.Add(new("Tolerance", tolerance));
            return new Result(lines);
        });

    private static Calculator StandardValue() => new(
        "standard-value",
        "E-series preferred values",
        "Finds the closest value you can actually buy. Works for resistors, capacitors and inductors. Prefixes work: 3.3k, 5100, 47n.",
        [
            new TextField("value", "Target value", "5.1k"),
            new ChoiceField("series", "Series", ["E6 (20%)", "E12 (10%)", "E24 (5%)", "E48 (2%)", "E96 (1%)"], 2),
        ],
        x =>
        {
            var text = x.Text("value");
            if (text.Length == 0) return Result.Hint("Enter the value you calculated.");
            if (!Eng.TryParse(text, out var target)) return Result.Error("That is not a value.");
            Guard.Positive(target, "The value");

            var kind = (ESeriesKind)x.Choice("series");
            var nearest = ESeries.Nearest(target, kind);
            var error = (nearest - target) / target * 100;
            return Result.Of(
                new("Nearest standard value", Eng.Format(nearest, "").TrimEnd()),
                new("Difference", $"{(error >= 0 ? "+" : "−")}{Eng.Number(Math.Abs(error), 3)}%"),
                new("Next lower", Eng.Format(ESeries.Floor(target, kind), "").TrimEnd()),
                new("Next higher", Eng.Format(ESeries.Ceiling(target, kind), "").TrimEnd()));
        });

    private static Calculator RcTimeConstant() => new(
        "rc-time-constant",
        "τ = R × C",
        "How fast a capacitor charges through a resistor. After one time constant it reaches 63%; after five it is effectively full. Add an elapsed time to see the charge at that moment.",
        [
            new NumberField("r", "Resistance (R)", Units.Resistance.WithDefault("kΩ")),
            new NumberField("c", "Capacitance (C)", Units.Capacitance),
            new NumberField("t", "Elapsed time", Units.Time.WithDefault("ms"), Optional: true),
        ],
        x =>
        {
            if (!x.HasAll("r", "c")) return Result.Hint("Enter the resistance and capacitance.");
            var tau = Guard.Positive(x["r"], "Resistance") * Guard.Positive(x["c"], "Capacitance");
            var lines = new List<ResultLine>
            {
                new("Time constant (τ)", Eng.Format(tau, "s")),
                new("Fully charged (5τ)", Eng.Format(5 * tau, "s")),
                new("Cut-off frequency", Eng.Format(Formulas.RcCutoff(x["r"], x["c"]), "Hz")),
            };
            if (x.Get("t") is { } elapsed)
            {
                var charged = (1 - Math.Exp(-Guard.NotNegative(elapsed, "Elapsed time") / tau)) * 100;
                lines.Add(new("Charge at that time", Eng.Number(charged) + "%"));
            }
            return new Result(lines);
        });

    private static Calculator RcFilter() => new(
        "rc-filter",
        "fc = 1 ÷ (2π × R × C)",
        "The −3 dB cut-off of an RC low-pass or high-pass filter. Enter any two values to find the third.",
        [
            new NumberField("r", "Resistance (R)", Units.Resistance.WithDefault("kΩ")),
            new NumberField("c", "Capacitance (C)", Units.Capacitance.WithDefault("nF")),
            new NumberField("f", "Cut-off frequency (fc)", Units.Frequency.WithDefault("kHz")),
        ],
        x =>
        {
            var count = x.Count("r", "c", "f");
            if (count < 2) return Result.Hint("Enter any two values.");
            if (count > 2) return Result.Hint(ExactlyTwo);

            RequirePositive(x, ("r", "Resistance"), ("c", "Capacitance"), ("f", "Frequency"));

            // R, C and fc are related symmetrically: each is 1 ÷ (2π × the other two).
            var r = x.Get("r") ?? Formulas.RcCutoff(x["c"], x["f"]);
            var c = x.Get("c") ?? Formulas.RcCutoff(x["r"], x["f"]);
            var f = x.Get("f") ?? Formulas.RcCutoff(x["r"], x["c"]);

            return Result.Of(
                new("Resistance", Eng.Format(r, "Ω")),
                new("Capacitance", Eng.Format(c, "F")),
                new("Cut-off frequency", Eng.Format(f, "Hz")),
                new("Time constant (τ)", Eng.Format(r * c, "s")));
        });

    private static Calculator Reactance() => new(
        "reactance",
        "Xc = 1 ÷ (2π × f × C)        Xl = 2π × f × L",
        "The opposition a capacitor or inductor presents to AC at a given frequency. Enter the frequency and a capacitance, an inductance, or both.",
        [
            new NumberField("f", "Frequency (f)", Units.Frequency.WithDefault("kHz")),
            new NumberField("c", "Capacitance (C)", Units.Capacitance, Optional: true),
            new NumberField("l", "Inductance (L)", Units.Inductance, Optional: true),
        ],
        x =>
        {
            if (!x.Has("f") || x.Count("c", "l") == 0)
                return Result.Hint("Enter the frequency and a capacitance or an inductance.");

            var lines = new List<ResultLine>();
            if (x.Get("c") is { } c)
                lines.Add(new("Capacitive reactance (Xc)", Eng.Format(Formulas.CapacitiveReactance(x["f"], c), "Ω")));
            if (x.Get("l") is { } l)
                lines.Add(new("Inductive reactance (Xl)", Eng.Format(Formulas.InductiveReactance(x["f"], l), "Ω")));
            if (x.HasAll("c", "l"))
            {
                var net = Formulas.InductiveReactance(x["f"], x["l"]) - Formulas.CapacitiveReactance(x["f"], x["c"]);
                var kind = net > 0 ? "inductive" : net < 0 ? "capacitive" : "resonant";
                lines.Add(new("In series", $"{Eng.Format(Math.Abs(net), "Ω")} {kind}"));
                lines.Add(new("They resonate at", Eng.Format(Formulas.ResonantFrequency(x["l"], x["c"]), "Hz")));
            }
            return new Result(lines);
        });

    private static Calculator LcResonance() => new(
        "lc-resonance",
        "f = 1 ÷ (2π × √(L × C))",
        "The resonant frequency of an inductor and capacitor. Enter any two values to find the third.",
        [
            new NumberField("l", "Inductance (L)", Units.Inductance.WithDefault("µH")),
            new NumberField("c", "Capacitance (C)", Units.Capacitance.WithDefault("pF")),
            new NumberField("f", "Resonant frequency (f)", Units.Frequency.WithDefault("MHz")),
        ],
        x =>
        {
            var count = x.Count("l", "c", "f");
            if (count < 2) return Result.Hint("Enter any two values.");
            if (count > 2) return Result.Hint(ExactlyTwo);

            RequirePositive(x, ("l", "Inductance"), ("c", "Capacitance"), ("f", "Frequency"));

            var l = x.Get("l") ?? Formulas.ResonantPartner(x["f"], x["c"]);
            var c = x.Get("c") ?? Formulas.ResonantPartner(x["f"], x["l"]);
            var f = x.Get("f") ?? Formulas.ResonantFrequency(x["l"], x["c"]);

            return Result.Of(
                new("Inductance", Eng.Format(l, "H")),
                new("Capacitance", Eng.Format(c, "F")),
                new("Resonant frequency", Eng.Format(f, "Hz")),
                new("Characteristic impedance", Eng.Format(Math.Sqrt(l / c), "Ω")));
        });

    private static Calculator Timer555Astable() => new(
        "timer-555-astable",
        "f = 1.44 ÷ ((R1 + 2 × R2) × C)",
        "A 555 wired as a free-running oscillator. R1 runs from the supply to pin 7, R2 from pin 7 to pins 6 and 2, and C from pins 6 and 2 to ground.",
        [
            new NumberField("r1", "R1", Units.Resistance.WithDefault("kΩ")),
            new NumberField("r2", "R2", Units.Resistance.WithDefault("kΩ")),
            new NumberField("c", "Capacitance (C)", Units.Capacitance),
        ],
        x =>
        {
            if (!x.HasAll("r1", "r2", "c")) return Result.Hint("Enter both resistors and the capacitor.");
            var s = Formulas.Timer555Astable(x["r1"], x["r2"], x["c"]);
            return Result.Of(
                new("Frequency", Eng.Format(s.Frequency, "Hz")),
                new("Period", Eng.Format(s.Period, "s")),
                new("Duty cycle", Eng.Number(s.DutyCycle * 100) + "%"),
                new("Time high", Eng.Format(s.High, "s")),
                new("Time low", Eng.Format(s.Low, "s")));
        });

    private static Calculator Timer555Monostable() => new(
        "timer-555-monostable",
        "t = 1.1 × R × C",
        "A 555 wired as a one-shot. A low pulse on the trigger (pin 2) drives the output high for this long.",
        [
            new NumberField("r", "Resistance (R)", Units.Resistance.WithDefault("kΩ")),
            new NumberField("c", "Capacitance (C)", Units.Capacitance),
        ],
        x => x.HasAll("r", "c")
            ? Result.Of(new ResultLine("Pulse width", Eng.Format(Formulas.Timer555Monostable(x["r"], x["c"]), "s")))
            : Result.Hint("Enter the resistor and capacitor."));

    private static Calculator RmsPeak() => new(
        "rms-peak",
        "Vrms = Vpeak ÷ √2",
        "Converts between the ways of stating a sine wave's size. Enter any one value.",
        [
            new NumberField("pk", "Peak", Units.Voltage),
            new NumberField("pp", "Peak-to-peak", Units.Voltage),
            new NumberField("rms", "RMS", Units.Voltage),
            new NumberField("avg", "Average (rectified)", Units.Voltage),
        ],
        x =>
        {
            var count = x.Count("pk", "pp", "rms", "avg");
            if (count == 0) return Result.Hint("Enter any one value.");
            if (count > 1) return Result.Hint(ExactlyOne);

            var peak = x.Get("pk")
                ?? x.Get("pp") / 2
                ?? x.Get("rms") * Math.Sqrt(2)
                ?? x["avg"] * Math.PI / 2;
            Guard.Positive(peak, "The value");

            return Result.Of(
                new("Peak", Eng.Format(peak, "V")),
                new("Peak-to-peak", Eng.Format(peak * 2, "V")),
                new("RMS", Eng.Format(peak / Math.Sqrt(2), "V")),
                new("Average (rectified)", Eng.Format(peak * 2 / Math.PI, "V")));
        });

    private static Calculator FrequencyWavelength() => new(
        "frequency-wavelength",
        "λ = c × VF ÷ f        T = 1 ÷ f",
        "Converts between frequency, period and wavelength. Enter any one. Set the velocity factor below 1 for a signal in cable, such as 0.66 for solid-polyethylene coax.",
        [
            new NumberField("f", "Frequency (f)", Units.Frequency.WithDefault("MHz")),
            new NumberField("t", "Period (T)", Units.Time.WithDefault("ms")),
            new NumberField("w", "Wavelength (λ)", Units.Length),
            new NumberField("vf", "Velocity factor (VF)", Units.None, "1"),
        ],
        x =>
        {
            var count = x.Count("f", "t", "w");
            if (count == 0) return Result.Hint("Enter a frequency, period or wavelength.");
            if (count > 1) return Result.Hint(ExactlyOne);

            var velocityFactor = x.Get("vf") ?? 1;
            if (velocityFactor is <= 0 or > 1) return Result.Error("The velocity factor must be between 0 and 1.");

            double frequency;
            if (x.Get("f") is { } f) frequency = Guard.Positive(f, "Frequency");
            else if (x.Get("t") is { } period) frequency = 1 / Guard.Positive(period, "Period");
            else frequency = Formulas.SpeedOfLight * velocityFactor / Guard.Positive(x["w"], "Wavelength");

            var wavelength = Formulas.Wavelength(frequency, velocityFactor);
            return Result.Of(
                new("Frequency", Eng.Format(frequency, "Hz")),
                new("Period", Eng.Format(1 / frequency, "s")),
                new("Wavelength", Eng.Format(wavelength, "m")),
                new("Half wave", Eng.Format(wavelength / 2, "m")),
                new("Quarter wave", Eng.Format(wavelength / 4, "m")));
        });

    private static Calculator OpAmpGain() => new(
        "opamp-gain",
        "Non-inverting: G = 1 + Rf ÷ Rin\nInverting: G = −Rf ÷ Rin",
        "The closed-loop gain of an op-amp stage. Rf is the feedback resistor from output to the inverting input. Rin goes from the inverting input to ground (non-inverting) or to the signal (inverting).",
        [
            new ChoiceField("mode", "Configuration", ["Non-inverting", "Inverting"]),
            new NumberField("rf", "Feedback resistor (Rf)", Units.Resistance.WithDefault("kΩ")),
            new NumberField("rin", "Input resistor (Rin)", Units.Resistance.WithDefault("kΩ")),
            new NumberField("vin", "Input voltage", Units.Voltage, Optional: true, Signed: true),
        ],
        x =>
        {
            if (!x.HasAll("rf", "rin")) return Result.Hint("Enter both resistors.");
            var ratio = Guard.Positive(x["rf"], "Rf") / Guard.Positive(x["rin"], "Rin");
            var gain = x.Choice("mode") == 0 ? 1 + ratio : -ratio;

            var lines = new List<ResultLine>
            {
                new("Gain", (gain < 0 ? "−" : "") + Eng.Number(Math.Abs(gain)) + " ×"),
                new("Gain in decibels", Signed(Formulas.DecibelsFromVoltageRatio(Math.Abs(gain))) + " dB"),
            };
            if (x.Get("vin") is not { } vin) return new Result(lines);

            lines.Add(new("Output voltage", SignedFormat(vin * gain, "V")));
            return new Result(lines, "The output cannot swing beyond the supply rails.");
        });

    private static Calculator Decibel() => new(
        "decibel",
        "dB = 10 × log(P2 ÷ P1) = 20 × log(V2 ÷ V1)",
        "Converts between a gain in decibels and the matching power and voltage ratios. Enter any one. Negative decibels are a loss.",
        [
            new NumberField("db", "Gain", Units.Decibel, Signed: true),
            new NumberField("pr", "Power ratio (P2 ÷ P1)", Units.None),
            new NumberField("vr", "Voltage ratio (V2 ÷ V1)", Units.None),
        ],
        x =>
        {
            var count = x.Count("db", "pr", "vr");
            if (count == 0) return Result.Hint("Enter any one value.");
            if (count > 1) return Result.Hint(ExactlyOne);

            var decibels = x.Get("db")
                ?? (x.Has("pr") ? Formulas.DecibelsFromPowerRatio(x["pr"]) : Formulas.DecibelsFromVoltageRatio(x["vr"]));

            return Result.Of(
                new("Gain", Signed(decibels) + " dB"),
                new("Power ratio", Eng.Number(Formulas.PowerRatioFromDecibels(decibels)) + " ×"),
                new("Voltage ratio", Eng.Number(Formulas.VoltageRatioFromDecibels(decibels)) + " ×"));
        });

    private static Calculator Dbm() => new(
        "dbm",
        "dBm = 10 × log(P ÷ 1 mW)",
        "Converts between power and dBm, the power level relative to one milliwatt. Enter either value.",
        [
            new NumberField("p", "Power", Units.Power.WithDefault("mW")),
            new NumberField("dbm", "Level", Units.DecibelMilliwatt, Signed: true),
        ],
        x =>
        {
            var count = x.Count("p", "dbm");
            if (count == 0) return Result.Hint("Enter a power or a level.");
            if (count > 1) return Result.Hint(ExactlyOne);

            var watts = x.Get("p") ?? Formulas.WattsFromDbm(x["dbm"]);
            return Result.Of(
                new("Power", Eng.Format(watts, "W")),
                new("Level", Signed(Formulas.DbmFromWatts(watts)) + " dBm"),
                new("RMS voltage into 50 Ω", Eng.Format(Math.Sqrt(watts * 50), "V")));
        });

    private static Calculator BatteryLife() => new(
        "battery-life",
        "t = capacity × usable ÷ current",
        "Estimates how long a battery lasts at a steady load. Usable capacity allows for cut-off voltage and ageing; 80% is a sensible default.",
        [
            new NumberField("cap", "Battery capacity", Units.Capacity),
            new NumberField("i", "Load current", Units.Current.WithDefault("mA")),
            new NumberField("usable", "Usable capacity", Units.Percent, "80"),
        ],
        x =>
        {
            if (!x.HasAll("cap", "i", "usable")) return Result.Hint("Enter the capacity and load current.");
            if (x["usable"] is <= 0 or > 100) return Result.Error("Usable capacity must be between 0 and 100%.");

            var hours = Formulas.BatteryLifeHours(x["cap"], x["i"], x["usable"] / 100);
            return Result.Of(
                new("Run time", Duration(hours)),
                new("In hours", Eng.Number(hours) + " h"));
        });

    private static Calculator Lm317() => new(
        "lm317",
        "Vout = 1.25 × (1 + R2 ÷ R1)",
        "Sets the output of an LM317 adjustable regulator. R1 goes from the output to the adjust pin, R2 from the adjust pin to ground. Enter any two values.",
        [
            new NumberField("r1", "R1", Units.Resistance, "240"),
            new NumberField("r2", "R2", Units.Resistance),
            new NumberField("vout", "Output voltage", Units.Voltage),
        ],
        x =>
        {
            var count = x.Count("r1", "r2", "vout");
            if (count < 2) return Result.Hint("Enter any two values.");
            if (count > 2) return Result.Hint(ExactlyTwo);

            if (x.Get("vout") is { } target && target <= Formulas.Lm317Reference)
                return Result.Error("An LM317 cannot regulate below 1.25 V.");

            var ratio = x.Has("vout") ? x["vout"] / Formulas.Lm317Reference - 1 : 0;
            var r1 = x.Get("r1") ?? Guard.Positive(x["r2"], "R2") / ratio;
            var r2 = x.Get("r2") ?? Guard.Positive(x["r1"], "R1") * ratio;

            return Result.Of(
                new("R1", Eng.Format(r1, "Ω")),
                new("R2", Eng.Format(r2, "Ω")),
                new("Output voltage", Eng.Format(Formulas.Lm317Output(r1, r2), "V")))
                .WithNote("Leaves out the small adjust-pin current. The input must be at least 3 V above the output.");
        });

    private static Calculator LinearRegulator() => new(
        "linear-regulator",
        "P = (Vin − Vout) × I",
        "The heat a linear regulator such as a 7805 has to get rid of, and how efficient it is.",
        [
            new NumberField("vin", "Input voltage", Units.Voltage),
            new NumberField("vout", "Output voltage", Units.Voltage),
            new NumberField("i", "Load current", Units.Current.WithDefault("mA")),
        ],
        x =>
        {
            if (!x.HasAll("vin", "vout", "i")) return Result.Hint("Enter both voltages and the load current.");
            var vout = Guard.Positive(x["vout"], "Output voltage");
            var current = Guard.Positive(x["i"], "Load current");
            if (x["vin"] <= vout) return Result.Error("The input voltage must be higher than the output.");

            return Result.Of(
                new("Heat dissipated", Eng.Format((x["vin"] - vout) * current, "W")),
                new("Power delivered", Eng.Format(vout * current, "W")),
                new("Efficiency", Eng.Number(vout / x["vin"] * 100) + "%"));
        });

    private static Calculator Heatsink() => new(
        "heatsink",
        "θSA = (Tj − Ta) ÷ P − θJC − θCS",
        "The largest thermal resistance a heat sink can have while keeping a part within its junction temperature. Lower numbers mean a bigger heat sink. θJC comes from the part's datasheet.",
        [
            new NumberField("p", "Power dissipated (P)", Units.Power),
            new NumberField("tj", "Junction limit (Tj)", Units.Celsius, "125"),
            new NumberField("ta", "Ambient temperature (Ta)", Units.Celsius, "25", Signed: true),
            new NumberField("jc", "Junction to case (θJC)", Units.ThermalResistance),
            new NumberField("cs", "Case to heat sink (θCS)", Units.ThermalResistance, "0.5"),
        ],
        x =>
        {
            if (!x.HasAll("p", "tj", "ta", "jc", "cs")) return Result.Hint("Enter the power and the junction-to-case resistance.");

            var required = Formulas.HeatsinkThermalResistance(x["p"], x["tj"], x["ta"], x["jc"], x["cs"]);
            if (required <= 0)
                return Result.Error("No heat sink can keep this part cool enough. Reduce the power or lower the ambient temperature.");

            return Result.Of(
                new("Heat sink must be at most", Eng.Number(required) + " °C/W"),
                new("Total junction to air", Eng.Number((x["tj"] - x["ta"]) / x["p"]) + " °C/W"));
        });

    private static Calculator WireVoltageDrop() => new(
        "wire-voltage-drop",
        "Vdrop = I × ρ × 2L ÷ A",
        "The voltage lost in a copper cable. The length is one way; the calculation covers the current's trip out and back. Add the supply voltage to see the drop as a percentage.",
        [
            new ChoiceField("size", "Wire size", WireSizes.Select(size => size.Label).ToArray(), 6),
            new NumberField("len", "Cable length (one way)", Units.Length),
            new NumberField("i", "Current", Units.Current),
            new NumberField("vs", "Supply voltage", Units.Voltage, Optional: true),
        ],
        x =>
        {
            if (!x.HasAll("len", "i")) return Result.Hint("Enter the cable length and current.");
            var area = WireSizes[x.Choice("size")].AreaSquareMm;
            var current = Guard.Positive(x["i"], "Current");
            var resistance = Formulas.CopperWireResistance(area, 2 * x["len"]);
            var drop = current * resistance;

            var lines = new List<ResultLine>
            {
                new("Voltage drop", Eng.Format(drop, "V")),
                new("Cable resistance (out and back)", Eng.Format(resistance, "Ω")),
                new("Power lost in the cable", Eng.Format(drop * current, "W")),
                new("Conductor area", Eng.Number(area) + " mm²"),
            };
            if (x.Get("vs") is { } supply)
            {
                Guard.Positive(supply, "Supply voltage");
                lines.Insert(1, new("Drop as a share of supply", Eng.Number(drop / supply * 100) + "%"));
                lines.Insert(2, new("Voltage at the load", Eng.Format(Math.Max(supply - drop, 0), "V")));
            }
            return new Result(lines, "Copper at 20 °C. Resistance rises about 0.4% per °C.");
        });

    private static Calculator PcbTraceWidth() => new(
        "pcb-trace-width",
        "I = k × ΔT^0.44 × A^0.725",
        "The narrowest PCB trace that carries a current without heating more than the rise you allow, from the IPC-2221 charts.",
        [
            new NumberField("i", "Current", Units.Current),
            new NumberField("dt", "Temperature rise allowed", Units.Celsius, "10"),
            new ChoiceField("cu", "Copper weight", ["0.5 oz (17.5 µm)", "1 oz (35 µm)", "2 oz (70 µm)", "3 oz (105 µm)"], 1),
            new ChoiceField("layer", "Layer", ["External", "Internal"]),
        ],
        x =>
        {
            if (!x.HasAll("i", "dt")) return Result.Hint("Enter the current.");
            var external = x.Choice("layer") == 0;
            var mils = Formulas.PcbTraceWidth(x["i"], x["dt"], CopperWeights[x.Choice("cu")], external);

            return Result.Of(
                new("Minimum width", Eng.Number(mils * 0.0254) + " mm"),
                new("In mils", Eng.Number(mils) + " mil"),
                new("Copper cross-section", Eng.Number(Formulas.PcbTraceArea(x["i"], x["dt"], external)) + " mil²"))
                .WithNote("IPC-2221 applies up to 35 A and a 100 °C rise. Treat the result as a minimum.");
        });

    private static Calculator NumberBaseConverter() => new(
        "number-base",
        "1010₂ = 10₁₀ = A₁₆",
        "Converts a whole number between decimal, hexadecimal, binary and octal. Handles values up to 64 bits.",
        [
            new ChoiceField("base", "The number is in", ["Decimal", "Hexadecimal", "Binary", "Octal"]),
            new TextField("value", "Number", "255"),
        ],
        x =>
        {
            var text = x.Text("value");
            if (text.Length == 0) return Result.Hint("Enter a whole number.");

            var radix = x.Choice("base") switch { 0 => 10, 1 => 16, 2 => 2, _ => 8 };
            if (!NumberBase.TryParse(text, radix, out var value))
                return Result.Error("That is not a valid number in this base, or it needs more than 64 bits.");

            return Result.Of(
                new("Decimal", NumberBase.ToDecimal(value)),
                new("Hexadecimal", NumberBase.ToHex(value)),
                new("Binary", NumberBase.ToBinary(value)),
                new("Octal", NumberBase.ToOctal(value)),
                new("Bits needed", NumberBase.BitLength(value).ToString()));
        });

    private static Calculator Adc() => new(
        "adc",
        "V = reading × Vref ÷ 2ⁿ",
        "The resolution of an analogue-to-digital converter. Add a reading to turn it back into a voltage. An Arduino UNO is 10 bits with a 5 V reference.",
        [
            new NumberField("bits", "Resolution (bits)", Units.None, "10"),
            new NumberField("vref", "Reference voltage", Units.Voltage, "5"),
            new NumberField("reading", "ADC reading", Units.None, Optional: true),
        ],
        x =>
        {
            if (!x.HasAll("bits", "vref")) return Result.Hint("Enter the resolution and reference voltage.");
            if (x["bits"] is < 1 or > 32 || x["bits"] != Math.Floor(x["bits"]))
                return Result.Error("The resolution must be a whole number of bits from 1 to 32.");

            var levels = Math.Pow(2, x["bits"]);
            var step = Guard.Positive(x["vref"], "Reference voltage") / levels;
            var lines = new List<ResultLine>
            {
                new("Step size (1 LSB)", Eng.Format(step, "V")),
                new("Number of levels", NumberBase.ToDecimal((ulong)levels)),
                new("Highest reading", NumberBase.ToDecimal((ulong)levels - 1)),
            };
            if (x.Get("reading") is { } reading)
            {
                if (reading < 0 || reading > levels - 1)
                    return Result.Error($"The reading must be between 0 and {NumberBase.ToDecimal((ulong)levels - 1)}.");
                lines.Add(new("Input voltage", Eng.Format(reading * step, "V")));
            }
            return new Result(lines);
        });

    /// <summary>Checks the fields that were filled in, so errors name the right one.</summary>
    private static void RequirePositive(Inputs x, params (string Key, string Name)[] fields)
    {
        foreach (var (key, name) in fields)
            if (x.Get(key) is { } value) Guard.Positive(value, name);
    }

    private static string Signed(double value) => (value < 0 ? "−" : "+") + Eng.Number(Math.Abs(value));

    private static string SignedFormat(double value, string unit) => (value < 0 ? "−" : "") + Eng.Format(Math.Abs(value), unit);

    private static string Duration(double hours) => hours switch
    {
        < 1.0 / 60 => Eng.Number(hours * 3600, 3) + " seconds",
        < 1 => Eng.Number(hours * 60, 3) + " minutes",
        < 48 => Eng.Number(hours, 3) + " hours",
        < 24 * 365 => Eng.Number(hours / 24, 3) + " days",
        _ => Eng.Number(hours / 24 / 365, 3) + " years",
    };
}
