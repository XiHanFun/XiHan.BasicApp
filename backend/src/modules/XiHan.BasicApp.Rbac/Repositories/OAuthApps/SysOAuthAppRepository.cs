#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthAppRepository
// Guid:bdb2c3d4-e5f6-7890-abcd-ef123456789c
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

namespace XiHan.BasicApp.Rbac.Repositories.OAuthApps;

/// <summary>
/// 系统OAuth应用仓储实现
/// </summary>
public class SysOAuthAppRepository : SqlSugarRepositoryBase<SysOAuthApp, XiHanBasicAppIdType>, ISysOAuthAppRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysOAuthAppRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据客户端ID获取应用
    /// </summary>
    public async Task<SysOAuthApp?> GetByClientIdAsync(string clientId)
    {
        return await GetFirstAsync(app => app.ClientId == clientId);
    }

    /// <summary>
    /// 根据应用名称获取应用
    /// </summary>
    public async Task<SysOAuthApp?> GetByAppNameAsync(string appName)
    {
        return await GetFirstAsync(app => app.AppName == appName);
    }

    /// <summary>
    /// 检查客户端ID是否存在
    /// </summary>
    public async Task<bool> ExistsByClientIdAsync(string clientId, XiHanBasicAppIdType? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysOAuthApp>().Where(app => app.ClientId == clientId);
        if (excludeId.HasValue)
        {
            query = query.Where(app => app.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }
}
