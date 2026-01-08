#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthCodeRepository
// Guid:f2a3b4c5-d6e7-4f5a-8b9c-1d2e3f4a5b6c
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

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// OAuth授权码仓储实现
/// </summary>
public class OAuthCodeRepository : SqlSugarRepositoryBase<SysOAuthCode, long>, IOAuthCodeRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthCodeRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据授权码查询
    /// </summary>
    public async Task<SysOAuthCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOAuthCode>()
            .FirstAsync(c => c.Code == code, cancellationToken);
    }

    /// <summary>
    /// 验证授权码并删除（授权码一次性使用）
    /// </summary>
    public async Task<SysOAuthCode?> ValidateAndRemoveAsync(string code, string clientId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentTime = DateTimeOffset.UtcNow;
        var oauthCode = await _dbClient.Queryable<SysOAuthCode>()
            .FirstAsync(c => c.Code == code
                && c.ClientId == clientId
                && !c.IsUsed
                && c.ExpiresAt > currentTime, cancellationToken);

        if (oauthCode != null)
        {
            // 标记为已使用或直接删除
            await _dbClient.Deleteable<SysOAuthCode>()
                .Where(c => c.BasicId == oauthCode.BasicId)
                .ExecuteCommandAsync(cancellationToken);
        }

        return oauthCode;
    }

    /// <summary>
    /// 清理过期授权码
    /// </summary>
    public async Task<int> CleanExpiredCodesAsync(DateTimeOffset currentTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Deleteable<SysOAuthCode>()
            .Where(c => c.ExpiresAt < currentTime || c.IsUsed)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户的授权码列表
    /// </summary>
    public async Task<List<SysOAuthCode>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOAuthCode>()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }
}
