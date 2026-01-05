#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthTokenRepository
// Guid:bfb2c3d4-e5f6-7890-abcd-ef123456789e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.OAuthTokens;

/// <summary>
/// 系统OAuth令牌仓储实现
/// </summary>
public class SysOAuthTokenRepository : SqlSugarRepositoryBase<SysOAuthToken, long>, ISysOAuthTokenRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysOAuthTokenRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据访问令牌获取
    /// </summary>
    public async Task<SysOAuthToken?> GetByAccessTokenAsync(string accessToken)
    {
        return await GetFirstAsync(t => t.AccessToken == accessToken);
    }

    /// <summary>
    /// 根据刷新令牌获取
    /// </summary>
    public async Task<SysOAuthToken?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await GetFirstAsync(t => t.RefreshToken == refreshToken);
    }

    /// <summary>
    /// 根据客户端ID和用户ID获取令牌列表
    /// </summary>
    public async Task<List<SysOAuthToken>> GetByClientAndUserAsync(string clientId, long userId)
    {
        var result = await GetListAsync(t => t.ClientId == clientId && t.UserId == userId);
        return [.. result.OrderByDescending(t => t.CreatedTime)];
    }

    /// <summary>
    /// 删除过期的令牌
    /// </summary>
    public async Task<int> DeleteExpiredTokensAsync()
    {
        return await _dbContext.GetClient()
            .Deleteable<SysOAuthToken>()
            .Where(t => t.AccessTokenExpiresAt < DateTimeOffset.Now && (t.RefreshTokenExpiresAt == null || t.RefreshTokenExpiresAt < DateTimeOffset.Now))
            .ExecuteCommandAsync();
    }
}
