namespace JohnnyCastaway.ScreenSaver;

public readonly record struct SaverSettings(int StartOfDayHHMM, bool SoundEnabled, int Scale)
{
    public static SaverSettings Defaults => new(900, false, 4);
}

public interface ISettingsStore
{
    SaverSettings Load();
    void Save(SaverSettings settings);
}

public sealed class InMemorySettingsStore : ISettingsStore
{
    private SaverSettings _s = SaverSettings.Defaults;
    public SaverSettings Load() => _s;
    public void Save(SaverSettings settings) => _s = settings;
}
