using JohnnyCastaway.ScreenSaver;

internal static class Program
{
    // Scale 4 = up-res'd assets. ADS director selects and sequences the vignette clips.
    private const int Scale = 4;

    [STAThread]
    private static int Main(string[] args)
    {
        var mode = ScreenSaverArgs.Parse(args);
        ApplicationConfiguration.Initialize();

        string repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));

        switch (mode.Kind)
        {
            case ScreenSaverModeKind.Configure:
                MessageBox.Show("Johnny Castaway settings arrive in a later build.",
                    "Screen Antics", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;

            case ScreenSaverModeKind.Preview:
            {
                var provider = VignetteSource.CreateScheduledProvider(repo, Scale, startOfDayHHMM: 900, seed: 7);
                var form = new SaverForm(provider, exitOnInput: false);
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
                var provider = VignetteSource.CreateScheduledProvider(repo, Scale, startOfDayHHMM: 900, seed: 7);
                var form = new SaverForm(provider, exitOnInput: true)
                {
                    Bounds = Screen.PrimaryScreen!.Bounds,
                    TopMost = true,
                };
                form.Shown += (_, _) => { form.Activate(); form.BringToFront(); };
                Application.Run(form);
                return 0;
            }
        }
    }
}
