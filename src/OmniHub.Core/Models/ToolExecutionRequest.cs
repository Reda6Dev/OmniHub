namespace OmniHub.Core.Models;

public class ToolExecutionRequest
{
    public ToolDefinition Tool { get; init; } = new();
    public Dictionary<string, string> Parameters { get; init; } = new();
}

