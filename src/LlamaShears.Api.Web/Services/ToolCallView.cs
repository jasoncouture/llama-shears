namespace LlamaShears.Api.Web.Services;

public sealed record ToolCallView(string Source, string Name, string ArgumentsJson, string CallId);
