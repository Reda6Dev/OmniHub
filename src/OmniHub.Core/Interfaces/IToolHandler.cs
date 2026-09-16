using OmniHub.Core.Models;

namespace OmniHub.Core.Interfaces;

public interface IToolHandler
{
    string HandlerId { get; }
    Task<ProcessExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        IProgress<LogEntry> progress,
        CancellationToken cancellationToken);
}

