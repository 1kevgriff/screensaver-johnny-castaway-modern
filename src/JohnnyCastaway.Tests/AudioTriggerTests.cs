using JohnnyCastaway.ScreenSaver;
using Xunit;

namespace JohnnyCastaway.Tests;

public class AudioTriggerTests
{
    sealed class SpyAudio : IAudioPlayer
    {
        public readonly List<int> Played = new();
        public void Play(int id) => Played.Add(id);
    }

    [Fact]
    public void NullAudioPlayerIsSafeNoop()
    {
        var a = new NullAudioPlayer();
        a.Play(5);   // must not throw
    }

    [Fact]
    public void SpyRecordsPlays()
    {
        var a = new SpyAudio();
        a.Play(3); a.Play(9);
        Assert.Equal(new[] { 3, 9 }, a.Played);
    }
}
