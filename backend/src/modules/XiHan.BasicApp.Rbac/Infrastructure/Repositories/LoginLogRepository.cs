#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginLogRepository
// Guid:62ab869d-0506-48b3-a9e0-407d03fca2c0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:56:24
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 登录日志仓储实现
/// </summary>
public class LoginLogRepository : SqlSugarRepositoryBase<SysLoginLog, long>, ILoginLogRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    public LoginLogRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider)
        : base(clientProvider, currentTenant, serviceProvider)
    {
    }

    /// <summary>
    /// 获取最近失败次数
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="minutes"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> GetRecentFailureCountAsync(string userName, int minutes, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        if (minutes <= 0)
        {
            return 0;
        }

        var since = DateTimeOffset.UtcNow.AddMinutes(-minutes);
        var query = CreateTenantQueryable()
            .Where(log => log.UserName == userName
                          && log.LoginTime >= since
                          && log.LoginResult != LoginResult.Success);

        if (tenantId.HasValue)
        {
            query = query.Where(log => log.TenantId == tenantId.Value);
        }

        var count = await query.CountAsync(cancellationToken);
        return count;
    }
}
