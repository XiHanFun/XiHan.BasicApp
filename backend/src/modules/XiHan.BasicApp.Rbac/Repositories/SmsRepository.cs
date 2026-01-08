#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsRepository
// Guid:c1d2e3f4-a5b6-4c5d-7e8f-0a1b2c3d4e5f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
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
/// 短信仓储实现
/// </summary>
public class SmsRepository : SqlSugarAggregateRepository<SysSms, long>, ISmsRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SmsRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据手机号获取短信列表
    /// </summary>
    public async Task<List<SysSms>> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysSms>()
            .Where(s => s.ToPhone.Contains(phoneNumber))
            .OrderBy(s => s.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据发送状态获取短信列表
    /// </summary>
    public async Task<List<SysSms>> GetBySendStatusAsync(SmsStatus smsStatus, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysSms>()
            .Where(s => s.SmsStatus == smsStatus)
            .OrderBy(s => s.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取待发送的短信列表
    /// </summary>
    public async Task<List<SysSms>> GetPendingSmsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysSms>()
            .Where(s => s.SmsStatus == SmsStatus.Pending)
            .OrderBy(s => s.CreatedTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 更新短信发送状态
    /// </summary>
    public async Task<bool> UpdateSendStatusAsync(long smsId, SmsStatus smsStatus, DateTimeOffset? sendTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysSms>()
            .SetColumns(s => new SysSms
            {
                SmsStatus = smsStatus,
                SendTime = sendTime
            })
            .Where(s => s.BasicId == smsId)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <summary>
    /// 获取指定时间段内的短信列表
    /// </summary>
    public async Task<List<SysSms>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysSms>()
            .Where(s => s.CreatedTime >= startTime && s.CreatedTime <= endTime)
            .OrderBy(s => s.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }
}
