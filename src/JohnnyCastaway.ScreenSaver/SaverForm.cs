using SkiaSharp;

namespace JohnnyCastaway.ScreenSaver;

public sealed class SaverForm : Form
{
    private readonly Func<ScenePlayer> _next;
    private ScenePlayer _player;
    private long _startTicks;
    private readonly bool _exitOnInput;
    private readonly System.Windows.Forms.Timer _timer;
    private Point? _lastMouse;
    private SKImage? _lastSkImage;
    private Bitmap? _current;

    // Convenience overload: wraps a single player in a constant provider.
    public SaverForm(ScenePlayer player, bool exitOnInput) : this(() => player, exitOnInput) { }

    public SaverForm(Func<ScenePlayer> nextVignette, bool exitOnInput)
    {
        _next = nextVignette;
        _player = _next();
        _startTicks = Environment.TickCount64;
        _exitOnInput = exitOnInput;

        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.Black;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        _timer = new System.Windows.Forms.Timer { Interval = 33 }; // ~30 fps
        _timer.Tick += OnTick;
        _timer.Start();

        if (_exitOnInput)
        {
            Cursor.Hide();
            MouseMove += OnMouseMove;
            MouseDown += (_, _) => Application.Exit();
            KeyDown += (_, _) => Application.Exit();
            KeyPreview = true;
        }
    }

    private void OnTick(object? sender, EventArgs e) => Invalidate();

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_lastMouse is { } p && (Math.Abs(p.X - e.X) > 8 || Math.Abs(p.Y - e.Y) > 8))
            Application.Exit();
        _lastMouse = e.Location;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        long elapsed = Environment.TickCount64 - _startTicks;
        if (elapsed >= _player.TotalMs)
        {
            var old = _player;
            // Clear the cached bitmap before disposing the old player's images.
            _lastSkImage = null;
            _current?.Dispose();
            _current = null;

            _player = _next();
            _startTicks = Environment.TickCount64;
            elapsed = 0;

            if (!ReferenceEquals(_player, old))
                old.DisposeFrames();
        }

        var skImg = _player.ImageAt(elapsed);
        var bmp = GetBitmap(skImg);

        var g = e.Graphics;
        g.Clear(Color.Black);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        int cw = ClientSize.Width, ch = ClientSize.Height;
        float scale = Math.Min((float)cw / bmp.Width, (float)ch / bmp.Height);
        int w = (int)(bmp.Width * scale), h = (int)(bmp.Height * scale);
        int x = (cw - w) / 2, y = (ch - h) / 2;
        g.DrawImage(bmp, x, y, w, h);
    }

    private Bitmap GetBitmap(SKImage skImg)
    {
        if (ReferenceEquals(skImg, _lastSkImage) && _current is not null) return _current;
        var old = _current;
        _lastSkImage = skImg;
        _current = ToBitmap(skImg);
        old?.Dispose();
        return _current;
    }

    private static Bitmap ToBitmap(SKImage skImg)
    {
        using var data = skImg.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = data.AsStream();
        return new Bitmap(ms);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _timer.Dispose();
        _player.DisposeFrames();
        _current?.Dispose();
        base.OnFormClosed(e);
    }
}
