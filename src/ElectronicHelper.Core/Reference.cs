namespace ElectronicHelper.Core;

/// <summary>How a table is laid out on screen.</summary>
public enum TableStyle
{
    /// <summary>A grid with a header row. Suits a few short columns.</summary>
    Grid,

    /// <summary>One card per row, titled by the first column. Suits many or wordy columns.</summary>
    Cards,
}

/// <summary>
/// A table of reference data. <see cref="Swatches"/> gives an optional colour chip per row,
/// and <see cref="Links"/> an optional URL opened when the row is tapped.
/// </summary>
public sealed record RefTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    TableStyle Style = TableStyle.Grid,
    IReadOnlyList<float>? Weights = null,
    IReadOnlyList<string?>? Swatches = null,
    IReadOnlyList<string>? Links = null);

public sealed record RefSection(string? Heading, string? Text = null, RefTable? Table = null);

/// <summary>A reference page: an introduction followed by sections of text and tables.</summary>
public sealed record ReferenceDoc(
    string Id,
    string? Intro,
    IReadOnlyList<RefSection> Sections,
    bool Searchable = false,
    string? Footnote = null);

public static partial class ReferenceCatalog
{
    public static IReadOnlyList<ReferenceDoc> All { get; } =
    [
        FormulaSheet(), SiPrefixes(),
        ResistorColours(), CapacitorCodes(), LedVoltages(), Transistors(), IcPinouts(), SwitchTypes(),
        AwgTable(), Batteries(),
        ArduinoBoards(), ArduinoUnoPins(), RaspberryPiGpio(),
        UsbConnectors(), RfConnectors(), PlugTypes(), MainsByCountry(),
        LearningResources(),
    ];

    private static readonly Dictionary<string, ReferenceDoc> ById = All.ToDictionary(doc => doc.Id);

    public static ReferenceDoc? Find(string id) => ById.GetValueOrDefault(id);

    private static IReadOnlyList<IReadOnlyList<string>> Rows(params string[][] rows) => rows;
}
