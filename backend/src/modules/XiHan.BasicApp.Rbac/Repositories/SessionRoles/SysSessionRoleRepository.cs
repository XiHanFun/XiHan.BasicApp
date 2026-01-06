#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSessionRoleRepository
// Guid:8a2b3c4d-5e6f-7890-abcd-ef1234567807
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 15:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.SessionRoles;

/// <summary>
/// 系统会话角色仓储实现
/// </summary>
public class SysSessionRoleRepository : SqlSugarRepositoryBase<SysSessionRole, long>, ISysSessionRoleRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysSessionRoleRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取会话中的活跃角色列表
    /// </summary>
    public async Task<List<SysSessionRole>> GetActiveRolesAsync(long sessionId)
    {
        return await GetListAsync(sr => sr.SessionId == sessionId && sr.Status == SessionRoleStatus.Active);
    }

    /// <summary>
    /// 获取会话中的所有角色（包括已停用）
    /// </summary>
    public async Task<List<SysSessionRole>> GetAllSessionRolesAsync(long sessionId)
    {
        return await GetListAsync(sr => sr.SessionId == sessionId);
    }

    /// <summary>
    /// 激活会话角色
    /// </summary>
    public async Task ActivateRoleAsync(long sessionId, long roleId)
    {
        var sessionRole = await GetFirstAsync(sr => sr.SessionId == sessionId && sr.RoleId == roleId);
        if (sessionRole == null)
        {
            sessionRole = new SysSessionRole
            {
                SessionId = sessionId,
                RoleId = roleId,
                ActivatedAt = DateTimeOffset.Now,
                Status = SessionRoleStatus.Active
            };
            await AddAsync(sessionRole);
        }
        else
        {
            sessionRole.ActivatedAt = DateTimeOffset.Now;
            sessionRole.DeactivatedAt = null;
            sessionRole.Status = SessionRoleStatus.Active;
            await UpdateAsync(sessionRole);
        }
    }

    /// <summary>
    /// 停用会话角色
    /// </summary>
    public async Task DeactivateRoleAsync(long sessionId, long roleId)
    {
        var sessionRole = await GetFirstAsync(sr => sr.SessionId == sessionId && sr.RoleId == roleId);
        if (sessionRole != null)
        {
            sessionRole.DeactivatedAt = DateTimeOffset.Now;
            sessionRole.Status = SessionRoleStatus.Inactive;
            await UpdateAsync(sessionRole);
        }
    }

    /// <summary>
    /// 检查角色是否在会话中激活
    /// </summary>
    public async Task<bool> IsRoleActiveAsync(long sessionId, long roleId)
    {
        return await _dbContext.GetClient().Queryable<SysSessionRole>()
            .Where(sr => sr.SessionId == sessionId && sr.RoleId == roleId && sr.Status == SessionRoleStatus.Active)
            .AnyAsync();
    }

    /// <summary>
    /// 获取会话中激活的角色ID列表
    /// </summary>
    public async Task<List<long>> GetActiveRoleIdsAsync(long sessionId)
    {
        return await _dbContext.GetClient().Queryable<SysSessionRole>()
            .Where(sr => sr.SessionId == sessionId && sr.Status == SessionRoleStatus.Active)
            .Select(sr => sr.RoleId)
            .ToListAsync();
    }

    /// <summary>
    /// 清除会话中的所有角色
    /// </summary>
    public async Task ClearSessionRolesAsync(long sessionId)
    {
        await _dbContext.GetClient().Deleteable<SysSessionRole>()
            .Where(sr => sr.SessionId == sessionId)
            .ExecuteCommandAsync();
    }
}
