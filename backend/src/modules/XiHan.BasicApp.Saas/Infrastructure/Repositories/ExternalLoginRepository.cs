#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExternalLoginRepository
// Guid:a1b2c3d4-5e6f-7890-abcd-ef1234567812
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 第三方登录仓储实现
/// </summary>
public class ExternalLoginRepository : IExternalLoginRepository
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExternalLoginRepository(ISqlSugarDbContext dbContext)
    {
        _db = dbContext.GetClient();
    }

    /// <inheritdoc/>
    public async Task<SysExternalLogin?> FindByProviderAsync(string provider, string providerKey, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<SysExternalLogin>()
            .Where(e => e.Provider == provider && e.ProviderKey == providerKey)
            .WhereIF(tenantId.HasValue, e => e.TenantId == tenantId)
            .WhereIF(!tenantId.HasValue, e => e.TenantId == null)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SysExternalLogin>> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<SysExternalLogin>()
            .Where(e => e.UserId == userId)
            .WhereIF(tenantId.HasValue, e => e.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SysExternalLogin> AddAsync(SysExternalLogin entity, CancellationToken cancellationToken = default)
    {
        return await _db.Insertable(entity).ExecuteReturnEntityAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SysExternalLogin entity, CancellationToken cancellationToken = default)
    {
        await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(long userId, string provider, CancellationToken cancellationToken = default)
    {
        await _db.Deleteable<SysExternalLogin>()
            .Where(e => e.UserId == userId && e.Provider == provider)
            .ExecuteCommandAsync(cancellationToken);
    }
}
