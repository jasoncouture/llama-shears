using LlamaShears.Core.Paths;

namespace LlamaShears.UnitTests.Paths;

public sealed class PathExpanderTests
{
    [Test]
    public async Task ReturnsAbsolutePathUnchanged()
    {
        var expander = new PathExpander();
        var absolute = OperatingSystem.IsWindows() ? @"C:\foo\bar" : "/foo/bar";

        var result = expander.ExpandPath(absolute, "/working/dir");

        await Assert.That(result).IsEqualTo(absolute);
    }

    [Test]
    public async Task ExpandsTildeToUserProfile()
    {
        var expander = new PathExpander();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var result = expander.ExpandPath("~/projects/x", "/working/dir");

        await Assert.That(result).IsEqualTo(Path.Combine(home, "projects/x"));
    }

    [Test]
    public async Task JoinsRelativePathWithWorkingDirectory()
    {
        var expander = new PathExpander();
        var working = Path.GetTempPath();

        var result = expander.ExpandPath("nested/file.txt", working);

        await Assert.That(result).IsEqualTo(Path.GetFullPath(Path.Combine(working, "nested/file.txt")));
    }

    [Test]
    public async Task NormalizesParentSegmentsViaGetFullPath()
    {
        var expander = new PathExpander();
        var working = Path.GetTempPath();

        var result = expander.ExpandPath("a/../b/file.txt", working);

        await Assert.That(result).IsEqualTo(Path.GetFullPath(Path.Combine(working, "b/file.txt")));
    }

    [Test]
    public async Task BareTildeReturnsUserProfile()
    {
        var expander = new PathExpander();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var result = expander.ExpandPath("~", "/working/dir");

        await Assert.That(result).IsEqualTo(home);
    }

    [Test]
    public async Task TildeSlashReturnsUserProfile()
    {
        var expander = new PathExpander();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var result = expander.ExpandPath("~/", "/working/dir");

        await Assert.That(result).IsEqualTo(home);
    }

    [Test]
    public async Task DoesNotExpandPlainTildeWithoutSlash()
    {
        var expander = new PathExpander();
        var working = Path.GetTempPath();

        var result = expander.ExpandPath("~user/x", working);

        await Assert.That(result).IsEqualTo(Path.GetFullPath(Path.Combine(working, "~user/x")));
    }
}
