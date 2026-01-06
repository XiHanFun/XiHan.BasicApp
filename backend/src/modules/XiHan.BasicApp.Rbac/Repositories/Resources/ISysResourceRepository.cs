#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysResourceRepository
// Guid:1a2b3c4d-5e6f-7890-abcd-ef1234567800
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 15:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Resources;

/// <summary>
/// 系统资源仓储接口
/// </summary>
public interface ISysResourceRepository : IRepositoryBase<SysResource, long>
{
    /// <summary>
    /// 根据资源编码获取资源
    /// </summary>
    /// <param name="resourceCode">资源编码</param>
    /// <returns></returns>
    Task<SysResource?> GetByResourceCodeAsync(string resourceCode);

    /// <summary>
    /// 检查资源编码是否存在
    /// </summary>
    /// <param name="resourceCode">资源编码</param>
    /// <param name="excludeId">排除的资源ID</param>
    /// <returns></returns>
    Task<bool> ExistsByResourceCodeAsync(string resourceCode, long? excludeId = null);

    /// <summary>
    /// 根据资源类型获取资源列表
    /// </summary>
    /// <param name="resourceType">资源类型</param>
    /// <returns></returns>
    Task<List<SysResource>> GetByTypeAsync(ResourceType resourceType);

    /// <summary>
    /// 获取子资源列表
    /// </summary>
    /// <param name="parentId">父资源ID</param>
    /// <returns></returns>
    Task<List<SysResource>> GetChildrenAsync(long parentId);

    /// <summary>
    /// 获取资源树（包含子资源）
    /// </summary>
    /// <param name="parentId">父资源ID，null表示获取根资源</param>
    /// <returns>资源树</returns>
    Task<List<SysResource>> GetResourceTreeAsync(long? parentId = null);

    /// <summary>
    /// 获取所有父资源ID（递归查询）
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <returns>父资源ID列表</returns>
    Task<List<long>> GetParentResourceIdsAsync(long resourceId);

    /// <summary>
    /// 获取所有子资源ID（递归查询）
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <returns>子资源ID列表</returns>
    Task<List<long>> GetChildResourceIdsAsync(long resourceId);

    /// <summary>
    /// 检查是否会形成循环依赖
    /// </summary>
    /// <param name="resourceId">当前资源ID</param>
    /// <param name="parentId">要设置的父资源ID</param>
    /// <returns>是否会形成循环</returns>
    Task<bool> WouldCreateCycleAsync(long resourceId, long parentId);

    /// <summary>
    /// 根据资源路径获取资源
    /// </summary>
    /// <param name="resourcePath">资源路径</param>
    /// <returns></returns>
    Task<SysResource?> GetByResourcePathAsync(string resourcePath);

    /// <summary>
    /// 获取公共资源列表（不需要认证）
    /// </summary>
    /// <returns></returns>
    Task<List<SysResource>> GetPublicResourcesAsync();

    /// <summary>
    /// 获取需要认证的资源列表
    /// </summary>
    /// <returns></returns>
    Task<List<SysResource>> GetAuthRequiredResourcesAsync();
}
