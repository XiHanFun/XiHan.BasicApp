#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthEmailLoginCodeService
// Guid:6f2a1d4c-8b3e-4a7c-9d2f-1e5b7c9a3d10
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 邮箱验证码登录的验证码下发与校验服务
/// </summary>
/// <remarks>
/// 基于框架 IOneTimeCodeService（分布式缓存后端）：接入 Redis 后支持多实例水平扩展与重启不丢码。
/// </remarks>
public interface IAuthEmailLoginCodeService
{
    /// <summary>
    /// 验证码有效期（秒）
    /// </summary>
    int ExpiresInSeconds { get; }

    /// <summary>
    /// 为指定租户 + 邮箱生成并暂存一条登录验证码
    /// </summary>
    /// <param name="tenantId">租户标识（平台态为空）</param>
    /// <param name="email">邮箱地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的验证码</returns>
    Task<string> IssueCodeAsync(long? tenantId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验并消费指定租户 + 邮箱的登录验证码（一次性，消费即销毁）
    /// </summary>
    /// <param name="tenantId">租户标识（平台态为空）</param>
    /// <param name="email">邮箱地址</param>
    /// <param name="code">待校验验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>校验是否通过</returns>
    Task<bool> TryConsumeAsync(long? tenantId, string email, string? code, CancellationToken cancellationToken = default);
}
