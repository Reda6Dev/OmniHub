namespace OmniHub.Core.Interfaces;

public interface IToolRegistry
{
    IToolHandler? GetHandler(string handlerId);
    void RegisterHandler(IToolHandler handler);
}

