#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthRepository
// Guid:e5f6a7b8-c9d0-1234-5678-901234e90123
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
/// OAuth仓储实现
/// </summary>
public class SysOAuthRepository : SqlSugarAggregateRepository<SysOAuthApp, long>, ISysOAuthRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysOAuthRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    // ========== OAuth应用 ==========

    /// <summary>
    /// 根据ClientId获取应用
    /// </summary>
    public async Task<SysOAuthApp?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysOAuthApp>()
            .Where(app => app.ClientId == clientId && app.Status == YesOrNo.Yes)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 验证ClientSecret
    /// </summary>
    public async Task<bool> ValidateClientSecretAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysOAuthApp>()
            .Where(app => app.ClientId == clientId && app.ClientSecret == clientSecret && app.Status == YesOrNo.Yes)
            .AnyAsync(cancellationToken);
    }

    // ========== OAuth授权码 ==========

    /// <summary>
    /// 添加授权码
    /// </summary>
    public async Task<SysOAuthCode> AddOAuthCodeAsync(SysOAuthCode code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(code).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 根据授权码获取
    /// </summary>
    public async Task<SysOAuthCode?> GetOAuthCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysOAuthCode>()
            .Where(c => c.Code == code && c.State == YesOrNo.Yes)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 删除授权码
    /// </summary>
    public async Task DeleteOAuthCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysOAuthCode>()
            .Where(c => c.Code == code)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 清理过期授权码
    /// </summary>
    public async Task CleanExpiredCodesAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysOAuthCode>()
            .Where(c => c.ExpiresTime < beforeDate)
            .ExecuteCommandAsync(cancellationToken);
    }

    // ========== OAuth令牌 ==========

    /// <summary>
    /// 添加令牌
    /// </summary>
    public async Task<SysOAuthToken> AddOAuthTokenAsync(SysOAuthToken token, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(token).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 根据AccessToken获取令牌
    /// </summary>
    public async Task<SysOAuthToken?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysOAuthToken>()
            .Where(t => t.AccessToken == accessToken && t.State == YesOrNo.Yes)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据RefreshToken获取令牌
    /// </summary>
    public async Task<SysOAuthToken?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysOAuthToken>()
            .Where(t => t.RefreshToken == refreshToken && t.State == YesOrNo.Yes)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 更新令牌
    /// </summary>
    public async Task UpdateOAuthTokenAsync(SysOAuthToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable(token).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 撤销令牌
    /// </summary>
    public async Task RevokeTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysOAuthToken>()
            .SetColumns(t => new SysOAuthToken
            {
                State = YesOrNo.No,
                RevokedTime = DateTimeOffset.UtcNow
            })
            .Where(t => t.AccessToken == accessToken)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 清理过期令牌
    /// </summary>
    public async Task CleanExpiredTokensAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysOAuthToken>()
            .Where(t => t.AccessTokenExpiresTime < beforeDate)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户的所有令牌
    /// </summary>
    public async Task<List<SysOAuthToken>> GetUserTokensAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysOAuthToken>()
            .Where(t => t.UserId == userId && t.State == YesOrNo.Yes)
            .OrderByDescending(t => t.CreatedTime)
            .ToListAsync(cancellationToken);
    }
}
