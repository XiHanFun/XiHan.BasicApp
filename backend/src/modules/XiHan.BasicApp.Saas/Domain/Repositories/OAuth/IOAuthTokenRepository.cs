#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthTokenRepository
// Guid:06ab14c2-369d-4759-a704-4d287db7572b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// OAuth Token 仓储接口
/// </summary>
public interface IOAuthTokenRepository : ISaasRepository<SysOAuthToken>
{
    /// <summary>
    /// 根据访问令牌JTI获取
    /// </summary>
    Task<SysOAuthToken?> GetByAccessTokenAsync(string accessTokenJti, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据刷新令牌获取
    /// </summary>
    Task<SysOAuthToken?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 吊销用户所有令牌
    /// </summary>
    Task<int> RevokeByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
