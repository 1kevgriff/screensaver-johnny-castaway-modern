using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace JohnnyCastaway.ScreenSaver;

public sealed class NAudioPlayer : IAudioPlayer, IDisposable
{
    private readonly string _dir;
    private readonly IWavePlayer _out;
    private readonly MixingSampleProvider _mixer;
    // null sentinel: id present but file missing
    private readonly Dictionary<int, CachedSound?> _cache = new();

    public NAudioPlayer(string wavDir)
    {
        _dir = wavDir;
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(11025, 1)) { ReadFully = true };
        _out = new WaveOutEvent();
        _out.Init(_mixer);
        _out.Play();
    }

    public void Play(int sampleId)
    {
        CachedSound? s = Get(sampleId);
        if (s is null) return;
        _mixer.AddMixerInput(new CachedSoundSampleProvider(s));
    }

    private CachedSound? Get(int id)
    {
        if (_cache.TryGetValue(id, out var c)) return c;
        string path = Path.Combine(_dir, $"sfx_{id:D2}.wav");
        if (!File.Exists(path)) { _cache[id] = null; return null; }
        var cs = new CachedSound(path, _mixer.WaveFormat);
        _cache[id] = cs;
        return cs;
    }

    public void Dispose() { _out.Dispose(); GC.SuppressFinalize(this); }

    private sealed class CachedSound
    {
        public float[] Data { get; }
        public WaveFormat WaveFormat { get; }
        public CachedSound(string path, WaveFormat target)
        {
            using var reader = new AudioFileReader(path);
            ISampleProvider sp = reader;
            if (reader.WaveFormat.SampleRate != target.SampleRate || reader.WaveFormat.Channels != target.Channels)
                sp = new WdlResamplingSampleProvider(
                    reader.WaveFormat.Channels == 1 ? sp : new StereoToMonoSampleProvider(reader), target.SampleRate);
            WaveFormat = target;
            var buf = new List<float>();
            var tmp = new float[target.SampleRate];
            int n;
            while ((n = sp.Read(tmp, 0, tmp.Length)) > 0) buf.AddRange(tmp[..n]);
            Data = buf.ToArray();
        }
    }

    private sealed class CachedSoundSampleProvider(CachedSound s) : ISampleProvider
    {
        private long _pos;
        public WaveFormat WaveFormat => s.WaveFormat;
        public int Read(float[] buffer, int offset, int count)
        {
            long avail = s.Data.Length - _pos;
            int n = (int)Math.Min(avail, count);
            Array.Copy(s.Data, _pos, buffer, offset, n);
            _pos += n;
            return n;
        }
    }
}
