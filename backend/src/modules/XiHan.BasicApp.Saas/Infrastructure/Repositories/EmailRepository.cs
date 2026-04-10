#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailRepository
// Guid:d99d2ccf-c241-4f23-8d76-3cc8e13a8cce
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:17:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 邮件仓储实现
/// </summary>
public class EmailRepository : SqlSugarAggregateRepository<SysEmail, long>, IEmailRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public EmailRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 获取待发送邮件
    /// </summary>
    public async Task<IReadOnlyList<SysEmail>> GetPendingEmailsAsync(int maxCount = 100, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var take = maxCount <= 0 ? 100 : maxCount;
        var resolvedTenantId = tenantId;

        var query = CreateTenantQueryable()
            .Where(email => email.EmailStatus == EmailStatus.Pending);

        query = resolvedTenantId.HasValue
            ? query.Where(email => email.TenantId == resolvedTenantId.Value)
            : query.Where(email => email.TenantId == null);

        return await query
            .OrderBy(email => email.ScheduledTime)
            .OrderBy(email => email.CreatedTime)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
