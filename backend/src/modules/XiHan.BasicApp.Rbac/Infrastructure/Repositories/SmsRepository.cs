#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsRepository
// Guid:2c424269-641f-487f-9e93-43f8b0fd8022
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:18:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 短信仓储实现
/// </summary>
public class SmsRepository : SqlSugarAggregateRepository<SysSms, long>, ISmsRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SmsRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 获取待发送短信
    /// </summary>
    public async Task<IReadOnlyList<SysSms>> GetPendingSmsAsync(int maxCount = 100, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var take = maxCount <= 0 ? 100 : maxCount;
        var resolvedTenantId = tenantId;

        var query = CreateTenantQueryable()
            .Where(sms => sms.SmsStatus == SmsStatus.Pending);

        if (resolvedTenantId.HasValue)
        {
            query = query.Where(sms => sms.TenantId == resolvedTenantId.Value);
        }
        else
        {
            query = query.Where(sms => sms.TenantId == null);
        }

        return await query
            .OrderBy(sms => sms.ScheduledTime)
            .OrderBy(sms => sms.CreatedTime)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
