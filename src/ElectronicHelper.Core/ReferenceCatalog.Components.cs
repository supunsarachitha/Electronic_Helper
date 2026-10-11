using System.Globalization;

namespace ElectronicHelper.Core;

public static partial class ReferenceCatalog
{
    private const string DatasheetNote =
        "Typical values for quick reference. Ratings and pin order vary between manufacturers and packages, so check the datasheet for the exact part you have.";

    private static ReferenceDoc FormulaSheet() => new(
        "formulas",
        "The equations used most often on the bench.",
        [
            new("DC circuits", Table: new(["Formula", "Meaning"], Rows(
                ["V = I × R", "Ohm's law"],
                ["P = V × I = I² × R = V² ÷ R", "Power"],
                ["R = R1 + R2 + …", "Resistors in series"],
                ["1/R = 1/R1 + 1/R2 + …", "Resistors in parallel"],
                ["R = R1 × R2 ÷ (R1 + R2)", "Two resistors in parallel"],
                ["Vout = Vin × R2 ÷ (R1 + R2)", "Voltage divider"],
                ["I1 = I × R2 ÷ (R1 + R2)", "Current divider"]),
                Weights: [3, 2])),
            new("Capacitors and inductors", Table: new(["Formula", "Meaning"], Rows(
                ["Q = C × V", "Charge on a capacitor"],
                ["E = ½ × C × V²", "Energy in a capacitor"],
                ["E = ½ × L × I²", "Energy in an inductor"],
                ["C = C1 + C2 + …", "Capacitors in parallel"],
                ["1/C = 1/C1 + 1/C2 + …", "Capacitors in series"],
                ["τ = R × C", "RC time constant"],
                ["τ = L ÷ R", "RL time constant"]),
                Weights: [3, 2])),
            new("AC circuits", Table: new(["Formula", "Meaning"], Rows(
                ["Xc = 1 ÷ (2π × f × C)", "Capacitive reactance"],
                ["Xl = 2π × f × L", "Inductive reactance"],
                ["Z = √(R² + (Xl − Xc)²)", "Series RLC impedance"],
                ["f = 1 ÷ (2π × √(L × C))", "Resonant frequency"],
                ["fc = 1 ÷ (2π × R × C)", "RC filter cut-off"],
                ["Vrms = Vpeak ÷ √2", "RMS of a sine wave"],
                ["T = 1 ÷ f", "Period"],
                ["λ = c ÷ f", "Wavelength"]),
                Weights: [3, 2])),
            new("Gain and decibels", Table: new(["Formula", "Meaning"], Rows(
                ["dB = 10 × log(P2 ÷ P1)", "Power gain"],
                ["dB = 20 × log(V2 ÷ V1)", "Voltage gain"],
                ["dBm = 10 × log(P ÷ 1 mW)", "Power level"],
                ["G = 1 + Rf ÷ Rin", "Non-inverting op-amp"],
                ["G = −Rf ÷ Rin", "Inverting op-amp"]),
                Weights: [3, 2])),
            new("Handy numbers", Table: new(["Change", "Decibels"], Rows(
                ["Power × 2", "+3 dB"],
                ["Power × 10", "+10 dB"],
                ["Voltage × 2", "+6 dB"],
                ["Voltage × 10", "+20 dB"],
                ["Half-power point", "−3 dB"]))),
        ]);

    private static ReferenceDoc SiPrefixes() => new(
        "si-prefixes",
        "Prefixes scale a unit by powers of ten. 4.7 kΩ is 4,700 Ω; 100 nF is 0.1 µF.",
        [
            new(null, Table: new(["Prefix", "Symbol", "Factor", "Example"], Rows(
                ["tera", "T", "10¹²", "1 TΩ"],
                ["giga", "G", "10⁹", "2.4 GHz"],
                ["mega", "M", "10⁶", "1 MΩ"],
                ["kilo", "k", "10³", "4.7 kΩ"],
                ["(none)", "", "1", "5 V"],
                ["milli", "m", "10⁻³", "20 mA"],
                ["micro", "µ", "10⁻⁶", "100 µF"],
                ["nano", "n", "10⁻⁹", "100 nF"],
                ["pico", "p", "10⁻¹²", "22 pF"],
                ["femto", "f", "10⁻¹⁵", "1 fF"]),
                Weights: [2, 1.4f, 1.6f, 2])),
            new("Capacitor unit conversion", Table: new(["pF", "nF", "µF"], Rows(
                ["1", "0.001", "0.000001"],
                ["10", "0.01", "0.00001"],
                ["100", "0.1", "0.0001"],
                ["1,000", "1", "0.001"],
                ["10,000", "10", "0.01"],
                ["100,000", "100", "0.1"],
                ["1,000,000", "1,000", "1"]))),
            new("Units", Table: new(["Quantity", "Unit", "Symbol"], Rows(
                ["Voltage", "volt", "V"],
                ["Current", "ampere", "A"],
                ["Resistance", "ohm", "Ω"],
                ["Power", "watt", "W"],
                ["Capacitance", "farad", "F"],
                ["Inductance", "henry", "H"],
                ["Frequency", "hertz", "Hz"],
                ["Charge", "coulomb", "C"],
                ["Energy", "joule", "J"],
                ["Conductance", "siemens", "S"]),
                Weights: [2, 2, 1])),
        ]);

    private static ReferenceDoc ResistorColours()
    {
        var colours = ResistorColorCode.Colors;
        return new(
            "resistor-colours",
            "Read the bands from the end where they sit closest together. The gold or silver tolerance band goes on the right.",
            [
                new(null, Table: new(
                    ["Colour", "Digit", "Multiplier", "Tolerance", "ppm/°C"],
                    colours.Select(c => (IReadOnlyList<string>)
                    [
                        c.Name,
                        c.Digit?.ToString(CultureInfo.InvariantCulture) ?? "–",
                        "× " + (c.Multiplier < 1 ? Eng.Number(c.Multiplier) : Eng.Format(c.Multiplier, "").TrimEnd()),
                        c.TolerancePercent is { } tolerance ? $"±{Eng.Number(tolerance)}%" : "–",
                        c.TempCoefficient?.ToString(CultureInfo.InvariantCulture) ?? "–",
                    ]).ToArray(),
                    Weights: [2, 1.2f, 2, 2, 1.5f],
                    Swatches: colours.Select(c => (string?)c.Hex).ToArray())),
                new("How many bands?", Table: new(["Bands", "Meaning"], Rows(
                    ["3", "Two digits and a multiplier. Tolerance is ±20%."],
                    ["4", "Two digits, multiplier and tolerance. The common 5% type."],
                    ["5", "Three digits, multiplier and tolerance. Used for 1% parts."],
                    ["6", "As five bands, plus a temperature coefficient."]),
                    Weights: [1, 5])),
                new("Example", "Yellow, violet, red, gold is 4, 7, × 100, ±5%: a 4.7 kΩ resistor that may measure anywhere from 4.465 kΩ to 4.935 kΩ."),
            ]);
    }

    private static ReferenceDoc CapacitorCodes() => new(
        "capacitor-codes",
        "Small capacitors carry a three-digit code in picofarads: two digits, then the number of zeros to add.",
        [
            new("Common codes", Table: new(["Code", "pF", "nF", "µF"], Rows(
                ["100", "10", "0.01", "0.00001"],
                ["220", "22", "0.022", "0.000022"],
                ["470", "47", "0.047", "0.000047"],
                ["101", "100", "0.1", "0.0001"],
                ["221", "220", "0.22", "0.00022"],
                ["471", "470", "0.47", "0.00047"],
                ["102", "1,000", "1", "0.001"],
                ["222", "2,200", "2.2", "0.0022"],
                ["472", "4,700", "4.7", "0.0047"],
                ["103", "10,000", "10", "0.01"],
                ["223", "22,000", "22", "0.022"],
                ["473", "47,000", "47", "0.047"],
                ["104", "100,000", "100", "0.1"],
                ["224", "220,000", "220", "0.22"],
                ["474", "470,000", "470", "0.47"],
                ["105", "1,000,000", "1,000", "1"]))),
            new("Tolerance letter", Table: new(["Letter", "Tolerance"],
                CapacitorCode.ToleranceLetters
                    .Select(t => (IReadOnlyList<string>)[t.Letter.ToString(), t.Tolerance])
                    .ToArray())),
            new("Voltage code", "Some parts add a two-character voltage code before the value.", new(["Code", "Rating"], Rows(
                ["0J", "6.3 V"],
                ["1A", "10 V"],
                ["1C", "16 V"],
                ["1E", "25 V"],
                ["1H", "50 V"],
                ["2A", "100 V"],
                ["2D", "200 V"],
                ["2E", "250 V"],
                ["2G", "400 V"],
                ["2J", "630 V"]))),
            new("Ceramic dielectrics", Table: new(["Type", "Behaviour"], Rows(
                ["C0G / NP0", "Stable with temperature and voltage. Small values. Use for timing and filters."],
                ["X7R", "±15% from −55 to 125 °C. General-purpose decoupling."],
                ["X5R", "±15% from −55 to 85 °C. High capacitance in small packages."],
                ["Y5V / Z5U", "Loses most of its capacitance with temperature and voltage. Bulk decoupling only."]),
                TableStyle.Cards)),
        ]);

    private static ReferenceDoc LedVoltages() => new(
        "led-voltages",
        "The forward voltage an LED drops depends on its colour. Use it with the LED resistor calculator.",
        [
            new(null, Table: new(["Colour", "Forward voltage", "Wavelength"], Rows(
                ["Infrared", "1.2 – 1.6 V", "850 – 940 nm"],
                ["Red", "1.8 – 2.2 V", "620 – 750 nm"],
                ["Orange", "2.0 – 2.2 V", "590 – 620 nm"],
                ["Yellow", "2.0 – 2.2 V", "570 – 590 nm"],
                ["Green (standard)", "2.0 – 2.4 V", "560 – 570 nm"],
                ["Green (pure)", "3.0 – 3.4 V", "520 – 530 nm"],
                ["Blue", "3.0 – 3.4 V", "450 – 495 nm"],
                ["White", "3.0 – 3.4 V", "Blue chip with phosphor"],
                ["Ultraviolet", "3.1 – 4.0 V", "365 – 405 nm"]),
                Weights: [2, 2, 2.4f],
                Swatches: ["#5D4037", "#D32F2F", "#F57C00", "#FBC02D", "#7CB342", "#00C853", "#1976D2", "#F5F5F5", "#7B1FA2"])),
            new("Typical currents", Table: new(["LED type", "Current"], Rows(
                ["Low-current indicator", "2 mA"],
                ["Standard 3 mm or 5 mm", "10 – 20 mA"],
                ["High-brightness 5 mm", "20 – 30 mA"],
                ["1 W power LED", "350 mA"],
                ["3 W power LED", "700 mA"]))),
            new("Finding the pins", "On a through-hole LED the longer lead is the anode (+). The cathode (−) is the shorter lead, on the side where the rim is flat."),
        ],
        Footnote: DatasheetNote);

    private static ReferenceDoc Transistors() => new(
        "transistors",
        "Common small-signal and power transistors. Pins are listed left to right with the flat or printed face towards you and the leads pointing down.",
        [
            new("NPN bipolar", Table: new(["Part", "Package", "Max voltage", "Max current", "Pins"], Rows(
                ["2N3904", "TO-92", "40 V", "200 mA", "E · B · C"],
                ["PN2222A", "TO-92", "40 V", "600 mA", "E · B · C"],
                ["BC547", "TO-92", "45 V", "100 mA", "C · B · E"],
                ["BC337", "TO-92", "45 V", "800 mA", "C · B · E"],
                ["2N5551", "TO-92", "160 V", "600 mA", "E · B · C"],
                ["BD139", "TO-126", "80 V", "1.5 A", "E · C · B"],
                ["TIP31C", "TO-220", "100 V", "3 A", "B · C · E"],
                ["TIP41C", "TO-220", "100 V", "6 A", "B · C · E"],
                ["TIP120 (Darlington)", "TO-220", "60 V", "5 A", "B · C · E"]),
                TableStyle.Cards)),
            new("PNP bipolar", Table: new(["Part", "Package", "Max voltage", "Max current", "Pins"], Rows(
                ["2N3906", "TO-92", "40 V", "200 mA", "E · B · C"],
                ["PN2907A", "TO-92", "60 V", "600 mA", "E · B · C"],
                ["BC557", "TO-92", "45 V", "100 mA", "C · B · E"],
                ["BC327", "TO-92", "45 V", "800 mA", "C · B · E"],
                ["BD140", "TO-126", "80 V", "1.5 A", "E · C · B"],
                ["TIP32C", "TO-220", "100 V", "3 A", "B · C · E"],
                ["TIP42C", "TO-220", "100 V", "6 A", "B · C · E"]),
                TableStyle.Cards)),
            new("MOSFETs", Table: new(["Part", "Type", "Package", "Max voltage", "Max current", "Pins"], Rows(
                ["2N7000", "N-channel", "TO-92", "60 V", "200 mA", "S · G · D"],
                ["BS170", "N-channel", "TO-92", "60 V", "500 mA", "D · G · S"],
                ["IRF520N", "N-channel", "TO-220", "100 V", "9.7 A", "G · D · S"],
                ["IRF540N", "N-channel", "TO-220", "100 V", "33 A", "G · D · S"],
                ["IRLZ44N (logic level)", "N-channel", "TO-220", "55 V", "47 A", "G · D · S"],
                ["IRF9540N", "P-channel", "TO-220", "100 V", "23 A", "G · D · S"]),
                TableStyle.Cards)),
            new("Choosing between them", "A bipolar transistor is switched by base current: allow about one tenth of the collector current. A MOSFET is switched by gate voltage. To drive one straight from a 3.3 V or 5 V microcontroller pin, pick a logic-level part such as the IRLZ44N."),
        ],
        Footnote: DatasheetNote);

    private static ReferenceDoc IcPinouts() => new(
        "ic-pinouts",
        "Pin 1 is marked by a dot or sits to the left of the notch. Numbers run anticlockwise when viewed from above.",
        [
            new("555 timer (8-pin)", Table: new(["Pin", "Name", "Purpose"], Rows(
                ["1", "GND", "Ground"],
                ["2", "TRIG", "Starts timing when pulled below ⅓ of the supply"],
                ["3", "OUT", "Output"],
                ["4", "RESET", "Active low. Tie to the supply if unused"],
                ["5", "CTRL", "Control voltage. Bypass to ground with 10 nF"],
                ["6", "THR", "Ends timing when above ⅔ of the supply"],
                ["7", "DIS", "Discharges the timing capacitor"],
                ["8", "VCC", "Supply, 4.5 to 16 V"]),
                Weights: [1, 1.6f, 5])),
            new("LM358 dual op-amp (8-pin)", Table: new(["Pin", "Name", "Purpose"], Rows(
                ["1", "OUT A", "Output of amplifier A"],
                ["2", "IN A−", "Inverting input A"],
                ["3", "IN A+", "Non-inverting input A"],
                ["4", "GND", "Ground or negative supply"],
                ["5", "IN B+", "Non-inverting input B"],
                ["6", "IN B−", "Inverting input B"],
                ["7", "OUT B", "Output of amplifier B"],
                ["8", "VCC", "Positive supply, 3 to 32 V"]),
                Weights: [1, 1.6f, 5])),
            new("741 op-amp (8-pin)", Table: new(["Pin", "Name", "Purpose"], Rows(
                ["1", "NULL", "Offset null"],
                ["2", "IN−", "Inverting input"],
                ["3", "IN+", "Non-inverting input"],
                ["4", "V−", "Negative supply"],
                ["5", "NULL", "Offset null"],
                ["6", "OUT", "Output"],
                ["7", "V+", "Positive supply"],
                ["8", "NC", "Not connected"]),
                Weights: [1, 1.6f, 5])),
            new("Voltage regulators (TO-220)", "Pins left to right with the printed face towards you.", new(["Part", "Pin 1", "Pin 2", "Pin 3"], Rows(
                ["78xx (positive)", "Input", "Ground", "Output"],
                ["79xx (negative)", "Ground", "Input", "Output"],
                ["LM317 (adjustable)", "Adjust", "Output", "Input"],
                ["LM1117 / AMS1117", "Ground or adjust", "Output", "Input"]),
                Weights: [2.4f, 1.6f, 1.6f, 1.6f])),
            new("7400 quad NAND (14-pin)", "The same layout is used by the 7408 (AND), 7432 (OR) and 7486 (XOR). Y is the output of inputs A and B.", new(["Pin", "Name", "Pin", "Name"], Rows(
                ["1", "1A", "14", "VCC"],
                ["2", "1B", "13", "4B"],
                ["3", "1Y", "12", "4A"],
                ["4", "2A", "11", "4Y"],
                ["5", "2B", "10", "3B"],
                ["6", "2Y", "9", "3A"],
                ["7", "GND", "8", "3Y"]))),
        ],
        Footnote: DatasheetNote);

    private static ReferenceDoc SwitchTypes() => new(
        "switch-types",
        "Switches are described by poles (separate circuits switched together) and throws (positions each pole can connect to).",
        [
            new("Contact arrangements", Table: new(["Type", "Stands for", "What it does"], Rows(
                ["SPST", "Single pole, single throw", "A simple on/off switch with two terminals."],
                ["SPDT", "Single pole, double throw", "Connects one common terminal to either of two others. Also called a changeover switch."],
                ["DPST", "Double pole, single throw", "Two on/off switches worked by one lever, for breaking both supply wires at once."],
                ["DPDT", "Double pole, double throw", "Two changeover switches worked together. Often wired to reverse a DC motor."],
                ["SP3T, SP4T …", "Single pole, multiple throw", "One common terminal selects between several outputs, as in a rotary selector."]),
                TableStyle.Cards)),
            new("Action", Table: new(["Type", "What it does"], Rows(
                ["Latching", "Stays in the position you leave it in."],
                ["Momentary, normally open (NO)", "Makes contact only while pressed."],
                ["Momentary, normally closed (NC)", "Breaks contact only while pressed."],
                ["Centre-off (ON-OFF-ON)", "A three-position switch with nothing connected in the middle."]),
                TableStyle.Cards)),
            new("Common styles", Table: new(["Style", "Typical use"], Rows(
                ["Toggle", "Panel-mounted power and mode switches."],
                ["Rocker", "Mains power on equipment and extension leads."],
                ["Slide", "Small on/off and mode switches on circuit boards."],
                ["Tactile push button", "Reset and user buttons on circuit boards. Momentary."],
                ["DIP", "A row of tiny slide switches for setting addresses and options."],
                ["Rotary", "Selecting one of many positions, such as a range or input."],
                ["Microswitch", "Limit and door sensing. Needs very little force and travel."],
                ["Reed", "Closed by a nearby magnet. Used in door and window sensors."]),
                TableStyle.Cards)),
            new("Before you choose", "Check the voltage and current ratings for the kind of load. A switch rated for 250 V AC may be rated for only 30 V DC, because DC arcs do not extinguish themselves. Mechanical contacts also bounce for a few milliseconds, so debounce any switch that feeds digital logic."),
        ]);

    private static ReferenceDoc AwgTable()
    {
        var gauges = new[] { -3, -2, -1, 0, 1, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40 };
        return new(
            "awg-table",
            "American Wire Gauge sizes for solid copper. A smaller number is a thicker wire: every three gauges halves the cross-section.",
            [
                new(null, Table: new(
                    ["AWG", "Diameter", "Area", "Resistance"],
                    gauges.Select(awg => (IReadOnlyList<string>)
                    [
                        WireGauge.Name(awg),
                        Eng.Number(WireGauge.DiameterMm(awg), 3) + " mm",
                        Eng.Number(WireGauge.AreaSquareMm(awg), 3) + " mm²",
                        Eng.Number(WireGauge.OhmsPerKm(awg), 3) + " Ω/km",
                    ]).ToArray(),
                    Weights: [1, 1.6f, 1.8f, 2])),
                new("Metric sizes", Table: new(["Metric", "Usual AWG substitute"], Rows(
                    ["0.5 mm²", "20"],
                    ["0.75 mm²", "18"],
                    ["1.0 mm²", "17"],
                    ["1.5 mm²", "16"],
                    ["2.5 mm²", "14"],
                    ["4 mm²", "12"],
                    ["6 mm²", "10"],
                    ["10 mm²", "8"],
                    ["16 mm²", "6"]))),
            ],
            Footnote: "Resistance is for copper at 20 °C. Safe current depends on insulation, bundling and ambient temperature, so follow your local wiring regulations for mains cabling.");
    }

    private static ReferenceDoc Batteries() => new(
        "batteries",
        "Voltages are per cell. Multiply by the number of cells in series for a pack.",
        [
            new("Chemistries", Table: new(["Chemistry", "Nominal", "Fully charged", "Empty", "Rechargeable"], Rows(
                ["Alkaline", "1.5 V", "1.6 V", "0.9 V", "No"],
                ["Zinc-carbon", "1.5 V", "1.6 V", "0.9 V", "No"],
                ["Lithium primary (CR2032)", "3.0 V", "3.3 V", "2.0 V", "No"],
                ["NiMH", "1.2 V", "1.4 V", "1.0 V", "Yes"],
                ["NiCd", "1.2 V", "1.4 V", "1.0 V", "Yes"],
                ["Lithium-ion / LiPo", "3.7 V", "4.2 V", "3.0 V", "Yes"],
                ["LiFePO₄", "3.2 V", "3.65 V", "2.5 V", "Yes"],
                ["Lead-acid", "2.0 V", "2.1 V", "1.75 V", "Yes"]),
                TableStyle.Cards)),
            new("Common sizes", Table: new(["Size", "Voltage", "Typical capacity"], Rows(
                ["AAA alkaline", "1.5 V", "1,000 mAh"],
                ["AA alkaline", "1.5 V", "2,500 mAh"],
                ["AA NiMH", "1.2 V", "2,000 mAh"],
                ["C alkaline", "1.5 V", "7,000 mAh"],
                ["D alkaline", "1.5 V", "15,000 mAh"],
                ["9 V alkaline (PP3)", "9 V", "550 mAh"],
                ["CR2032 coin cell", "3 V", "220 mAh"],
                ["18650 lithium-ion", "3.7 V", "2,500 mAh"],
                ["12 V lead-acid (7 Ah)", "12 V", "7,000 mAh"]),
                Weights: [2.4f, 1.2f, 2])),
            new("Safety", "Never charge a lithium cell above 4.2 V or discharge it below about 3.0 V, and always use a protection circuit. A 12 V lead-acid battery reads about 12.6 V when full and 11.8 V when empty."),
        ],
        Footnote: "Capacity falls at high discharge currents and low temperatures. Figures are typical for new cells.");
}
