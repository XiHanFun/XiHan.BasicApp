#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthCodeRepository
// Guid:beb2c3d4-e5f6-7890-abcd-ef123456789d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.OAuthCodes;

/// <summary>
/// 系统OAuth授权码仓储实现
/// </summary>
public class SysOAuthCodeRepository : SqlSugarRepositoryBase<SysOAuthCode, XiHanBasicAppIdType>, ISysOAuthCodeRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysOAuthCodeRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据授权码获取
    /// </summary>
    public async Task<SysOAuthCode?> GetByCodeAsync(string code)
    {
        return await GetFirstAsync(c => c.Code == code);
    }

    /// <summary>
    /// 根据客户端ID和用户ID获取授权码列表
    /// </summary>
    public async Task<List<SysOAuthCode>> GetByClientAndUserAsync(string clientId, XiHanBasicAppIdType userId)
    {
        var result = await GetListAsync(c => c.ClientId == clientId && c.UserId == userId);
        return result.OrderByDescending(c => c.CreatedTime).ToList();
    }

    /// <summary>
    /// 删除过期的授权码
    /// </summary>
    public async Task<int> DeleteExpiredCodesAsync()
    {
        return await _dbContext.GetClient()
            .Deleteable<SysOAuthCode>()
            .Where(c => c.ExpiresAt < DateTimeOffset.Now)
            .ExecuteCommandAsync();
    }
}
