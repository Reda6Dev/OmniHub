namespace OmniHub.Core.Interfaces;

public interface ITokenInterpolator
{
    string Interpolate(string template, IReadOnlyDictionary<string, string> parameters, IReadOnlyDictionary<string, string>? globalVariables = null);
}

