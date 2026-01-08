#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppRepository
// Guid:e1f2a3b4-c5d6-4e5f-7a8b-0c1d2e3f4a5b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// OAuth应用仓储实现
/// </summary>
public class OAuthAppRepository : SqlSugarAggregateRepository<SysOAuthApp, long>, IOAuthAppRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据客户端ID查询应用
    /// </summary>
    public async Task<SysOAuthApp?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOAuthApp>()
            .FirstAsync(a => a.ClientId == clientId, cancellationToken);
    }

    /// <summary>
    /// 检查客户端ID是否存在
    /// </summary>
    public async Task<bool> ExistsByClientIdAsync(string clientId, long? excludeAppId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysOAuthApp>()
            .Where(a => a.ClientId == clientId);

        if (excludeAppId.HasValue)
        {
            query = query.Where(a => a.BasicId != excludeAppId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 验证客户端凭证
    /// </summary>
    public async Task<SysOAuthApp?> ValidateClientCredentialsAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOAuthApp>()
            .FirstAsync(a => a.ClientId == clientId && a.ClientSecret == clientSecret, cancellationToken);
    }

    /// <summary>
    /// 获取用户创建的应用列表
    /// </summary>
    public async Task<List<SysOAuthApp>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOAuthApp>()
            .Where(a => a.CreatedId == userId)
            .OrderBy(a => a.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }
}
