#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOAuthAppRepository
// Guid:f6a7b8c9-d0e1-2345-6789-0abcdef12345
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统 OAuth 应用仓储接口
/// </summary>
public interface ISysOAuthAppRepository : IAggregateRootRepository<SysOAuthApp, long>
{
    /// <summary>
    /// 根据客户端ID获取应用
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应用实体</returns>
    Task<SysOAuthApp?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证客户端凭证
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="clientSecret">客户端密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应用实体（验证失败返回 null）</returns>
    Task<SysOAuthApp?> ValidateClientCredentialsAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查客户端ID是否存在
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="excludeAppId">排除的应用ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsClientIdExistsAsync(string clientId, long? excludeAppId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有活跃的应用列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应用列表</returns>
    Task<List<SysOAuthApp>> GetActiveAppsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存应用
    /// </summary>
    /// <param name="oauthApp">应用实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的应用实体</returns>
    Task<SysOAuthApp> SaveAsync(SysOAuthApp oauthApp, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用应用
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnableAppAsync(long appId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用应用
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisableAppAsync(long appId, CancellationToken cancellationToken = default);
}
