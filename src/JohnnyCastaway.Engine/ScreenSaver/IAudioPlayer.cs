namespace JohnnyCastaway.ScreenSaver;

public interface IAudioPlayer { void Play(int sampleId); }
public sealed class NullAudioPlayer : IAudioPlayer { public void Play(int sampleId) { } }
