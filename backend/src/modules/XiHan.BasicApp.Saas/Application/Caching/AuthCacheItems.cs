#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthCacheItems
// Guid:83330734-a0ac-492e-9716-d0f5c804117b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/07 10:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// 刷新令牌缓存项
/// </summary>
[CacheName("AuthRefreshToken")]
[IgnoreMultiTenancy]
public class AuthRefreshTokenCacheItem
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset ExpireAt { get; set; }
}

/// <summary>
/// 会话令牌映射缓存项
/// </summary>
[CacheName("AuthSessionTokenMap")]
[IgnoreMultiTenancy]
public class AuthSessionTokenMapCacheItem
{
    /// <summary>
    /// 刷新令牌缓存键
    /// </summary>
    public string RefreshTokenCacheKey { get; set; } = string.Empty;
}

/// <summary>
/// 登录验证码缓存项
/// </summary>
[CacheName("AuthVerificationCode")]
[IgnoreMultiTenancy]
public class AuthVerificationCodeCacheItem
{
    /// <summary>
    /// 验证码用途
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// 目标标识（手机号/邮箱）
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 验证码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset ExpireAt { get; set; }
}
