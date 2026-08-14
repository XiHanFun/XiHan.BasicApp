// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Authentication.OneTimeCode;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 邮箱验证码登录的验证码下发与校验服务实现
/// </summary>
/// <remarks>
/// 委托框架 <see cref="IOneTimeCodeService"/>（分布式缓存后端，加密安全 RNG，消费即销毁）：
/// 默认内存缓存即可用，宿主接入 Redis 后自动获得多实例水平扩展与重启不丢码能力。
/// </remarks>
public sealed class AuthEmailLoginCodeService : IAuthEmailLoginCodeService
{
    private const string Purpose = "auth:email-login";

    private readonly IOneTimeCodeService _oneTimeCodeService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthEmailLoginCodeService(IOneTimeCodeService oneTimeCodeService)
    {
        _oneTimeCodeService = oneTimeCodeService;
    }

    /// <summary>
    /// 验证码有效期（秒）
    /// </summary>
    public int ExpiresInSeconds => 600;

    /// <summary>
    /// 为指定租户 + 邮箱生成并暂存一条登录验证码
    /// </summary>
    /// <param name="tenantId">租户标识（平台态为空）</param>
    /// <param name="email">邮箱地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的验证码</returns>
    public async Task<string> IssueCodeAsync(long? tenantId, string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var result = await _oneTimeCodeService.IssueAsync(
            Purpose,
            BuildTarget(tenantId, email),
            payload: null,
            new OneTimeCodeOptions { CodeLength = 6, ExpiresInSeconds = ExpiresInSeconds },
            cancellationToken);
        return result.Code;
    }

    /// <summary>
    /// 校验并消费指定租户 + 邮箱的登录验证码（一次性，消费即销毁）
    /// </summary>
    /// <param name="tenantId">租户标识（平台态为空）</param>
    /// <param name="email">邮箱地址</param>
    /// <param name="code">待校验验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>校验是否通过</returns>
    public async Task<bool> TryConsumeAsync(long? tenantId, string email, string? code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var result = await _oneTimeCodeService.TryConsumeAsync(Purpose, BuildTarget(tenantId, email), code, cancellationToken);
        return result.Succeeded;
    }

    private static string BuildTarget(long? tenantId, string email)
    {
        return $"{tenantId?.ToString() ?? "platform"}:{email.Trim().ToLowerInvariant()}";
    }
}
