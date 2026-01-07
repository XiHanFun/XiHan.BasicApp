#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsCommandService
// Guid:c9d0e1f2-a3b4-4c5d-6e7f-8a9b0c1d2e3f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// 短信命令服务（处理短信的写操作）
/// </summary>
public class SmsCommandService : CrudApplicationServiceBase<SysSms, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly ISmsRepository _smsRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SmsCommandService(ISmsRepository smsRepository)
        : base(smsRepository)
    {
        _smsRepository = smsRepository;
    }

    /// <summary>
    /// 发送短信（创建短信记录并标记为待发送）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        var sms = input.Adapt<SysSms>();

        // 设置初始状态
        sms.SmsStatus = SmsStatus.Pending;
        sms.SendTime = DateTimeOffset.UtcNow;
        sms.RetryCount = 0;

        sms = await _smsRepository.AddAsync(sms);

        // TODO: 触发短信发送任务
        // await _smsSender.SendAsync(sms);

        return await MapToEntityDtoAsync(sms);
    }

    /// <summary>
    /// 更新短信发送状态
    /// </summary>
    /// <param name="smsId">短信ID</param>
    /// <param name="status">发送状态</param>
    /// <param name="errorMessage">错误信息</param>
    public async Task<bool> UpdateSendStatusAsync(long smsId, SmsStatus status, string? errorMessage = null)
    {
        var sms = await _smsRepository.GetByIdAsync(smsId);
        if (sms == null)
        {
            return false;
        }

        sms.SmsStatus = status;
        if (!string.IsNullOrEmpty(errorMessage))
        {
            sms.ErrorMessage = errorMessage;
        }

        await _smsRepository.UpdateAsync(sms);
        return true;
    }

    /// <summary>
    /// 重试发送失败的短信
    /// </summary>
    /// <param name="smsId">短信ID</param>
    public async Task<bool> RetryFailedSmsAsync(long smsId)
    {
        var sms = await _smsRepository.GetByIdAsync(smsId);
        if (sms == null || sms.SmsStatus != Enums.SmsStatus.Failed)
        {
            return false;
        }

        sms.SmsStatus = Enums.SmsStatus.Pending;
        sms.RetryCount++;
        sms.SendTime = DateTimeOffset.UtcNow;

        await _smsRepository.UpdateAsync(sms);

        // TODO: 触发短信发送任务
        // await _smsSender.SendAsync(sms);

        return true;
    }

    /// <summary>
    /// 批量删除已发送的旧短信
    /// </summary>
    /// <param name="beforeDate">删除此日期之前的短信</param>
    public async Task<int> DeleteSentSmsBeforeDateAsync(DateTimeOffset beforeDate)
    {
        return await _smsRepository.DeleteSentSmsBeforeDateAsync(beforeDate);
    }
}
