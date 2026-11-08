using RedragonFizzMacro.Core.Enums;

namespace RedragonFizzMacro.Core.Models;

public sealed class MacroBinding
{
    public int TriggerVkCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public MacroActionType ActionType { get; set; } = MacroActionType.MediaVolume;

    // Media & Volume Properties
    public MediaCommand MediaCmd { get; set; } = MediaCommand.PlayPause;
    public int VolumeSteps { get; set; } = 1;

    // Key Combo & Discord Shortcut Properties
    public bool ModCtrl { get; set; }
    public bool ModShift { get; set; }
    public bool ModAlt { get; set; }
    public bool ModWin { get; set; }
    public int ComboVkCode { get; set; }

    // Launch App / URL / Script
    public string TargetPath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;

    // Type Text / Snippet
    public string TextSnippet { get; set; } = string.Empty;
    public bool PressEnterAfterText { get; set; }

    public string GetShortBadge()
    {
        if (!string.IsNullOrWhiteSpace(Name))
            return Truncate(Name, 7);

        return ActionType switch
        {
            MacroActionType.MediaVolume => MediaCmd switch
            {
                MediaCommand.PlayPause => "Play",
                MediaCommand.NextTrack => "Próx",
                MediaCommand.PrevTrack => "Ant",
                MediaCommand.StopMedia => "Stop",
                MediaCommand.VolumeUp => $"+{VolumeSteps * 2}%",
                MediaCommand.VolumeDown => $"−{VolumeSteps * 2}%",
                MediaCommand.VolumeMute => "Mute",
                MediaCommand.MicMute => "Mic Win",
                MediaCommand.DiscordMute => "Discord",
                _ => "Mídia"
            },
            MacroActionType.KeyCombo => "Atalho",
            MacroActionType.LaunchAppOrUrl => "App",
            MacroActionType.TypeText => "Texto",
            _ => "Macro"
        };
    }

    public string GetDisplayDescription()
    {
        if (!string.IsNullOrWhiteSpace(Name))
            return Name;

        return ActionType switch
        {
            MacroActionType.MediaVolume => MediaCmd switch
            {
                MediaCommand.PlayPause => "Play / Pause",
                MediaCommand.NextTrack => "Próxima faixa",
                MediaCommand.PrevTrack => "Faixa anterior",
                MediaCommand.StopMedia => "Parar mídia",
                MediaCommand.VolumeUp => $"Volume +{VolumeSteps * 2}%",
                MediaCommand.VolumeDown => $"Volume −{VolumeSteps * 2}%",
                MediaCommand.VolumeMute => "Mutar áudio do Windows",
                MediaCommand.MicMute => "Mutar microfone (Windows)",
                MediaCommand.DiscordMute => $"Mutar Discord ({FormatDiscordCombo()})",
                _ => "Mídia"
            },
            MacroActionType.KeyCombo => FormatCombo(),
            MacroActionType.LaunchAppOrUrl => $"Abrir {Path.GetFileName(TargetPath)}",
            MacroActionType.TypeText => "Digitar texto automático",
            _ => "Macro"
        };
    }

    public string FormatDiscordCombo()
    {
        if (ComboVkCode == 0 && !ModCtrl && !ModShift && !ModAlt && !ModWin)
            return "Ctrl + Shift + M";
        return FormatCombo();
    }

    public string FormatCombo()
    {
        var parts = new List<string>();
        if (ModCtrl) parts.Add("Ctrl");
        if (ModShift) parts.Add("Shift");
        if (ModAlt) parts.Add("Alt");
        if (ModWin) parts.Add("Win");
        if (ComboVkCode != 0)
            parts.Add(((Keys)ComboVkCode).ToString());
        return parts.Count > 0 ? string.Join(" + ", parts) : "Atalho";
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max];

    public MacroBinding Clone() => new()
    {
        TriggerVkCode = TriggerVkCode,
        Name = Name,
        ActionType = ActionType,
        MediaCmd = MediaCmd,
        VolumeSteps = VolumeSteps,
        ModCtrl = ModCtrl,
        ModShift = ModShift,
        ModAlt = ModAlt,
        ModWin = ModWin,
        ComboVkCode = ComboVkCode,
        TargetPath = TargetPath,
        Arguments = Arguments,
        TextSnippet = TextSnippet,
        PressEnterAfterText = PressEnterAfterText
    };
}
