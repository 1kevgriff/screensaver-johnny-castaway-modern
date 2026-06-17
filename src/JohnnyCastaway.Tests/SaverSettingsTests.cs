using JohnnyCastaway.ScreenSaver;
using Xunit;

namespace JohnnyCastaway.Tests;

public class SaverSettingsTests
{
    [Fact]
    public void DefaultsAreSensible()
    {
        var d = SaverSettings.Defaults;
        Assert.Equal(900, d.StartOfDayHHMM);
        Assert.False(d.SoundEnabled);
        Assert.Equal(4, d.Scale);
    }

    [Fact]
    public void InMemoryStoreRoundTrips()
    {
        var store = new InMemorySettingsStore();
        Assert.Equal(SaverSettings.Defaults, store.Load());     // initial = defaults
        var s = new SaverSettings(730, true, 1);
        store.Save(s);
        Assert.Equal(s, store.Load());
    }
}
