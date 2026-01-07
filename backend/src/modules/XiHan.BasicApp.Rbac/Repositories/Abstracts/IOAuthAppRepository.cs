#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthAppRepository
// Guid:e1f2a3b4-c5d6-4e5f-7a8b-0c1d2e3f4a5b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// OAuth应用仓储接口
/// </summary>
public interface IOAuthAppRepository : IAggregateRootRepository<SysOAuthApp, long>
{
    /// <summary>
    /// 根据客户端ID查询应用
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth应用实体</returns>
    Task<SysOAuthApp?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查客户端ID是否存在
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="excludeAppId">排除的应用ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByClientIdAsync(string clientId, long? excludeAppId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证客户端凭证
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="clientSecret">客户端密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth应用实体</returns>
    Task<SysOAuthApp?> ValidateClientCredentialsAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户创建的应用列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth应用列表</returns>
    Task<List<SysOAuthApp>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
