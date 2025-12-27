#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSmsRepository
// Guid:bbb2c3d4-e5f6-7890-abcd-ef123456789a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 系统短信仓储实现
/// </summary>
public class SysSmsRepository : SqlSugarRepositoryBase<SysSms, XiHanBasicAppIdType>, ISysSmsRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysSmsRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据短信状态获取短信列表
    /// </summary>
    public async Task<List<SysSms>> GetByStatusAsync(SmsStatus smsStatus)
    {
        var result = await GetListAsync(sms => sms.SmsStatus == smsStatus);
        return result.OrderByDescending(sms => sms.CreatedTime).ToList();
    }

    /// <summary>
    /// 根据短信类型获取短信列表
    /// </summary>
    public async Task<List<SysSms>> GetByTypeAsync(SmsType smsType)
    {
        var result = await GetListAsync(sms => sms.SmsType == smsType);
        return result.OrderByDescending(sms => sms.CreatedTime).ToList();
    }

    /// <summary>
    /// 根据手机号获取短信列表
    /// </summary>
    public async Task<List<SysSms>> GetByToPhoneAsync(string toPhone)
    {
        var result = await GetListAsync(sms => sms.ToPhone.Contains(toPhone));
        return result.OrderByDescending(sms => sms.SendTime ?? sms.CreatedTime).ToList();
    }

    /// <summary>
    /// 根据发送者ID获取短信列表
    /// </summary>
    public async Task<List<SysSms>> GetBySenderIdAsync(XiHanBasicAppIdType senderId)
    {
        var result = await GetListAsync(sms => sms.SenderId == senderId);
        return result.OrderByDescending(sms => sms.SendTime ?? sms.CreatedTime).ToList();
    }

    /// <summary>
    /// 根据接收者ID获取短信列表
    /// </summary>
    public async Task<List<SysSms>> GetByReceiverIdAsync(XiHanBasicAppIdType receiverId)
    {
        var result = await GetListAsync(sms => sms.ReceiverId == receiverId);
        return result.OrderByDescending(sms => sms.SendTime ?? sms.CreatedTime).ToList();
    }

    /// <summary>
    /// 获取待发送的短信列表
    /// </summary>
    public async Task<List<SysSms>> GetPendingSmsAsync(int count = 100)
    {
        return await _dbContext.GetClient()
            .Queryable<SysSms>()
            .Where(sms => sms.SmsStatus == SmsStatus.Pending)
            .OrderBy(sms => sms.ScheduledTime ?? sms.CreatedTime)
            .Take(count)
            .ToListAsync();
    }
}
