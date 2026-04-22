#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthTokenCacheHelper
// Guid:c1d2e3f4-5a6b-7c8d-9e0f-1a2b3c4d5e6f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// 认证令牌缓存辅助服务
/// 封装 RefreshToken / SessionTokenMap 的缓存读写，供 AuthAppService 和 ProfileAppService 共用
/// </summary>
public interface IAuthTokenCacheHelper
{
    /// <summary>
    /// 保存刷新令牌到缓存
    /// </summary>
    Task SaveRefreshTokenAsync(string refreshToken, SysUser user, long? effectiveTenantId, string sessionId);

    /// <summary>
    /// 移除会话关联的刷新令牌缓存（通过 SessionTokenMap 间接移除）
    /// </summary>
    Task RemoveSessionTokenAsync(string sessionId);

    /// <summary>
    /// 根据刷新令牌获取缓存项
    /// </summary>
    Task<AuthRefreshTokenCacheItem?> GetRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 直接移除指定刷新令牌缓存
    /// </summary>
    Task RemoveRefreshTokenDirectAsync(string refreshToken);

    /// <summary>
    /// 直接移除会话映射缓存
    /// </summary>
    Task RemoveSessionTokenMapDirectAsync(string sessionId);
}
