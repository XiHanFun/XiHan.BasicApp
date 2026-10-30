// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 邮件 / 短信验证码的签发、限频与校验。
/// </summary>
/// <remarks>
/// 单例服务，状态都在共享缓存里（多实例共用），4.x 的规则：6 位数字、有效期 <see cref="IdentityOptions.VerificationCodeLifetime"/>，
/// 同一用途同一主体再发即覆盖旧码；同一用途同一目标两次发码至少隔 <see cref="IdentityOptions.VerificationCodeInterval"/>，
/// 一个目标（邮箱 / 手机号）与一个客户端地址每天（UTC）各有上限、不分用途；输错 <see cref="IdentityOptions.VerificationCodeMaxFailures"/> 次即作废，
/// 这一用途这一主体锁定 <see cref="IdentityOptions.VerificationCodeLockout"/>。缓存里只存码的 SHA-256，校验成功即删（先删再算成功，并发的两次只有一次过）。
/// 与 4.x 不同：码不存明文；输错有次数而不是错一次即作废。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="options">身份配置。</param>
/// <param name="codes">码源（与图形验证码共用，测试换成固定码）。</param>
/// <param name="clock">时间源。</param>
public sealed class VerificationCodes(ISharedCache cache, IdentityOptions options, ICaptchaCodeSource codes, TimeProvider clock)
{
    /// <summary>
    /// 码的长度。
    /// </summary>
    public const int CodeLength = 6;

    /// <summary>
    /// 签发一个验证码。
    /// </summary>
    /// <remarks>
    /// 先看锁定、间隔与每日上限，都过了才签发并计数。
    /// </remarks>
    /// <param name="purpose">用途（如 <c>contact-verify:email</c>）。</param>
    /// <param name="subject">码归属的主体（如用户 ID、登录的账号）。</param>
    /// <param name="target">收码的邮箱或手机号（限频与每日上限按它算）。</param>
    /// <param name="payload">随码保存、校验成功时取回的内容（如要换成的新邮箱）。</param>
    /// <param name="address">客户端地址；没有即不计地址的每日上限。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>验证码与有效秒数；被锁定或超出频率时失败。</returns>
    public async Task<Result<(string Code, int ExpiresInSeconds)>> IssueAsync(string purpose, string subject, string target, string payload, string? address, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        ArgumentNullException.ThrowIfNull(payload);
        if ((await cache.GetAsync<string>(LockKey(purpose, subject), cancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Locked();
        }

        var intervalKey = new CacheKey($"identity:code-interval:{purpose}:{HashOf(Normalize(target))}");
        if ((await cache.GetAsync<string>(intervalKey, cancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Unavailable(IdentityErrors.CodeTooFrequent, IdentityMessages.CodeTooFrequent, MessageArguments.Of(("seconds", (int)options.VerificationCodeInterval.TotalSeconds)));
        }

        var now = clock.GetUtcNow();
        var day = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var untilTomorrow = now.Date.AddDays(1) - now.UtcDateTime;
        var targetKey = new CacheKey($"identity:code-daily:{day}:target:{HashOf(Normalize(target))}");
        if (await CountAsync(targetKey, cancellationToken).ConfigureAwait(false) >= options.VerificationCodeDailyPerTarget)
        {
            return Error.Unavailable(IdentityErrors.CodeTargetDailyLimit, IdentityMessages.CodeTargetDailyLimit);
        }

        var addressKey = string.IsNullOrWhiteSpace(address) ? (CacheKey?)null : new CacheKey($"identity:code-daily:{day}:address:{HashOf(address.Trim())}");
        if (addressKey is { } byAddress && await CountAsync(byAddress, cancellationToken).ConfigureAwait(false) >= options.VerificationCodeDailyPerAddress)
        {
            return Error.Unavailable(IdentityErrors.CodeAddressDailyLimit, IdentityMessages.CodeAddressDailyLimit);
        }

        var code = codes.Generate(CodeLength);
        await cache.SetAsync(CodeKey(purpose, subject), Encode(new CodeState(HashOf(code), 0, now + options.VerificationCodeLifetime, payload)), CacheEntryOptions.Expires(options.VerificationCodeLifetime), cancellationToken).ConfigureAwait(false);
        await cache.SetAsync(intervalKey, "1", CacheEntryOptions.Expires(options.VerificationCodeInterval), cancellationToken).ConfigureAwait(false);
        await IncrementAsync(targetKey, untilTomorrow, cancellationToken).ConfigureAwait(false);
        if (addressKey is { } counted)
        {
            await IncrementAsync(counted, untilTomorrow, cancellationToken).ConfigureAwait(false);
        }

        return (code, (int)options.VerificationCodeLifetime.TotalSeconds);
    }

    /// <summary>
    /// 校验并作废验证码。
    /// </summary>
    /// <remarks>
    /// 码对即删并返回签发时保存的内容；码错计一次，用完次数即作废并锁定。没有码、已过期或已用过都是 <c>IDENTITY_VALIDATION_018</c>。
    /// </remarks>
    /// <param name="purpose">用途。</param>
    /// <param name="subject">主体。</param>
    /// <param name="code">输入的码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>签发时保存的内容。</returns>
    public async Task<Result<string>> ConsumeAsync(string purpose, string subject, string? code, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        if ((await cache.GetAsync<string>(LockKey(purpose, subject), cancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Locked();
        }

        var key = CodeKey(purpose, subject);
        var now = clock.GetUtcNow();
        if (string.IsNullOrWhiteSpace(code) || !(await cache.GetAsync<string>(key, cancellationToken).ConfigureAwait(false)).TryGetValue(out var raw) || Decode(raw) is not { } state || state.ExpiresAt <= now)
        {
            return Invalid();
        }

        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(state.Hash), Encoding.ASCII.GetBytes(HashOf(code.Trim()))))
        {
            var failures = state.Failures + 1;
            if (failures >= options.VerificationCodeMaxFailures)
            {
                await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                await cache.SetAsync(LockKey(purpose, subject), "1", CacheEntryOptions.Expires(options.VerificationCodeLockout), cancellationToken).ConfigureAwait(false);
                return Locked();
            }

            // 只记次数，不顺延有效期
            await cache.SetAsync(key, Encode(state with { Failures = failures }), CacheEntryOptions.Expires(state.ExpiresAt - now), cancellationToken).ConfigureAwait(false);
            return Invalid();
        }

        // 先删：并发的两次提交只有删成功的那一次算数
        return await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false) ? state.Payload : Invalid();
    }

    private static Error Invalid() => Error.Validation(IdentityErrors.CodeInvalid, IdentityMessages.CodeInvalid);

    private Error Locked() => Error.Unavailable(IdentityErrors.CodeLocked, IdentityMessages.CodeLocked, MessageArguments.Of(("minutes", (int)options.VerificationCodeLockout.TotalMinutes)));

    private async Task<int> CountAsync(CacheKey key, CancellationToken cancellationToken)
        => (await cache.GetAsync<string>(key, cancellationToken).ConfigureAwait(false)).TryGetValue(out var raw) && int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var count) ? count : 0;

    private async Task IncrementAsync(CacheKey key, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var next = await CountAsync(key, cancellationToken).ConfigureAwait(false) + 1;
        await cache.SetAsync(key, next.ToString(CultureInfo.InvariantCulture), CacheEntryOptions.Expires(lifetime), cancellationToken).ConfigureAwait(false);
    }

    private static CacheKey CodeKey(string purpose, string subject) => new($"identity:code:{purpose}:{HashOf(subject)}");

    private static CacheKey LockKey(string purpose, string subject) => new($"identity:code-lock:{purpose}:{HashOf(subject)}");

    private static string Normalize(string target) => target.Trim().ToLowerInvariant();

    private static string HashOf(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Encode(CodeState state)
        => string.Join('|', state.Hash, state.Failures.ToString(CultureInfo.InvariantCulture), state.ExpiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), Convert.ToBase64String(Encoding.UTF8.GetBytes(state.Payload)));

    private static CodeState? Decode(string raw)
    {
        var parts = raw.Split('|');
        return parts.Length == 4 && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var failures) && long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var expires)
            ? new CodeState(parts[0], failures, DateTimeOffset.FromUnixTimeSeconds(expires), Encoding.UTF8.GetString(Convert.FromBase64String(parts[3])))
            : null;
    }

    private sealed record CodeState(string Hash, int Failures, DateTimeOffset ExpiresAt, string Payload);
}
