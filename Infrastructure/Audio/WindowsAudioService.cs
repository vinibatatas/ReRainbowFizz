using System.Runtime.InteropServices;

namespace RedragonFizzMacro.Infrastructure.Audio;

public static class WindowsAudioService
{
    private static readonly Guid ClsidMmDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IidAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");

    public static bool? ToggleDefaultMicrophoneMute()
    {
        try
        {
            Type? enumeratorType = Type.GetTypeFromCLSID(ClsidMmDeviceEnumerator);
            if (enumeratorType == null) return null;

            var enumerator = (IMMDeviceEnumerator?)Activator.CreateInstance(enumeratorType);
            if (enumerator == null) return null;

            Guid emptyCtx = Guid.Empty;
            bool? targetMuteState = null;

            // 1 = eCapture (input audio devices). 0 = eConsole, 2 = eCommunications
            foreach (int role in new[] { 0, 2 })
            {
                if (enumerator.GetDefaultAudioEndpoint(1, role, out IMMDevice device) == 0 && device != null)
                {
                    Guid iid = IidAudioEndpointVolume;
                    if (device.Activate(ref iid, 23, IntPtr.Zero, out object volObj) == 0 &&
                        volObj is IAudioEndpointVolume endpointVol)
                    {
                        if (targetMuteState == null)
                        {
                            endpointVol.GetMute(out bool currentMute);
                            targetMuteState = !currentMute;
                        }
                        endpointVol.SetMute(targetMuteState.Value, ref emptyCtx);
                    }
                }
            }

            return targetMuteState;
        }
        catch
        {
            return null;
        }
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr ppDevices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr pNotify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr pNotify);
        [PreserveSig] int GetChannelCount(out int pnChannelCount);
        [PreserveSig] int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        [PreserveSig] int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        [PreserveSig] int GetMasterVolumeLevel(out float pfLevelDB);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float pfLevel);
        [PreserveSig] int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
        [PreserveSig] int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }
}
