using Microsoft.Win32;

namespace JohnnyCastaway.ScreenSaver;

public sealed class RegistrySettingsStore : ISettingsStore
{
    private const string Key = @"Software\JohnnyCastaway";

    public SaverSettings Load()
    {
        using var k = Registry.CurrentUser.OpenSubKey(Key);
        var d = SaverSettings.Defaults;
        if (k is null) return d;
        int start = ClampStart(k.GetValue("StartOfDay") is int sv ? sv : d.StartOfDayHHMM);
        bool sound = (k.GetValue("SoundEnabled") is int snd ? snd : (d.SoundEnabled ? 1 : 0)) != 0;
        int scale = (k.GetValue("Scale") is int sc ? sc : d.Scale) == 1 ? 1 : 4;
        return new SaverSettings(start, sound, scale);
    }

    public void Save(SaverSettings s)
    {
        using var k = Registry.CurrentUser.CreateSubKey(Key);
        k.SetValue("StartOfDay", ClampStart(s.StartOfDayHHMM), RegistryValueKind.DWord);
        k.SetValue("SoundEnabled", s.SoundEnabled ? 1 : 0, RegistryValueKind.DWord);
        k.SetValue("Scale", s.Scale == 1 ? 1 : 4, RegistryValueKind.DWord);
    }

    private static int ClampStart(int hhmm)
    {
        if (hhmm < 0) return 0;
        int h = Math.Clamp(hhmm / 100, 0, 23);
        int m = (hhmm % 100) >= 30 ? 30 : 0;
        return h * 100 + m;
    }
}
