#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSmsService
// Guid:y1z2a3b4-c5d6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.Sms;
using XiHan.BasicApp.Rbac.Services.Roles.Dtos;
using XiHan.BasicApp.Rbac.Services.Sms.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Sms;

/// <summary>
/// 系统短信服务实现
/// </summary>
public class SysSmsService : CrudApplicationServiceBase<SysSms, SmsDto, XiHanBasicAppIdType, CreateSmsDto, UpdateSmsDto>, ISysSmsService
{
    private readonly ISysSmsRepository _smsRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysSmsService(ISysSmsRepository smsRepository) : base(smsRepository)
    {
        _smsRepository = smsRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据短信状态获取短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetByStatusAsync(SmsStatus smsStatus)
    {
        var smsList = await _smsRepository.GetByStatusAsync(smsStatus);
        return smsList.Adapt<List<SmsDto>>();
    }

    /// <summary>
    /// 根据短信类型获取短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetByTypeAsync(SmsType smsType)
    {
        var smsList = await _smsRepository.GetByTypeAsync(smsType);
        return smsList.Adapt<List<SmsDto>>();
    }

    /// <summary>
    /// 根据手机号获取短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetByToPhoneAsync(string toPhone)
    {
        var smsList = await _smsRepository.GetByToPhoneAsync(toPhone);
        return smsList.Adapt<List<SmsDto>>();
    }

    /// <summary>
    /// 根据发送者ID获取短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetBySenderIdAsync(XiHanBasicAppIdType senderId)
    {
        var smsList = await _smsRepository.GetBySenderIdAsync(senderId);
        return smsList.Adapt<List<SmsDto>>();
    }

    /// <summary>
    /// 根据接收者ID获取短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetByReceiverIdAsync(XiHanBasicAppIdType receiverId)
    {
        var smsList = await _smsRepository.GetByReceiverIdAsync(receiverId);
        return smsList.Adapt<List<SmsDto>>();
    }

    /// <summary>
    /// 获取待发送的短信列表
    /// </summary>
    public async Task<List<SmsDto>> GetPendingSmsAsync(int count = 100)
    {
        var smsList = await _smsRepository.GetPendingSmsAsync(count);
        return smsList.Adapt<List<SmsDto>>();
    }

    #endregion 业务特定方法
}
