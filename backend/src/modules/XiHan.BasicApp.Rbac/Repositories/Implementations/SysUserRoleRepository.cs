#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRoleRepository
// Guid:9138221b-73ae-4283-bbd5-df1966a15f90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 系统用户角色仓储实现
/// </summary>
public class SysUserRoleRepository : SqlSugarReadOnlyRepository<SysUserRole, long>, ISysUserRoleRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserRoleRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户ID获取用户角色列表
    /// </summary>
    public async Task<List<SysUserRole>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysUserRole>()
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据角色ID获取用户角色列表
    /// </summary>
    public async Task<List<SysUserRole>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysUserRole>()
            .Where(ur => ur.RoleId == roleId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 删除用户的所有角色
    /// </summary>
    public async Task DeleteByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysUserRole>()
            .Where(ur => ur.UserId == userId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 保存用户角色
    /// </summary>
    public async Task<SysUserRole> SaveAsync(SysUserRole userRole, CancellationToken cancellationToken = default)
    {
        if (userRole.BasicId == 0)
        {
            // 新增
            await _dbContext.GetClient().Insertable(userRole).ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            // 更新
            await _dbContext.GetClient().Updateable(userRole).ExecuteCommandAsync(cancellationToken);
        }

        return userRole;
    }
}
