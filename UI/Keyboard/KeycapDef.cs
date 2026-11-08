namespace RedragonFizzMacro.UI.Keyboard;

public sealed record KeycapDef(
    int VkCode,
    string TopLegend,
    string SubLegend,
    int Row,
    float XUnits,
    float WidthUnits,
    bool IsIsoEnter = false,
    bool IsHardwareFn1 = false,
    bool IsFn2Key = false);
