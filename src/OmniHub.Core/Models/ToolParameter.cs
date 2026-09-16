using OmniHub.Core.Enums;

namespace OmniHub.Core.Models;

public class ToolParameter
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ParameterType Type { get; set; } = ParameterType.Text;
    public string DefaultValue { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string Filter { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
}

