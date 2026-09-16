using OmniHub.Core.Enums;

namespace OmniHub.Core.Models;

public record LogEntry(
    string Message,
    LogLevel Level = LogLevel.Standard)
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

