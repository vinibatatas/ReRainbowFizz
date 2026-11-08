namespace RedragonFizzMacro.Core.Models;

public sealed class MacroProfile
{
    public string Name { get; set; } = "P1";
    public Dictionary<int, MacroBinding> Bindings { get; set; } = new();
}
