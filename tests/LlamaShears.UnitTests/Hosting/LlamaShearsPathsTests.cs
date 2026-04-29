using LlamaShears.Hosting.Abstractions;

namespace LlamaShears.UnitTests.Hosting;

public sealed class LlamaShearsPathsTests
{
    [Test]
    public async Task DataRoot_is_dotted_directory_under_the_user_profile()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var dataRoot = LlamaShearsPaths.DataRoot;

        await Assert.That(Path.GetDirectoryName(dataRoot)).IsEqualTo(userProfile);
        await Assert.That(Path.GetFileName(dataRoot)).IsEqualTo(".llama-shears");
    }

    [Test]
    public async Task ConfigFile_lives_directly_under_DataRoot()
    {
        var configFile = LlamaShearsPaths.ConfigFile;

        await Assert.That(Path.GetDirectoryName(configFile)).IsEqualTo(LlamaShearsPaths.DataRoot);
        await Assert.That(Path.GetFileName(configFile)).IsEqualTo("config.json");
    }
}
