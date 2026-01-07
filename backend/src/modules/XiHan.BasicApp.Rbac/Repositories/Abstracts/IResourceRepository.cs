#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IResourceRepository
// Guid:e5f6a7b8-c9d0-4e5f-1a2b-4c5d6e7f8a9b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 资源仓储接口
/// </summary>
public interface IResourceRepository : IAggregateRootRepository<SysResource, long>
{
    /// <summary>
    /// 根据资源编码查询资源
    /// </summary>
    /// <param name="resourceCode">资源编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源实体</returns>
    Task<SysResource?> GetByResourceCodeAsync(string resourceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查资源编码是否存在
    /// </summary>
    /// <param name="resourceCode">资源编码</param>
    /// <param name="excludeResourceId">排除的资源ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByResourceCodeAsync(string resourceCode, long? excludeResourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据资源类型获取资源列表
    /// </summary>
    /// <param name="resourceType">资源类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<List<SysResource>> GetByResourceTypeAsync(ResourceType resourceType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据API路径查询资源
    /// </summary>
    /// <param name="apiPath">API路径</param>
    /// <param name="httpMethod">HTTP方法</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源实体</returns>
    Task<SysResource?> GetByApiPathAsync(string apiPath, string? httpMethod = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户可访问的资源
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<List<SysResource>> GetByUserIdAsync(long userId, ResourceType? resourceType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色可访问的资源
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="resourceType">资源类型（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<List<SysResource>> GetByRoleIdAsync(long roleId, ResourceType? resourceType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取资源
    /// </summary>
    /// <param name="resourceIds">资源ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<List<SysResource>> GetByIdsAsync(List<long> resourceIds, CancellationToken cancellationToken = default);
}
