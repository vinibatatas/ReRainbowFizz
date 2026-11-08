using RedragonFizzMacro.Core.Enums;

namespace RedragonFizzMacro.Core.Models;

public sealed class AppConfig
{
    public bool EngineEnabled { get; set; } = true;

    // FN2 One-Shot Layer
    public bool Fn2OneShotEnabled { get; set; } = true;
    public int Fn2TimeoutMs { get; set; } = 2500;
    public bool KeepFn2AliveOnMedia { get; set; } = true;
    public bool ShowHudOverlay { get; set; } = true;

    // Hold Layer (e.g. CapsLock Tap-Hold)
    public bool HoldLayerEnabled { get; set; } = true;
    public int HoldLayerVkCode { get; set; } = (int)Keys.Capital;

    // System Behavior
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public int ActiveProfileIndex { get; set; } = 0;
    public List<MacroProfile> Profiles { get; set; } = new();

    public MacroProfile GetActiveProfile()
    {
        if (Profiles.Count == 0)
            Profiles.Add(CreateDefaultProfile());

        if (ActiveProfileIndex < 0 || ActiveProfileIndex >= Profiles.Count)
            ActiveProfileIndex = 0;

        return Profiles[ActiveProfileIndex];
    }

    public void ResetToDefaults()
    {
        EngineEnabled = true;
        Fn2OneShotEnabled = true;
        Fn2TimeoutMs = 2500;
        KeepFn2AliveOnMedia = true;
        ShowHudOverlay = true;
        HoldLayerEnabled = true;
        HoldLayerVkCode = (int)Keys.Capital;
        MinimizeToTrayOnClose = true;
        ActiveProfileIndex = 0;
        Profiles.Clear();
        Profiles.Add(CreateDefaultProfile());
    }

    public static MacroProfile CreateDefaultProfile()
    {
        var profile = new MacroProfile { Name = "P1" };

        void Add(Keys k, MediaCommand cmd, int vol = 1)
        {
            profile.Bindings[(int)k] = new MacroBinding
            {
                TriggerVkCode = (int)k,
                ActionType = MacroActionType.MediaVolume,
                MediaCmd = cmd,
                VolumeSteps = vol
            };
        }

        Add(Keys.Q, MediaCommand.PrevTrack);
        Add(Keys.W, MediaCommand.PlayPause);
        Add(Keys.E, MediaCommand.NextTrack);
        Add(Keys.R, MediaCommand.VolumeMute);
        Add(Keys.A, MediaCommand.VolumeDown, 2);
        Add(Keys.S, MediaCommand.StopMedia);
        Add(Keys.D, MediaCommand.VolumeUp, 2);
        Add(Keys.F, MediaCommand.MicMute);

        // Discord Mute shortcut (Ctrl+Shift+M)
        profile.Bindings[(int)Keys.G] = new MacroBinding
        {
            TriggerVkCode = (int)Keys.G,
            ActionType = MacroActionType.MediaVolume,
            MediaCmd = MediaCommand.DiscordMute,
            ModCtrl = true,
            ModShift = true,
            ComboVkCode = (int)Keys.M
        };

        return profile;
    }
}
