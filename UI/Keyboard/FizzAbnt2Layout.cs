namespace RedragonFizzMacro.UI.Keyboard;

public static class FizzAbnt2Layout
{
    public const float TotalRowUnits = 15.0f;
    public const int TotalRows = 5;

    public static IReadOnlyList<KeycapDef> Keys { get; } = BuildLayout();

    private static List<KeycapDef> BuildLayout()
    {
        var list = new List<KeycapDef>(64);

        void AddRow(int row, params (int vk, string top, string sub, float w, bool iso, bool fn1, bool fn2)[] items)
        {
            float x = 0f;
            foreach (var it in items)
            {
                list.Add(new KeycapDef(it.vk, it.top, it.sub, row, x, it.w, it.iso, it.fn1, it.fn2));
                x += it.w;
            }
        }

        // Row 0 (15u)
        AddRow(0,
            (0x1B, "Esc", "", 1f, false, false, false),
            (0x31, "1", "", 1f, false, false, false),
            (0x32, "2", "", 1f, false, false, false),
            (0x33, "3", "", 1f, false, false, false),
            (0x34, "4", "", 1f, false, false, false),
            (0x35, "5", "", 1f, false, false, false),
            (0x36, "6", "", 1f, false, false, false),
            (0x37, "7", "", 1f, false, false, false),
            (0x38, "8", "", 1f, false, false, false),
            (0x39, "9", "Led", 1f, false, false, false),
            (0x30, "0", "Off", 1f, false, false, false),
            (0xBD, "-", "Br−", 1f, false, false, false),
            (0xBB, "=", "Br+", 1f, false, false, false),
            (0x08, "Back", "", 2f, false, false, false)
        );

        // Row 1 (15u - includes ABNT2/ISO Enter spanning row 1 & 2)
        AddRow(1,
            (0x09, "Tab", "", 1.5f, false, false, false),
            (0x51, "Q", "", 1f, false, false, false),
            (0x57, "W", "", 1f, false, false, false),
            (0x45, "E", "", 1f, false, false, false),
            (0x52, "R", "", 1f, false, false, false),
            (0x54, "T", "", 1f, false, false, false),
            (0x59, "Y", "", 1f, false, false, false),
            (0x55, "U", "", 1f, false, false, false),
            (0x49, "I", "", 1f, false, false, false),
            (0x4F, "O", "", 1f, false, false, false),
            (0x50, "P", "", 1f, false, false, false),
            (0xDB, "´", "", 1f, false, false, false),
            (0xDD, "[", "", 1f, false, false, false),
            (0x0D, "Enter", "", 1.5f, true, false, false)
        );

        // Row 2 (13.75u + 1.25u bottom part of ISO Enter = 15u)
        AddRow(2,
            (0x14, "Caps", "", 1.75f, false, false, false),
            (0x41, "A", "", 1f, false, false, false),
            (0x53, "S", "", 1f, false, false, false),
            (0x44, "D", "", 1f, false, false, false),
            (0x46, "F", "", 1f, false, false, false),
            (0x47, "G", "", 1f, false, false, false),
            (0x48, "H", "", 1f, false, false, false),
            (0x4A, "J", "", 1f, false, false, false),
            (0x4B, "K", "", 1f, false, false, false),
            (0x4C, "L", "", 1f, false, false, false),
            (0xBA, "Ç", "", 1f, false, false, false),
            (0xDE, "~", "", 1f, false, false, false),
            (0xDC, "]", "", 1f, false, false, false)
        );

        // Row 3 (15u - ABNT2 63-key Redragon Fizz)
        AddRow(3,
            (0xA0, "Shift", "", 1.25f, false, false, false),
            (0xE2, @"\", "", 1f, false, false, false),
            (0x5A, "Z", "", 1f, false, false, false),
            (0x58, "X", "", 1f, false, false, false),
            (0x43, "C", "", 1f, false, false, false),
            (0x56, "V", "", 1f, false, false, false),
            (0x42, "B", "", 1f, false, false, false),
            (0x4E, "N", "", 1f, false, false, false),
            (0x4D, "M", "", 1f, false, false, false),
            (0xBC, ",", "", 1f, false, false, false),
            (0xBE, ".", "", 1f, false, false, false),
            (0xBF, ";", "", 1f, false, false, false),
            (0xC1, "/", "", 1f, false, false, false),
            (0xA1, "Shift", "", 1.75f, false, false, false)
        );

        // Row 4 (15u)
        AddRow(4,
            (0xA2, "Ctrl", "", 1.25f, false, false, false),
            (0x5B, "Win", "", 1.25f, false, false, false),
            (0xA4, "Alt", "", 1.25f, false, false, false),
            (0x20, "", "", 6.25f, false, false, false),
            (0xA5, "AltGr", "", 1.25f, false, false, false),
            (0x00, "FN1", "", 1.25f, false, true, false),
            (0x5D, "FN2", "", 1.25f, false, false, true),
            (0xA3, "Ctrl", "", 1.25f, false, false, false)
        );

        return list;
    }
}
