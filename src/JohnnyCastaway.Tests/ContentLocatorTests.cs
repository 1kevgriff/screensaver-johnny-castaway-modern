using JohnnyCastaway.Content;
using Xunit;

namespace JohnnyCastaway.Tests;

public class ContentLocatorTests
{
    [Fact]
    public void PrefersExeAdjacentContentWhenScriptsPresent()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "jc_" + Guid.NewGuid().ToString("N"));
        string exeContent = Path.Combine(tmp, "content");
        Directory.CreateDirectory(exeContent);
        File.WriteAllText(Path.Combine(exeContent, "scripts.json"), "{}");
        try
        {
            Assert.Equal(exeContent, ContentLocator.FindContentDir(tmp, devRepoRoot: null));
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void FallsBackToDevRepoContent()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "jc_" + Guid.NewGuid().ToString("N"));
        string exeDir = Path.Combine(tmp, "bin");
        string dev = Path.Combine(tmp, "repo");
        string devContent = Path.Combine(dev, "content");
        Directory.CreateDirectory(exeDir);
        Directory.CreateDirectory(devContent);
        File.WriteAllText(Path.Combine(devContent, "scripts.json"), "{}");
        try
        {
            Assert.Equal(devContent, ContentLocator.FindContentDir(exeDir, dev));
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void ReturnsNullWhenNoBundle()
    {
        string tmp = Path.Combine(Path.GetTempPath(), "jc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try { Assert.Null(ContentLocator.FindContentDir(tmp, null)); }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void RootsAreUnderContentDir()
    {
        var (s, b, a) = ContentLocator.Roots("/x/content");
        Assert.Equal(Path.Combine("/x/content", "sprites"), s);
        Assert.Equal(Path.Combine("/x/content", "backgrounds"), b);
        Assert.Equal(Path.Combine("/x/content", "audio"), a);
    }
}
