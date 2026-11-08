using System.Diagnostics;
using System.Runtime.InteropServices;
using RedragonFizzMacro.Core.Enums;
using RedragonFizzMacro.Core.Models;
using RedragonFizzMacro.Infrastructure.Audio;
using RedragonFizzMacro.Infrastructure.Native;

namespace RedragonFizzMacro.Core.Services;

public static class MacroExecutor
{
    public static readonly UIntPtr InjectedSignature = new(0x52465A4D); // "RFZM"

    private const ushort VK_VOLUME_MUTE = 0xAD;
    private const ushort VK_VOLUME_DOWN = 0xAE;
    private const ushort VK_VOLUME_UP = 0xAF;
    private const ushort VK_MEDIA_NEXT_TRACK = 0xB0;
    private const ushort VK_MEDIA_PREV_TRACK = 0xB1;
    private const ushort VK_MEDIA_STOP = 0xB2;
    private const ushort VK_MEDIA_PLAY_PAUSE = 0xB3;

    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_SHIFT = 0x10;
    private const ushort VK_MENU = 0x12;
    private const ushort VK_LWIN = 0x5B;
    private const ushort VK_RETURN = 0x0D;

    public static string? Execute(MacroBinding binding)
    {
        try
        {
            return binding.ActionType switch
            {
                MacroActionType.MediaVolume => ExecuteMedia(binding),
                MacroActionType.KeyCombo => ExecuteKeyCombo(binding),
                MacroActionType.LaunchAppOrUrl => ExecuteLaunch(binding.TargetPath, binding.Arguments),
                MacroActionType.TypeText => ExecuteTypeText(binding.TextSnippet, binding.PressEnterAfterText),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    public static void TapSingleVirtualKey(ushort vk, bool extended = false)
    {
        uint extFlag = extended ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0u;
        var inputs = new[]
        {
            CreateKeyInput(vk, 0, extFlag),
            CreateKeyInput(vk, 0, extFlag | NativeMethods.KEYEVENTF_KEYUP)
        };
        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static string? ExecuteMedia(MacroBinding binding)
    {
        if (binding.MediaCmd == MediaCommand.MicMute)
        {
            bool? muted = WindowsAudioService.ToggleDefaultMicrophoneMute();
            return muted switch
            {
                true => "Mic Mutado",
                false => "Mic Ativo",
                null => "Mic (Sem disp.)"
            };
        }

        if (binding.MediaCmd == MediaCommand.DiscordMute)
        {
            if (binding.ComboVkCode == 0 && !binding.ModCtrl && !binding.ModShift && !binding.ModAlt && !binding.ModWin)
            {
                var defaultDiscord = new MacroBinding
                {
                    ModCtrl = true,
                    ModShift = true,
                    ComboVkCode = (int)Keys.M
                };
                ExecuteKeyCombo(defaultDiscord);
            }
            else
            {
                ExecuteKeyCombo(binding);
            }
            return "Mute Discord";
        }

        ushort vk = binding.MediaCmd switch
        {
            MediaCommand.PlayPause => VK_MEDIA_PLAY_PAUSE,
            MediaCommand.NextTrack => VK_MEDIA_NEXT_TRACK,
            MediaCommand.PrevTrack => VK_MEDIA_PREV_TRACK,
            MediaCommand.StopMedia => VK_MEDIA_STOP,
            MediaCommand.VolumeUp => VK_VOLUME_UP,
            MediaCommand.VolumeDown => VK_VOLUME_DOWN,
            MediaCommand.VolumeMute => VK_VOLUME_MUTE,
            _ => VK_MEDIA_PLAY_PAUSE
        };

        int steps = Math.Clamp(binding.VolumeSteps, 1, 25);
        int repeat = (binding.MediaCmd is MediaCommand.VolumeUp or MediaCommand.VolumeDown) ? steps : 1;
        for (int i = 0; i < repeat; i++)
        {
            TapSingleVirtualKey(vk, extended: true);
            if (repeat > 1 && i + 1 < repeat)
                Thread.Sleep(8);
        }
        return null;
    }

    private static string? ExecuteKeyCombo(MacroBinding b)
    {
        var downList = new List<NativeMethods.INPUT>();
        var upList = new List<NativeMethods.INPUT>();

        void AddMod(ushort vk, bool ext = false)
        {
            uint f = ext ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0u;
            downList.Add(CreateKeyInput(vk, 0, f));
            upList.Insert(0, CreateKeyInput(vk, 0, f | NativeMethods.KEYEVENTF_KEYUP));
        }

        if (b.ModWin) AddMod(VK_LWIN, ext: true);
        if (b.ModCtrl) AddMod(VK_CONTROL);
        if (b.ModAlt) AddMod(VK_MENU);
        if (b.ModShift) AddMod(VK_SHIFT);

        if (b.ComboVkCode != 0)
        {
            ushort vk = (ushort)b.ComboVkCode;
            bool ext = IsExtendedKey(vk);
            uint f = ext ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0u;
            downList.Add(CreateKeyInput(vk, 0, f));
            upList.Insert(0, CreateKeyInput(vk, 0, f | NativeMethods.KEYEVENTF_KEYUP));
        }

        var all = downList.Concat(upList).ToArray();
        if (all.Length > 0)
            NativeMethods.SendInput((uint)all.Length, all, Marshal.SizeOf<NativeMethods.INPUT>());

        return null;
    }

    private static string? ExecuteLaunch(string target, string args)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;

        var psi = new ProcessStartInfo
        {
            FileName = target.Trim(),
            Arguments = args?.Trim() ?? string.Empty,
            UseShellExecute = true
        };
        Process.Start(psi);
        return null;
    }

    private static string? ExecuteTypeText(string text, bool pressEnter)
    {
        if (!string.IsNullOrEmpty(text))
        {
            var inputs = new List<NativeMethods.INPUT>(text.Length * 2);
            foreach (char ch in text)
            {
                if (ch == '\r') continue;
                if (ch == '\n')
                {
                    inputs.Add(CreateKeyInput(VK_RETURN, 0, 0));
                    inputs.Add(CreateKeyInput(VK_RETURN, 0, NativeMethods.KEYEVENTF_KEYUP));
                    continue;
                }

                inputs.Add(CreateKeyInput(0, ch, NativeMethods.KEYEVENTF_UNICODE));
                inputs.Add(CreateKeyInput(0, ch, NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP));
            }

            if (inputs.Count > 0)
            {
                var arr = inputs.ToArray();
                NativeMethods.SendInput((uint)arr.Length, arr, Marshal.SizeOf<NativeMethods.INPUT>());
            }
        }

        if (pressEnter)
        {
            Thread.Sleep(10);
            TapSingleVirtualKey(VK_RETURN);
        }
        return null;
    }

    private static bool IsExtendedKey(ushort vk) =>
        vk is 0x21 or 0x22 or 0x23 or 0x24 or 0x25 or 0x26 or 0x27 or 0x28 or 0x2D or 0x2E or 0x5B or 0x5C or 0x5D;

    private static NativeMethods.INPUT CreateKeyInput(ushort vk, ushort scan, uint flags) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = vk,
                wScan = scan,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = InjectedSignature
            }
        }
    };
}
