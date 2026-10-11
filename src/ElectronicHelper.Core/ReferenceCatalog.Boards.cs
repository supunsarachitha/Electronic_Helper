namespace ElectronicHelper.Core;

public static partial class ReferenceCatalog
{
    // A property, not a field: field initialisers in other files of this partial class may not have run yet.
    private static string[] BoardHeaders =>
        ["Board", "Processor", "Logic level", "Digital pins", "PWM pins", "Analogue inputs", "Flash", "RAM", "Clock"];

    private static ReferenceDoc ArduinoBoards() => new(
        "arduino-boards",
        "The main specifications of popular Arduino boards. Never connect a 5 V signal to a 3.3 V board's pins.",
        [
            new("Classic boards", Table: new(BoardHeaders, Rows(
                ["UNO R3", "ATmega328P", "5 V", "14", "6", "6", "32 KB", "2 KB", "16 MHz"],
                ["Nano", "ATmega328P", "5 V", "14", "6", "8", "32 KB", "2 KB", "16 MHz"],
                ["Mini", "ATmega328P", "5 V", "14", "6", "8", "32 KB", "2 KB", "16 MHz"],
                ["Leonardo", "ATmega32U4", "5 V", "20", "7", "12", "32 KB", "2.5 KB", "16 MHz"],
                ["Micro", "ATmega32U4", "5 V", "20", "7", "12", "32 KB", "2.5 KB", "16 MHz"],
                ["Esplora", "ATmega32U4", "5 V", "Built-in controls", "–", "–", "32 KB", "2.5 KB", "16 MHz"],
                ["Yún", "ATmega32U4 + Linux module", "5 V", "20", "7", "12", "32 KB", "2.5 KB", "16 MHz"],
                ["Mega 2560", "ATmega2560", "5 V", "54", "15", "16", "256 KB", "8 KB", "16 MHz"],
                ["Due", "SAM3X8E (ARM Cortex-M3)", "3.3 V", "54", "12", "12", "512 KB", "96 KB", "84 MHz"],
                ["101", "Intel Curie", "3.3 V", "14", "4", "6", "196 KB", "24 KB", "32 MHz"],
                ["LilyPad", "ATmega328P", "2.7 – 5.5 V", "14", "6", "6", "32 KB", "2 KB", "8 MHz"]),
                TableStyle.Cards)),
            new("Current boards", Table: new(BoardHeaders, Rows(
                ["UNO R4 Minima", "Renesas RA4M1 (ARM Cortex-M4)", "5 V", "14", "6", "6", "256 KB", "32 KB", "48 MHz"],
                ["UNO R4 WiFi", "Renesas RA4M1 + ESP32-S3", "5 V", "14", "6", "6", "256 KB", "32 KB", "48 MHz"],
                ["Nano Every", "ATmega4809", "5 V", "14", "5", "8", "48 KB", "6 KB", "20 MHz"],
                ["Nano 33 IoT", "SAMD21 (ARM Cortex-M0+)", "3.3 V", "14", "11", "8", "256 KB", "32 KB", "48 MHz"],
                ["Nano ESP32", "ESP32-S3", "3.3 V", "14", "5", "8", "16 MB", "512 KB", "240 MHz"]),
                TableStyle.Cards)),
            new("Powering a board", "Most 5 V boards accept 7 to 12 V on the barrel jack or VIN pin, or 5 V over USB. Keep the current from any one pin of an ATmega board below 20 mA, and far lower on 3.3 V ARM boards."),
        ],
        Footnote: "Specifications are from the manufacturer's published figures and can differ between board revisions and clones.");

    private static ReferenceDoc ArduinoUnoPins() => new(
        "arduino-uno-pins",
        "What each pin on an Arduino UNO R3 can do. Pins marked ~ on the board support PWM.",
        [
            new("Digital pins", Table: new(["Pin", "Also used for"], Rows(
                ["D0", "Serial receive (RX)"],
                ["D1", "Serial transmit (TX)"],
                ["D2", "External interrupt 0"],
                ["D3 ~", "External interrupt 1, PWM"],
                ["D4", "–"],
                ["D5 ~", "PWM"],
                ["D6 ~", "PWM"],
                ["D7", "–"],
                ["D8", "–"],
                ["D9 ~", "PWM"],
                ["D10 ~", "PWM, SPI chip select (SS)"],
                ["D11 ~", "PWM, SPI data out (MOSI)"],
                ["D12", "SPI data in (MISO)"],
                ["D13", "SPI clock (SCK), built-in LED"]),
                Weights: [1, 3])),
            new("Analogue pins", "Each reads 0 to 5 V as 0 to 1023, and can also be used as a digital pin.", new(["Pin", "Also used for"], Rows(
                ["A0", "Digital pin 14"],
                ["A1", "Digital pin 15"],
                ["A2", "Digital pin 16"],
                ["A3", "Digital pin 17"],
                ["A4", "Digital pin 18, I²C data (SDA)"],
                ["A5", "Digital pin 19, I²C clock (SCL)"]),
                Weights: [1, 3])),
            new("Power pins", Table: new(["Pin", "Purpose"], Rows(
                ["VIN", "Unregulated input, 7 to 12 V"],
                ["5V", "Regulated 5 V output"],
                ["3.3V", "Regulated 3.3 V output, 50 mA at most"],
                ["GND", "Ground"],
                ["IOREF", "The board's logic voltage, for shields"],
                ["AREF", "Reference voltage for the analogue inputs"],
                ["RESET", "Pull low to reset the board"]),
                Weights: [1, 3])),
            new("Limits", "Each I/O pin can supply 20 mA continuously and 40 mA at the very most. The whole chip must stay under 200 mA."),
        ]);

    private static ReferenceDoc RaspberryPiGpio() => new(
        "raspberry-pi-gpio",
        "The 40-pin header found on every Raspberry Pi since the Model B+. Odd-numbered pins are on the inner row, nearest the middle of the board.",
        [
            new(null, Table: new(["Pin", "Function", "Pin", "Function"], Rows(
                ["1", "3.3 V", "2", "5 V"],
                ["3", "GPIO 2 (SDA)", "4", "5 V"],
                ["5", "GPIO 3 (SCL)", "6", "Ground"],
                ["7", "GPIO 4", "8", "GPIO 14 (TXD)"],
                ["9", "Ground", "10", "GPIO 15 (RXD)"],
                ["11", "GPIO 17", "12", "GPIO 18 (PWM)"],
                ["13", "GPIO 27", "14", "Ground"],
                ["15", "GPIO 22", "16", "GPIO 23"],
                ["17", "3.3 V", "18", "GPIO 24"],
                ["19", "GPIO 10 (MOSI)", "20", "Ground"],
                ["21", "GPIO 9 (MISO)", "22", "GPIO 25"],
                ["23", "GPIO 11 (SCLK)", "24", "GPIO 8 (CE0)"],
                ["25", "Ground", "26", "GPIO 7 (CE1)"],
                ["27", "GPIO 0 (ID_SD)", "28", "GPIO 1 (ID_SC)"],
                ["29", "GPIO 5", "30", "Ground"],
                ["31", "GPIO 6", "32", "GPIO 12 (PWM)"],
                ["33", "GPIO 13 (PWM)", "34", "Ground"],
                ["35", "GPIO 19 (PWM)", "36", "GPIO 16"],
                ["37", "GPIO 26", "38", "GPIO 20"],
                ["39", "Ground", "40", "GPIO 21"]),
                Weights: [1, 3, 1, 3])),
            new("Limits", "GPIO pins use 3.3 V logic and are not 5 V tolerant: a 5 V signal can destroy the processor. Draw no more than 16 mA from one pin and about 50 mA from all of them together. Pins 27 and 28 are reserved for add-on board identification."),
        ]);

    private static ReferenceDoc UsbConnectors() => new(
        "usb-connectors",
        "Pin functions and power limits for USB connectors.",
        [
            new("USB 2.0 Type-A and Type-B", Table: new(["Pin", "Signal", "Wire colour"], Rows(
                ["1", "VBUS (+5 V)", "Red"],
                ["2", "D−", "White"],
                ["3", "D+", "Green"],
                ["4", "Ground", "Black"]),
                Swatches: ["#D32F2F", "#F5F5F5", "#388E3C", "#1B1B1B"])),
            new("Mini-USB and Micro-USB", Table: new(["Pin", "Signal", "Wire colour"], Rows(
                ["1", "VBUS (+5 V)", "Red"],
                ["2", "D−", "White"],
                ["3", "D+", "Green"],
                ["4", "ID (grounded on an OTG host plug)", "–"],
                ["5", "Ground", "Black"]),
                Swatches: ["#D32F2F", "#F5F5F5", "#388E3C", null, "#1B1B1B"])),
            new("USB Type-C", "The 24-pin plug works either way up, so most signals appear twice.", new(["Signal", "Pins", "Purpose"], Rows(
                ["Ground", "A1, A12, B1, B12", "Return"],
                ["VBUS", "A4, A9, B4, B9", "Power, 5 to 48 V"],
                ["CC1, CC2", "A5, B5", "Detects orientation and negotiates power"],
                ["D+, D−", "A6, A7, B6, B7", "USB 2.0 data"],
                ["SBU1, SBU2", "A8, B8", "Sideband, used by alternate modes"],
                ["TX and RX pairs", "A2, A3, B2, B3, A10, A11, B10, B11", "High-speed data"]),
                TableStyle.Cards)),
            new("How much power?", Table: new(["Source", "Voltage", "Current", "Power"], Rows(
                ["USB 2.0 port", "5 V", "500 mA", "2.5 W"],
                ["USB 3.x port", "5 V", "900 mA", "4.5 W"],
                ["Charging port (BC 1.2)", "5 V", "1.5 A", "7.5 W"],
                ["Type-C, default", "5 V", "3 A", "15 W"],
                ["Power Delivery", "Up to 20 V", "5 A", "100 W"],
                ["Power Delivery 3.1 (EPR)", "Up to 48 V", "5 A", "240 W"]),
                TableStyle.Cards)),
            new("Type-C as a simple 5 V input", "A device that only needs 5 V from a Type-C socket must connect a 5.1 kΩ resistor from each CC pin to ground. Without them a Type-C charger supplies no power."),
        ]);

    private static ReferenceDoc RfConnectors() => new(
        "rf-connectors",
        "Coaxial connectors and cables, with their impedance and the highest frequency they are normally used at.",
        [
            new("Connectors", Table: new(["Connector", "Impedance", "Up to", "Coupling", "Typical use"], Rows(
                ["BNC", "50 or 75 Ω", "4 GHz", "Bayonet", "Test equipment, video"],
                ["SMA", "50 Ω", "18 GHz", "Threaded", "Wi-Fi, cellular and GPS antennas, RF modules"],
                ["RP-SMA", "50 Ω", "18 GHz", "Threaded", "Wi-Fi routers. The centre pin and socket are swapped"],
                ["N-type", "50 Ω", "11 GHz", "Threaded", "Base stations and outdoor antennas"],
                ["TNC", "50 Ω", "11 GHz", "Threaded", "A threaded BNC, for use where there is vibration"],
                ["F-type", "75 Ω", "1 GHz", "Threaded", "Television, satellite and cable modems"],
                ["UHF (PL-259 / SO-239)", "Not constant", "300 MHz", "Threaded", "HF and VHF amateur radio"],
                ["MCX", "50 Ω", "6 GHz", "Snap-on", "GPS receivers, small radio modules"],
                ["MMCX", "50 Ω", "6 GHz", "Snap-on", "Very small modules. The plug can rotate"],
                ["U.FL / IPEX", "50 Ω", "6 GHz", "Snap-on", "Antennas inside laptops, phones and modules"],
                ["RCA", "Not constant", "10 MHz", "Push-on", "Consumer audio and composite video"]),
                TableStyle.Cards)),
            new("Coaxial cables", Table: new(["Cable", "Impedance", "Outside diameter", "Typical use"], Rows(
                ["RG-174", "50 Ω", "2.8 mm", "Short, flexible jumpers"],
                ["RG-58", "50 Ω", "5 mm", "General radio and lab leads"],
                ["RG-213 / RG-8", "50 Ω", "10.3 mm", "Low-loss transmitter feeds"],
                ["LMR-400", "50 Ω", "10.3 mm", "Low-loss runs to outdoor antennas"],
                ["RG-59", "75 Ω", "6.1 mm", "CCTV and short video runs"],
                ["RG-6", "75 Ω", "6.9 mm", "Television, satellite and cable"],
                ["RG-11", "75 Ω", "10.3 mm", "Long television distribution runs"]),
                TableStyle.Cards)),
            new("Matching", "Use 50 Ω parts for radio and test equipment and 75 Ω parts for video and television. Mixing the two reflects part of the signal back down the cable."),
        ]);

    private static ReferenceDoc PlugTypes() => new(
        "plug-types",
        "Mains plugs are identified by a letter. Look up a country under Mains voltage by country to see which it uses.",
        [
            new(null, Table: new(["Type", "Pins", "Earthed", "Rating", "Used in"], Rows(
                ["A", "Two flat parallel pins", "No", "15 A", "North and Central America, Japan"],
                ["B", "Two flat parallel pins and a round earth pin", "Yes", "15 A", "North and Central America, Japan"],
                ["C", "Two round pins (the Europlug)", "No", "2.5 A", "Europe, South America and much of Asia"],
                ["D", "Three large round pins in a triangle", "Yes", "5 A", "India, Nepal, Sri Lanka"],
                ["E", "Two round pins and a hole for the socket's earth pin", "Yes", "16 A", "France, Belgium, Poland, Czechia, Slovakia"],
                ["F", "Two round pins with earth clips on the sides (Schuko)", "Yes", "16 A", "Germany and most of Europe, Russia"],
                ["G", "Three rectangular pins, with a fuse in the plug", "Yes", "13 A", "United Kingdom, Ireland, Malaysia, Singapore, Sri Lanka"],
                ["H", "Three round pins in a triangle", "Yes", "16 A", "Israel"],
                ["I", "Two flat pins in a V, with an optional earth pin", "Optional", "10 A", "Australia, New Zealand, China, Argentina"],
                ["J", "Three round pins, the earth pin offset", "Yes", "10 A", "Switzerland, Liechtenstein"],
                ["K", "Two round pins and a half-round earth pin", "Yes", "16 A", "Denmark, Greenland"],
                ["L", "Three round pins in a row", "Yes", "10 A or 16 A", "Italy, Chile"],
                ["M", "Three large round pins in a triangle, bigger than type D", "Yes", "15 A", "South Africa, and for large appliances in India"],
                ["N", "Three round pins, the earth pin slightly offset", "Yes", "10 A or 20 A", "Brazil, South Africa"],
                ["O", "Three round pins in a triangle", "Yes", "16 A", "Thailand"]),
                TableStyle.Cards)),
            new("Which plugs fit which sockets?", "A type C plug fits type E, F, J, K, L, N and O sockets. Type E and F plugs are normally made as one combined plug that fits both sockets. A type A plug fits a type B socket."),
        ]);

    private static ReferenceDoc LearningResources()
    {
        (string Title, string About, string Url)[] links =
        [
            ("Lessons in Electric Circuits", "A complete free textbook, from DC basics to digital and semiconductors.", "https://www.allaboutcircuits.com/textbook/"),
            ("Electronics (Wikibooks)", "An open textbook written and maintained by volunteers.", "https://en.wikibooks.org/wiki/Electronics"),
            ("Electronics Tutorials", "Short illustrated tutorials on components, amplifiers, filters and logic.", "https://www.electronics-tutorials.ws/"),
            ("Khan Academy: Electrical engineering", "Free video lessons on circuit analysis, signals and electromagnetism.", "https://www.khanacademy.org/science/electrical-engineering"),
            ("MIT OpenCourseWare: Circuits and Electronics", "The full MIT 6.002 course, with lecture videos and problem sets.", "https://ocw.mit.edu/courses/6-002-circuits-and-electronics-spring-2007/"),
            ("SparkFun tutorials", "Practical beginner guides to components, soldering and microcontrollers.", "https://learn.sparkfun.com/tutorials"),
            ("Adafruit Learning System", "Step-by-step project guides for sensors, displays and boards.", "https://learn.adafruit.com/"),
            ("Arduino documentation", "Official guides, board references and the programming language reference.", "https://docs.arduino.cc/"),
            ("Raspberry Pi documentation", "Official hardware and software guides for every Raspberry Pi.", "https://www.raspberrypi.com/documentation/"),
        ];

        return new(
            "learning-resources",
            "Free places to learn electronics properly. Tap a card to open it in your browser.",
            [
                new(null, Table: new(
                    ["Resource", "About"],
                    links.Select(link => (IReadOnlyList<string>)[link.Title, link.About]).ToArray(),
                    TableStyle.Cards,
                    Links: links.Select(link => link.Url).ToArray())),
            ],
            Footnote: "These are independent websites. Electronic Helper is not affiliated with them.");
    }
}
