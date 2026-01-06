#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysResourceRepository
// Guid:2a2b3c4d-5e6f-7890-abcd-ef1234567801
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 15:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Resources;

/// <summary>
/// 系统资源仓储实现
/// </summary>
public class SysResourceRepository : SqlSugarRepositoryBase<SysResource, long>, ISysResourceRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysResourceRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据资源编码获取资源
    /// </summary>
    public async Task<SysResource?> GetByResourceCodeAsync(string resourceCode)
    {
        return await GetFirstAsync(r => r.ResourceCode == resourceCode);
    }

    /// <summary>
    /// 检查资源编码是否存在
    /// </summary>
    public async Task<bool> ExistsByResourceCodeAsync(string resourceCode, long? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysResource>().Where(r => r.ResourceCode == resourceCode);
        if (excludeId.HasValue)
        {
            query = query.Where(r => r.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 根据资源类型获取资源列表
    /// </summary>
    public async Task<List<SysResource>> GetByTypeAsync(ResourceType resourceType)
    {
        return await GetListAsync(r => r.ResourceType == resourceType);
    }

    /// <summary>
    /// 获取子资源列表
    /// </summary>
    public async Task<List<SysResource>> GetChildrenAsync(long parentId)
    {
        return await GetListAsync(r => r.ParentId == parentId);
    }

    /// <summary>
    /// 获取资源树（包含子资源）
    /// </summary>
    public async Task<List<SysResource>> GetResourceTreeAsync(long? parentId = null)
    {
        return await GetListAsync(r => r.ParentId == parentId);
    }

    /// <summary>
    /// 获取所有父资源ID（递归查询）
    /// </summary>
    public async Task<List<long>> GetParentResourceIdsAsync(long resourceId)
    {
        var parentIds = new List<long>();
        await GetParentResourceIdsRecursiveAsync(resourceId, parentIds);
        return parentIds;
    }

    /// <summary>
    /// 获取所有子资源ID（递归查询）
    /// </summary>
    public async Task<List<long>> GetChildResourceIdsAsync(long resourceId)
    {
        var childIds = new List<long>();
        await GetChildResourceIdsRecursiveAsync(resourceId, childIds);
        return childIds;
    }

    /// <summary>
    /// 检查是否会形成循环依赖
    /// </summary>
    public async Task<bool> WouldCreateCycleAsync(long resourceId, long parentId)
    {
        if (resourceId == parentId)
        {
            return true;
        }

        var childIds = await GetChildResourceIdsAsync(resourceId);
        return childIds.Contains(parentId);
    }

    /// <summary>
    /// 根据资源路径获取资源
    /// </summary>
    public async Task<SysResource?> GetByResourcePathAsync(string resourcePath)
    {
        return await GetFirstAsync(r => r.ResourcePath == resourcePath);
    }

    /// <summary>
    /// 获取公共资源列表（不需要认证）
    /// </summary>
    public async Task<List<SysResource>> GetPublicResourcesAsync()
    {
        return await GetListAsync(r => r.IsPublic == true && r.Status == YesOrNo.Yes);
    }

    /// <summary>
    /// 获取需要认证的资源列表
    /// </summary>
    public async Task<List<SysResource>> GetAuthRequiredResourcesAsync()
    {
        return await GetListAsync(r => r.RequireAuth == true && r.Status == YesOrNo.Yes);
    }

    /// <summary>
    /// 递归获取父资源ID
    /// </summary>
    private async Task GetParentResourceIdsRecursiveAsync(long resourceId, List<long> result)
    {
        var resource = await GetByIdAsync(resourceId);
        if (resource?.ParentId != null && resource.ParentId.Value != default)
        {
            if (!result.Contains(resource.ParentId.Value))
            {
                result.Add(resource.ParentId.Value);
                await GetParentResourceIdsRecursiveAsync(resource.ParentId.Value, result);
            }
        }
    }

    /// <summary>
    /// 递归获取子资源ID
    /// </summary>
    private async Task GetChildResourceIdsRecursiveAsync(long resourceId, List<long> result)
    {
        var children = await GetChildrenAsync(resourceId);
        foreach (var child in children)
        {
            if (!result.Contains(child.BasicId))
            {
                result.Add(child.BasicId);
                await GetChildResourceIdsRecursiveAsync(child.BasicId, result);
            }
        }
    }
}
