using System.Runtime.InteropServices;
using RedragonFizzMacro.Core.Enums;
using RedragonFizzMacro.Core.Models;
using RedragonFizzMacro.Infrastructure.Native;

namespace RedragonFizzMacro.Core.Services;

public sealed class MacroHookEngine : IDisposable
{
    private const int VK_APPS = 0x5D;   // Physical FN2 scancode on Redragon Fizz Rainbow
    private const int VK_ESCAPE = 0x1B;

    public AppConfig Config { get; }

    public event Action<bool, string, int>? OnLayerStateChanged;
    public event Action<string, MacroBinding?>? OnMacroTriggered;
    public Func<int, bool>? KeyCaptureOverride { get; set; }

    private IntPtr _hookId = IntPtr.Zero;
    private NativeMethods.LowLevelKeyboardProc? _hookDelegate;
    private readonly HashSet<int> _suppressedKeysDown = new();
    private readonly System.Windows.Forms.Timer _expiryTimer;

    // FN2 One-Shot state
    private bool _fn2Armed;
    private long _fn2ArmedUntilTicks;

    // Hold Layer state (e.g. CapsLock Tap-Hold)
    private bool _holdKeyIsDown;
    private bool _holdKeyUsedForMacro;
    private long _holdKeyDownTicks;

    private bool _disposed;

    public MacroHookEngine(AppConfig config)
    {
        Config = config;
        _expiryTimer = new System.Windows.Forms.Timer { Interval = 60 };
        _expiryTimer.Tick += CheckFn2Timeout;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero) return;

        _hookDelegate = HookCallback;
        IntPtr hMod = NativeMethods.GetModuleHandle(null);
        _hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _hookDelegate, hMod, 0);
        _expiryTimer.Start();
    }

    private void CheckFn2Timeout(object? sender, EventArgs e)
    {
        if (_fn2Armed && Environment.TickCount64 > _fn2ArmedUntilTicks)
        {
            _fn2Armed = false;
            OnLayerStateChanged?.Invoke(false, string.Empty, 0);
        }
    }

    public void DisarmAll()
    {
        _fn2Armed = false;
        _holdKeyIsDown = false;
        _suppressedKeysDown.Clear();
        OnLayerStateChanged?.Invoke(false, string.Empty, 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

        var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

        // Ignore our own synthetic inputs
        bool isInjected = (kb.flags & 0x10) != 0 || kb.dwExtraInfo == MacroExecutor.InjectedSignature;
        if (isInjected)
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

        int msg = wParam.ToInt32();
        bool isKeyDown = msg is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN;
        bool isKeyUp = msg is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP;
        int rawVk = (int)kb.vkCode;

        // FN2 EXCLUSIVE MANAGEMENT: Block Windows/Google right-click menu entirely!
        if (rawVk == VK_APPS)
        {
            if (isKeyDown && Config.EngineEnabled && Config.Fn2OneShotEnabled && !_holdKeyIsDown)
            {
                _fn2Armed = true;
                _fn2ArmedUntilTicks = Environment.TickCount64 + Config.Fn2TimeoutMs;
                OnLayerStateChanged?.Invoke(true, "FN2", Config.Fn2TimeoutMs);
            }
            return (IntPtr)1;
        }

        // Shortcut recording override for UI
        if (KeyCaptureOverride != null)
        {
            if (isKeyDown && KeyCaptureOverride.Invoke(rawVk))
            {
                _suppressedKeysDown.Add(rawVk);
                return (IntPtr)1;
            }
            if (isKeyUp && _suppressedKeysDown.Remove(rawVk))
                return (IntPtr)1;
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // Swallow matching KeyUps for suppressed keys
        if (isKeyUp)
        {
            if (Config.EngineEnabled && Config.HoldLayerEnabled && rawVk == Config.HoldLayerVkCode && _holdKeyIsDown)
            {
                _holdKeyIsDown = false;
                OnLayerStateChanged?.Invoke(false, string.Empty, 0);

                long heldMs = Environment.TickCount64 - _holdKeyDownTicks;
                if (!_holdKeyUsedForMacro && heldMs < 350)
                {
                    ushort tapVk = (ushort)rawVk;
                    ThreadPool.QueueUserWorkItem(_ => MacroExecutor.TapSingleVirtualKey(tapVk));
                }
                return (IntPtr)1;
            }

            if (_suppressedKeysDown.Remove(rawVk))
                return (IntPtr)1;

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        if (!Config.EngineEnabled || !isKeyDown)
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

        // Inline check for FN2 expiration
        if (_fn2Armed && Environment.TickCount64 > _fn2ArmedUntilTicks)
        {
            _fn2Armed = false;
            OnLayerStateChanged?.Invoke(false, string.Empty, 0);
        }

        // 1. Hold Layer Trigger
        if (Config.HoldLayerEnabled && rawVk == Config.HoldLayerVkCode && !_fn2Armed)
        {
            if (!_holdKeyIsDown)
            {
                _holdKeyIsDown = true;
                _holdKeyUsedForMacro = false;
                _holdKeyDownTicks = Environment.TickCount64;
                string holdName = FormatKeyShort(rawVk);
                OnLayerStateChanged?.Invoke(true, holdName, 0);
            }
            return (IntPtr)1;
        }

        // 2. Active Layer (FN2 or Hold) Key Interception
        if (_holdKeyIsDown || _fn2Armed)
        {
            var profile = Config.GetActiveProfile();
            bool hasMacro = profile.Bindings.TryGetValue(rawVk, out MacroBinding? binding);

            if (_fn2Armed && rawVk == VK_ESCAPE && !hasMacro)
            {
                _fn2Armed = false;
                _suppressedKeysDown.Add(rawVk);
                OnLayerStateChanged?.Invoke(false, string.Empty, 0);
                OnMacroTriggered?.Invoke("FN2 cancelado", null);
                return (IntPtr)1;
            }

            _suppressedKeysDown.Add(rawVk);

            if (_holdKeyIsDown)
                _holdKeyUsedForMacro = true;

            string layerPrefix = _holdKeyIsDown ? FormatKeyShort(Config.HoldLayerVkCode) : "FN2";

            if (hasMacro && binding != null)
            {
                var toRun = binding;
                bool keepFn2 = _fn2Armed && Config.KeepFn2AliveOnMedia && toRun.ActionType == MacroActionType.MediaVolume;

                if (_fn2Armed && !keepFn2)
                {
                    _fn2Armed = false;
                    OnLayerStateChanged?.Invoke(false, string.Empty, 0);
                }

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    string? feedback = MacroExecutor.Execute(toRun);
                    string desc = feedback ?? toRun.GetDisplayDescription();
                    OnMacroTriggered?.Invoke($"{layerPrefix} + {FormatKeyShort(rawVk)} · {desc}", toRun);

                    if (keepFn2)
                    {
                        int renewMs = Math.Min(Config.Fn2TimeoutMs, 1800);
                        _fn2ArmedUntilTicks = Environment.TickCount64 + renewMs;
                        string badge = feedback ?? toRun.GetShortBadge();
                        OnLayerStateChanged?.Invoke(true, $"FN2 · {badge}", renewMs);
                    }
                });
            }
            else
            {
                OnMacroTriggered?.Invoke($"{layerPrefix} + {FormatKeyShort(rawVk)} · Sem macro", null);
                if (_fn2Armed)
                {
                    _fn2Armed = false;
                    OnLayerStateChanged?.Invoke(false, string.Empty, 0);
                }
            }

            return (IntPtr)1;
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public static string FormatKeyShort(int vk) => vk switch
    {
        0x14 => "Caps",
        0x5D => "FN2",
        0xA0 => "Shift Esq",
        0xA1 => "Shift Dir",
        0xA2 => "Ctrl Esq",
        0xA3 => "Ctrl Dir",
        0xA4 => "Alt Esq",
        0xA5 => "AltGr",
        0x5B => "Win",
        0x20 => "Espaço",
        0x08 => "Back",
        0x0D => "Enter",
        0x1B => "Esc",
        0x09 => "Tab",
        >= 0x30 and <= 0x39 => ((char)vk).ToString(),
        0xBD => "-",
        0xBB => "=",
        0xDB => "´",
        0xDD => "[",
        0xBA => "Ç",
        0xDE => "~",
        0xDC => "]",
        0xE2 => @"\",
        0xBC => ",",
        0xBE => ".",
        0xBF => ";",
        0xC1 => "/",
        _ => ((Keys)vk).ToString()
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _expiryTimer.Stop();
        _expiryTimer.Dispose();

        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}
