using JohnnyCastaway.Content;
using JohnnyCastaway.ScreenSaver;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var mode = ScreenSaverArgs.Parse(args);
        ApplicationConfiguration.Initialize();

        // Dev fallback: walk 5 parents to find the repo root
        string? devRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));

        string? contentDir = ContentLocator.FindContentDir(AppContext.BaseDirectory, devRoot);

        var settings = new RegistrySettingsStore().Load();

        switch (mode.Kind)
        {
            case ScreenSaverModeKind.Configure:
            {
                using var form = new SettingsForm(new RegistrySettingsStore());
                form.ShowDialog();
                return 0;
            }

            case ScreenSaverModeKind.Preview:
            {
                if (contentDir is null)
                {
                    System.Diagnostics.Debug.WriteLine("JohnnyCastaway: content bundle not found; preview skipped.");
                    return 0;
                }
                var provider = VignetteSource.CreateScheduledProvider(contentDir, scale: 4, startOfDayHHMM: settings.StartOfDayHHMM, seed: 7);
                var form = new SaverForm(provider, exitOnInput: false, audio: new NullAudioPlayer());
                form.FormBorderStyle = FormBorderStyle.None;
                Native.SetParent(form.Handle, (nint)mode.PreviewHandle);
                if (Native.GetClientRect((nint)mode.PreviewHandle, out var r))
                    form.Bounds = new Rectangle(0, 0, r.Right - r.Left, r.Bottom - r.Top);
                form.TopLevel = false; form.Visible = true;
                Application.Run(new ApplicationContext(form));
                return 0;
            }

            default: // Run
            {
                if (contentDir is null)
                {
                    MessageBox.Show("Johnny Castaway: content bundle not found. Please reinstall.",
                        "Johnny Castaway", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return 0;
                }

                var (_, _, audioDir) = ContentLocator.Roots(contentDir);
                IAudioPlayer audio = settings.SoundEnabled
                    ? new NAudioPlayer(audioDir)
                    : new NullAudioPlayer();

                var forms = new List<Form>();
                var i = 0;
                foreach (var screen in Screen.AllScreens)
                {
                    var provider = VignetteSource.CreateScheduledProvider(contentDir, scale: 4, startOfDayHHMM: settings.StartOfDayHHMM, seed: 7);
                    var formAudio = i == 0 ? audio : new NullAudioPlayer();
                    var form = new SaverForm(provider, exitOnInput: true, audio: formAudio)
                    {
                        Bounds = screen.Bounds,
                        TopMost = true,
                    };
                    forms.Add(form);
                    i++;
                }

                if (forms.Count == 0) return 1;

                // Activate the primary form
                var primary = forms[0];
                primary.Shown += (_, _) => { primary.Activate(); primary.BringToFront(); };

                Application.Run(new MultiFormContext(forms));
                if (audio is IDisposable d) d.Dispose();
                return 0;
            }
        }
    }
}

internal sealed class MultiFormContext : ApplicationContext
{
    private readonly List<Form> _forms;
    private bool _closing;

    public MultiFormContext(List<Form> forms)
    {
        _forms = forms;
        foreach (var f in forms) { f.FormClosed += (_, _) => CloseAll(); f.Show(); }
    }

    private void CloseAll()
    {
        if (_closing) return; _closing = true;
        foreach (var f in _forms.ToArray()) { try { f.Close(); } catch { } }
        ExitThread();
    }
}
