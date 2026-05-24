using System.Runtime.CompilerServices;
using System.Text;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.PromptContext;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.PromptContext;

public sealed class SkillRepository : ISkillRepository
{
    private readonly IFileParserCache<SkillRepository> _fileCache;
    private readonly ISkillFilter _filter;
    private readonly ISkillParser _parser;
    private readonly IApplicationPathProvider _pathProvider;
    private readonly ILogger<SkillRepository> _logger;
    private const string SkillFileName = "SKILL.md";

    public SkillRepository(ISkillFilter skillFilter, ISkillParser parser, IApplicationPathProvider pathProvider,
        IFileParserCache<SkillRepository> fileCache, ILogger<SkillRepository> logger)
    {
        _pathProvider = pathProvider;
        _logger = logger;
        _filter = skillFilter;
        _parser = parser;
        _fileCache = fileCache;
    }

    public async ValueTask<AgentSkills> GetSkillsAsync(string agentId, CancellationToken cancellationToken)
    {
        if (!_filter.AreSkillsAllowed()) return AgentSkills.None;
        var allSkills = await EnumerateSkills(agentId, cancellationToken)
            .DistinctBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .Where(i => _filter.IsSkillAllowed(i.Name))
            .ToListAsync(cancellationToken: cancellationToken);
        return new AgentSkills([..allSkills]);
    }

    private async IAsyncEnumerable<SkillRecord> EnumerateSkills(string agentId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var file in EnumerateSkillFiles(agentId))
        {
            var record = await TryReadSkillFile(file, cancellationToken);
            if (record is not null) yield return record;
        }
    }

    private async ValueTask<SkillRecord?> TryReadSkillFile(FileInfo file, CancellationToken cancellationToken)
    {
        return await _fileCache.GetOrParseAsync(file.FullName, file, ParseSkillAsync, cancellationToken);
    }

    private async ValueTask<SkillRecord?> ParseSkillAsync(Stream? stream, FileInfo fileInfo,
        CancellationToken cancellationToken)
    {
        if (stream is null) return null;
        using var stringReader = new StreamReader(stream, Encoding.UTF8);
        var text = await stringReader.ReadToEndAsync(cancellationToken);
        try
        {
            return _parser.Parse(text, fileInfo.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse skill {Path}, this skill will be ignored", fileInfo.FullName);
            return null;
        }
    }

    private IEnumerable<FileInfo> EnumerateSkillFiles(string agentId)
    {
        foreach (var directory in EnumerateSkillDirectories(agentId).SelectMany(i => i.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)))
        {
            var file = directory.EnumerateFiles(SkillFileName).SingleOrDefault();
            if (file is not null) yield return file;
        }
    }

    private IEnumerable<DirectoryInfo> EnumerateSkillDirectories(string agentId)
    {
        var targetPaths = new[]
        {
            _pathProvider.GetPath(PathKind.AgentSkills, agentId),
            _pathProvider.GetPath(PathKind.AppSkills),
            _pathProvider.GetPath(PathKind.GlobalSkills),
            
        };

        foreach (var path in targetPaths)
        {
            if (!Directory.Exists(path)) continue;
            yield return new DirectoryInfo(path);
        }
    }
}
