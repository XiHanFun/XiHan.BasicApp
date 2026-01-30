#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRepository
// Guid:a9b0c1d2-e3f4-5678-9012-345678a34567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 用户聚合仓储实现
/// </summary>
public class SysUserRepository : SqlSugarAggregateRepository<SysUser, long>, ISysUserRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    public async Task<SysUser?> GetByUserNameAsync(string userName, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>()
            .Where(u => u.UserName == userName && u.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    public async Task<SysUser?> GetByEmailAsync(string email, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>()
            .Where(u => u.Email == email && u.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    public async Task<SysUser?> GetByPhoneAsync(string phone, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>()
            .Where(u => u.Phone == phone && u.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户及安全信息
    /// </summary>
    public async Task<SysUser?> GetWithSecurityAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.GetClient().Queryable<SysUser>()
            .InnerJoin<SysUserSecurity>((u, s) => u.BasicId == s.UserId)
            .Where(u => u.BasicId == userId)
            .Select((u, s) => new SysUser
            {
                //BasicId = u.BasicId,
                UserName = u.UserName,
                Password = u.Password,
                // 其他用户字段...
            })
            .FirstAsync(cancellationToken);

        return user;
    }

    /// <summary>
    /// 获取用户及统计信息
    /// </summary>
    public async Task<SysUser?> GetWithStatisticsAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(userId, cancellationToken);
    }

    /// <summary>
    /// 更新最后登录信息
    /// </summary>
    public async Task UpdateLastLoginAsync(long userId, string? ip = null, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysUser>()
            .SetColumns(u => new SysUser
            {
                LastLoginTime = DateTimeOffset.UtcNow,
                LastLoginIp = ip
            })
            .Where(u => u.BasicId == userId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 批量获取用户
    /// </summary>
    public async Task<List<SysUser>> GetByIdsAsync(IEnumerable<long> userIds, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysUser>()
            .In(u => u.BasicId, userIds.ToArray())
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    public async Task<bool> ExistsByUserNameAsync(string userName, long? excludeUserId = null, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>()
            .Where(u => u.UserName == userName);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.BasicId != excludeUserId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
