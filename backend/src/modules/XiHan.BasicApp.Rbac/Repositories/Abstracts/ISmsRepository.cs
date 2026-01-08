#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISmsRepository
// Guid:c1d2e3f4-a5b6-4c5d-7e8f-0a1b2c3d4e5f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 短信仓储接口
/// </summary>
public interface ISmsRepository : IAggregateRootRepository<SysSms, long>
{
    /// <summary>
    /// 根据手机号获取短信列表
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信列表</returns>
    Task<List<SysSms>> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据发送状态获取短信列表
    /// </summary>
    /// <param name="smsStatus">发送状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信列表</returns>
    Task<List<SysSms>> GetBySendStatusAsync(SmsStatus smsStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待发送的短信列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信列表</returns>
    Task<List<SysSms>> GetPendingSmsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新短信发送状态
    /// </summary>
    /// <param name="smsId">短信ID</param>
    /// <param name="smsStatus">发送状态</param>
    /// <param name="sendTime">发送时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateSendStatusAsync(long smsId, SmsStatus smsStatus, DateTimeOffset? sendTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间段内的短信列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信列表</returns>
    Task<List<SysSms>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);
}
