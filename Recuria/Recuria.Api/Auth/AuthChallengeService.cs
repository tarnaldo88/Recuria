using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Recuria.Api.Auth;

public interface IAuthChallengeService
{
    string IssueEmailVerification(Guid userId, TimeSpan ttl);
    bool TryConsumeEmailVerification(string token, out Guid userId);

    string IssuePasswordReset(Guid userId, TimeSpan ttl);
    bool TryConsumePasswordReset(string token, out Guid userId);

    string IssueMagicLink(Guid userId, TimeSpan ttl);
    bool TryConsumeMagicLink(string token, out Guid userId);
}

public sealed class AuthChallengeService : IAuthChallengeService
{
    private static readonly ConcurrentDictionary<string, (string Prefix, Guid UserId)> TokenIndex = new();
    private readonly IMemoryCache _cache;

    public AuthChallengeService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string IssueEmailVerification(Guid userId, TimeSpan ttl) => Issue("verify", userId, ttl);
    public bool TryConsumeEmailVerification(string token, out Guid userId) => TryConsume("verify", token, out userId);

    public string IssuePasswordReset(Guid userId, TimeSpan ttl) => Issue("reset", userId, ttl);
    public bool TryConsumePasswordReset(string token, out Guid userId) => TryConsume("reset", token, out userId);

    public string IssueMagicLink(Guid userId, TimeSpan ttl) => Issue("magic", userId, ttl);
    public bool TryConsumeMagicLink(string token, out Guid userId) => TryConsume("magic", token, out userId);

    private string Issue(string prefix, Guid userId, TimeSpan ttl)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace("=", string.Empty, StringComparison.Ordinal);

        var key = $"{prefix}:{Hash(token)}";
        _cache.Set(key, userId, ttl);
        TokenIndex[key] = (prefix, userId);
        return token;
    }

    private bool TryConsume(string prefix, string token, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var key = $"{prefix}:{Hash(token.Trim())}";
        if (!_cache.TryGetValue<Guid>(key, out var found))
            return false;

        _cache.Remove(key);
        TokenIndex.TryRemove(key, out _);
        userId = found;
        return true;
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
