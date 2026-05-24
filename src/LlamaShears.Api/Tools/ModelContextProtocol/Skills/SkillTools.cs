using System.ComponentModel;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.PromptContext;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Skills;

[McpServerToolType]
public sealed class SkillTools
{
    private const string SkillFileName = "SKILL.md";

    private readonly IDataContextScope _dataContextScope;
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ISkillParser _parser;

    public SkillTools(IDataContextScope dataContextScope, IAgentWorkspaceLocator workspace, ISkillParser parser)
    {
        _dataContextScope = dataContextScope;
        _workspace = workspace;
        _parser = parser;
    }

    [McpServerTool(Name = "skill_get")]
    [Description("Returns the full record for a single skill by name: declared name, description, absolute path to the SKILL.md file, the markdown body with frontmatter stripped, plus any extra frontmatter fields and the optional metadata block. The catalog of available skills (name + description only) is already injected into the per-turn prompt context — call this only when the agent has chosen a skill and needs its body / resource directory. Returns an error string when skills are disabled or the name does not resolve.")]
    public GetSkillResponse GetSkill(
        [Description("Declared name of the skill to load (case-insensitive). Must match an entry in the per-turn skill catalog.")]
        string skillName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_dataContextScope.TryGetValue<AgentSkills>(AgentSkills.DataKey, out var skills))
            return "No skills are available for the current agent context.";
        if (string.IsNullOrWhiteSpace(skillName))
            return "Refused: skillName is required.";
        var skill = skills.Skills
            .FirstOrDefault(i => string.Equals(i.Name, skillName, StringComparison.OrdinalIgnoreCase));
        if (skill is null) return $"No skill named '{skillName}' was found.";
        return skill;
    }

    [McpServerTool(Name = "skill_test")]
    [Description("Loads a SKILL.md file from disk and runs it through the skill parser without registering it. Use after writing a new skill (see the create-skill skill) to confirm the frontmatter validates: name 1-64 chars / lowercase alphanumeric + hyphens / matches parent directory, description 1-1024 chars, metadata if present is an object. The file must be named exactly 'SKILL.md' — any other filename is rejected up front. On success returns the parsed SkillRecord (name, description, absolute path, body, extra frontmatter, metadata). On failure returns an error message describing what is wrong. Relative paths resolve against the agent's workspace; absolute paths are honored as-is.")]
    public async Task<SkillTestResponse> TestSkill(
        [Description("Path to a SKILL.md file. Relative paths resolve against the agent's workspace; absolute paths are used as-is. The basename must be exactly 'SKILL.md'.")]
        string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Refused: path is required.";

        var workspace = await _workspace.GetAsync(cancellationToken);
        var resolved = Path.GetFullPath(Path.IsPathRooted(path)
            ? path
            : Path.Combine(workspace.Root, path));

        var fileName = Path.GetFileName(resolved);
        if (!string.Equals(fileName, SkillFileName, StringComparison.Ordinal))
            return $"Refused: file must be named '{SkillFileName}' (got '{fileName}').";

        if (Directory.Exists(resolved))
            return $"Refused: '{path}' is a directory, not a file.";
        if (!File.Exists(resolved))
            return $"File not found: {path}";

        string text;
        try
        {
            text = await File.ReadAllTextAsync(resolved, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"Read failed: {ex.Message}";
        }

        try
        {
            var record = _parser.Parse(text, resolved);
            if (record is null) return "Parse failed: no YAML frontmatter block found in the document.";
            return record;
        }
        catch (InvalidOperationException ex)
        {
            return $"Parse failed: {ex.Message}";
        }
    }
}
