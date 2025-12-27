#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOAuthTokenRepository
// Guid:afb2c3d4-e5f6-7890-abcd-ef123456789e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstractions;

/// <summary>
/// 系统OAuth令牌仓储接口
/// </summary>
public interface ISysOAuthTokenRepository : IRepositoryBase<SysOAuthToken, XiHanBasicAppIdType>
{
    /// <summary>
    /// 根据访问令牌获取
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <returns></returns>
    Task<SysOAuthToken?> GetByAccessTokenAsync(string accessToken);

    /// <summary>
    /// 根据刷新令牌获取
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns></returns>
    Task<SysOAuthToken?> GetByRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 根据客户端ID和用户ID获取令牌列表
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysOAuthToken>> GetByClientAndUserAsync(string clientId, XiHanBasicAppIdType userId);

    /// <summary>
    /// 删除过期的令牌
    /// </summary>
    /// <returns></returns>
    Task<int> DeleteExpiredTokensAsync();
}
