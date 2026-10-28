// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 图形验证码（单例）：4 位数字，共享缓存里只存码的 SHA-256，有效期 <see cref="IdentityOptions.CaptchaLifetime"/>。
/// 一次性：校验先删再比，删除失败（已被别的请求拿走或已过期）即不通过，无论对错都不能再用；比较用常数时间。
/// </summary>
/// <param name="cache">共享缓存（多实例共用）。</param>
/// <param name="options">身份配置。</param>
/// <param name="codes">码源。</param>
public sealed class Captchas(ISharedCache cache, IdentityOptions options, ICaptchaCodeSource codes)
{
    /// <summary>
    /// 码的长度。
    /// </summary>
    public const int CodeLength = 4;

    /// <summary>
    /// 发一个新验证码：返回标识、图片与有效秒数。
    /// </summary>
    public async Task<(string Id, string Image, int ExpiresInSeconds)> IssueAsync(CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        var code = codes.Generate(CodeLength);
        await cache.SetAsync(KeyOf(id), HashOf(code), CacheEntryOptions.Expires(options.CaptchaLifetime), cancellationToken).ConfigureAwait(false);
        return (id, CaptchaImage.ToDataUri(code), (int)options.CaptchaLifetime.TotalSeconds);
    }

    /// <summary>
    /// 校验并作废：标识或码为空、标识不是本服务发的、已过期或已用过，都不通过。
    /// </summary>
    public async Task<bool> ConsumeAsync(string? id, string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(code) || !Guid.TryParseExact(id, "N", out _))
        {
            return false;
        }

        var key = KeyOf(id);
        if (!(await cache.GetAsync<string>(key, cancellationToken).ConfigureAwait(false)).TryGetValue(out var expected))
        {
            return false;
        }

        // 先删：并发的两次提交只有删成功的那一次能继续比较
        if (!await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(HashOf(code.Trim())));
    }

    private static CacheKey KeyOf(string id) => new($"identity:captcha:{id}");

    private static string HashOf(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
