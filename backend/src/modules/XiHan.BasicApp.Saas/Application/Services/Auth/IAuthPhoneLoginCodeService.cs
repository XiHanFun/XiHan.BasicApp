// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 手机验证码登录的验证码下发与校验服务
/// </summary>
/// <remarks>
/// 基于框架 IOneTimeCodeService（分布式缓存后端）：接入 Redis 后支持多实例水平扩展与重启不丢码。
/// </remarks>
public interface IAuthPhoneLoginCodeService
{
    /// <summary>
    /// 验证码有效期（秒）
    /// </summary>
    int ExpiresInSeconds { get; }

    /// <summary>
    /// 为指定租户 + 手机号码生成并暂存一条登录验证码
    /// </summary>
    /// <param name="tenantId">租户标识（平台态为空）</param>
    /// <param name="phone">手机号码（E.164）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的验证码</returns>
    Task<string> IssueCodeAsync(long? tenantId, string phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验并消费指定租户 + 手机号码的登录验证码（一次性，消费即销毁）
    /// </summary>
    /// <param name="tenantId">租户标识（平台态为空）</param>
    /// <param name="phone">手机号码（E.164）</param>
    /// <param name="code">待校验验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>校验是否通过</returns>
    Task<bool> TryConsumeAsync(long? tenantId, string phone, string? code, CancellationToken cancellationToken = default);
}
