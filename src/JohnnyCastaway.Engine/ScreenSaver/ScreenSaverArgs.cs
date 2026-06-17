using System.Globalization;

namespace JohnnyCastaway.ScreenSaver;

public enum ScreenSaverModeKind { Run, Preview, Configure }

public readonly record struct ScreenSaverMode(ScreenSaverModeKind Kind, long PreviewHandle);

public static class ScreenSaverArgs
{
    public static ScreenSaverMode Parse(string[] args)
    {
        if (args.Length == 0)
            return new ScreenSaverMode(ScreenSaverModeKind.Configure, 0);

        string a0 = args[0].Trim();
        // split "/p:1234" → flag "/p", inline "1234"
        string flag = a0, inline = "";
        int colon = a0.IndexOf(':');
        if (colon >= 0) { flag = a0[..colon]; inline = a0[(colon + 1)..]; }
        string opt = flag.TrimStart('/', '-').ToLowerInvariant();

        long handle = 0;
        string handleStr = inline.Length > 0 ? inline : (args.Length > 1 ? args[1] : "");
        long.TryParse(handleStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out handle);

        return opt switch
        {
            "s" => new ScreenSaverMode(ScreenSaverModeKind.Run, 0),
            "p" => new ScreenSaverMode(ScreenSaverModeKind.Preview, handle),
            _   => new ScreenSaverMode(ScreenSaverModeKind.Configure, 0), // c, a, anything else
        };
    }
}
