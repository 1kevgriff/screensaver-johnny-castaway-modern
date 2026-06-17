using JohnnyCastaway.ScreenSaver;
using Xunit;

namespace JohnnyCastaway.Tests;

public class ScreenSaverArgsTests
{
    [Theory]
    [InlineData(new string[0], ScreenSaverModeKind.Configure)]
    [InlineData(new[] { "/c" }, ScreenSaverModeKind.Configure)]
    [InlineData(new[] { "/C" }, ScreenSaverModeKind.Configure)]
    [InlineData(new[] { "-s" }, ScreenSaverModeKind.Run)]
    [InlineData(new[] { "/s" }, ScreenSaverModeKind.Run)]
    public void ParsesKind(string[] args, ScreenSaverModeKind expected)
        => Assert.Equal(expected, ScreenSaverArgs.Parse(args).Kind);

    [Theory]
    [InlineData(new[] { "/p", "12345" }, 12345L)]
    [InlineData(new[] { "/p:6789" }, 6789L)]
    [InlineData(new[] { "/c:222" }, 0L)]   // configure ignores handle
    public void ParsesPreviewHandle(string[] args, long expectedHandle)
    {
        var m = ScreenSaverArgs.Parse(args);
        if (args[0].StartsWith("/p", System.StringComparison.OrdinalIgnoreCase))
        {
            Assert.Equal(ScreenSaverModeKind.Preview, m.Kind);
            Assert.Equal(expectedHandle, m.PreviewHandle);
        }
        else
            Assert.Equal(ScreenSaverModeKind.Configure, m.Kind);
    }
}
