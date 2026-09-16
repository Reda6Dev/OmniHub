using System.Text.RegularExpressions;
using OmniHub.Core.Interfaces;

namespace OmniHub.Engine.Templating;

public partial class TokenInterpolator : ITokenInterpolator
{
    // Regex pattern for ${VarName} or ${env:VarName}
    [GeneratedRegex(@"\$\{([a-zA-Z0-9_:\.\-]+)\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    public string Interpolate(
        string template,
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlyDictionary<string, string>? globalVariables = null)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        return TokenRegex().Replace(template, match =>
        {
            var tokenKey = match.Groups[1].Value;

            // 1. Direct match in parameters
            if (parameters.TryGetValue(tokenKey, out var paramVal))
                return paramVal;

            // 2. Case-insensitive parameter match
            var caseInsensitiveParam = parameters.FirstOrDefault(p =>
                string.Equals(p.Key, tokenKey, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(caseInsensitiveParam.Key))
                return caseInsensitiveParam.Value;

            // 3. Global variables (e.g. configured paths)
            if (globalVariables != null)
            {
                if (globalVariables.TryGetValue(tokenKey, out var globalVal))
                    return globalVal;

                var caseInsensitiveGlobal = globalVariables.FirstOrDefault(g =>
                    string.Equals(g.Key, tokenKey, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(caseInsensitiveGlobal.Key))
                    return caseInsensitiveGlobal.Value;
            }

            // 4. Built-in dynamic system tokens
            var resolvedBuiltIn = ResolveBuiltInToken(tokenKey);
            if (resolvedBuiltIn != null)
                return resolvedBuiltIn;

            // 5. Environment variable lookup (${env:NAME} or ${env.NAME})
            if (tokenKey.StartsWith("env:", StringComparison.OrdinalIgnoreCase) ||
                tokenKey.StartsWith("env.", StringComparison.OrdinalIgnoreCase))
            {
                var envName = tokenKey[4..];
                var envVal = Environment.GetEnvironmentVariable(envName);
                if (!string.IsNullOrEmpty(envVal))
                    return envVal;
            }

            // Fallback: return match as is
            return match.Value;
        });
    }

    private static string? ResolveBuiltInToken(string tokenKey)
    {
        return tokenKey.ToLowerInvariant() switch
        {
            "timestamp" => DateTime.Now.ToString("yyyyMMdd_HHmmss"),
            "date" => DateTime.Now.ToString("yyyy-MM-dd"),
            "time" => DateTime.Now.ToString("HH:mm:ss"),
            "userprofile" => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "appdata" => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "localappdata" => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "temp" => Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            _ => null
        };
    }
}

