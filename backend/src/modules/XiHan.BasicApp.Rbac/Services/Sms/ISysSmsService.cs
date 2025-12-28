#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysSmsService
// Guid:x1y2z3a4-b5c6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Sms.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Sms;

/// <summary>
/// 系统短信服务接口
/// </summary>
public interface ISysSmsService : ICrudApplicationService<SmsDto, XiHanBasicAppIdType, CreateSmsDto, UpdateSmsDto>
{
    /// <summary>
    /// 根据短信状态获取短信列表
    /// </summary>
    /// <param name="smsStatus">短信状态</param>
    /// <returns></returns>
    Task<List<SmsDto>> GetByStatusAsync(SmsStatus smsStatus);

    /// <summary>
    /// 根据短信类型获取短信列表
    /// </summary>
    /// <param name="smsType">短信类型</param>
    /// <returns></returns>
    Task<List<SmsDto>> GetByTypeAsync(SmsType smsType);

    /// <summary>
    /// 根据手机号获取短信列表
    /// </summary>
    /// <param name="toPhone">手机号</param>
    /// <returns></returns>
    Task<List<SmsDto>> GetByToPhoneAsync(string toPhone);

    /// <summary>
    /// 根据发送者ID获取短信列表
    /// </summary>
    /// <param name="senderId">发送者ID</param>
    /// <returns></returns>
    Task<List<SmsDto>> GetBySenderIdAsync(XiHanBasicAppIdType senderId);

    /// <summary>
    /// 根据接收者ID获取短信列表
    /// </summary>
    /// <param name="receiverId">接收者ID</param>
    /// <returns></returns>
    Task<List<SmsDto>> GetByReceiverIdAsync(XiHanBasicAppIdType receiverId);

    /// <summary>
    /// 获取待发送的短信列表
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns></returns>
    Task<List<SmsDto>> GetPendingSmsAsync(int count = 100);
}

