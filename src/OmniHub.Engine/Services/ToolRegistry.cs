using System.Collections.Concurrent;
using OmniHub.Core.Interfaces;

namespace OmniHub.Engine.Services;

public class ToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, IToolHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public IToolHandler? GetHandler(string handlerId)
    {
        _handlers.TryGetValue(handlerId, out var handler);
        return handler;
    }

    public void RegisterHandler(IToolHandler handler)
    {
        _handlers[handler.HandlerId] = handler;
    }
}

