using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Paths;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;

internal static class TestFileProtectionPolicies
{
    public static IFileProtectionPolicy AllowAll => Substitute.For<IFileProtectionPolicy>();

    public static IFileProtectionPolicy WorkspaceDefaults
    {
        get
        {
            var options = new FileProtectionPolicyOptions();
            options.Rules.Add(new ProtectedFile(".git", ProtectionMode.Delete, FileType.Directory, "git metadata"));
            options.Rules.Add(new ProtectedFile(".git/**", ProtectionMode.Delete, FileType.Any, "git metadata"));
            options.Rules.Add(new ProtectedFile("**/.git", ProtectionMode.Delete, FileType.Directory, "nested git metadata"));
            options.Rules.Add(new ProtectedFile("**/.git/**", ProtectionMode.Delete, FileType.Any, "nested git metadata"));
            options.Rules.Add(new ProtectedFile("*.md", ProtectionMode.Delete, FileType.File, "agent root markdown"));
            options.Rules.Add(new ProtectedFile(".gitignore", ProtectionMode.Delete | ProtectionMode.Write, FileType.File, "workspace .gitignore"));
            return new FileProtectionPolicy(Options.Create(options));
        }
    }
}
