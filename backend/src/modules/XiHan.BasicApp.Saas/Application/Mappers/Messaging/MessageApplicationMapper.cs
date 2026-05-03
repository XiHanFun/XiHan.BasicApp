#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageApplicationMapper
// Guid:17ceec8e-7d13-4edf-aeeb-c34f9c203909
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统消息应用层映射器
/// </summary>
public static class MessageApplicationMapper
{
    /// <summary>
    /// 映射系统邮件列表项
    /// </summary>
    /// <param name="email">系统邮件实体</param>
    /// <returns>系统邮件列表项 DTO</returns>
    public static EmailListItemDto ToEmailListItemDto(SysEmail email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return new EmailListItemDto
        {
            BasicId = email.BasicId,
            SendUserId = email.SendUserId,
            ReceiveUserId = email.ReceiveUserId,
            EmailType = email.EmailType,
            Subject = email.Subject,
            IsHtml = email.IsHtml,
            TemplateId = email.TemplateId,
            EmailStatus = email.EmailStatus,
            ScheduledTime = email.ScheduledTime,
            SendTime = email.SendTime,
            RetryCount = email.RetryCount,
            MaxRetryCount = email.MaxRetryCount,
            BusinessType = email.BusinessType,
            BusinessId = email.BusinessId,
            CreatedTime = email.CreatedTime,
            ModifiedTime = email.ModifiedTime
        };
    }

    /// <summary>
    /// 映射系统邮件详情
    /// </summary>
    /// <param name="email">系统邮件实体</param>
    /// <returns>系统邮件详情 DTO</returns>
    public static EmailDetailDto ToEmailDetailDto(SysEmail email)
    {
        ArgumentNullException.ThrowIfNull(email);

        var item = ToEmailListItemDto(email);
        return new EmailDetailDto
        {
            BasicId = item.BasicId,
            SendUserId = item.SendUserId,
            ReceiveUserId = item.ReceiveUserId,
            EmailType = item.EmailType,
            Subject = item.Subject,
            IsHtml = item.IsHtml,
            TemplateId = item.TemplateId,
            EmailStatus = item.EmailStatus,
            ScheduledTime = item.ScheduledTime,
            SendTime = item.SendTime,
            RetryCount = item.RetryCount,
            MaxRetryCount = item.MaxRetryCount,
            BusinessType = item.BusinessType,
            BusinessId = item.BusinessId,
            CreatedTime = item.CreatedTime,
            CreatedId = email.CreatedId,
            CreatedBy = email.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = email.ModifiedId,
            ModifiedBy = email.ModifiedBy
        };
    }

    /// <summary>
    /// 映射系统短信列表项
    /// </summary>
    /// <param name="sms">系统短信实体</param>
    /// <returns>系统短信列表项 DTO</returns>
    public static SmsListItemDto ToSmsListItemDto(SysSms sms)
    {
        ArgumentNullException.ThrowIfNull(sms);

        return new SmsListItemDto
        {
            BasicId = sms.BasicId,
            SenderId = sms.SenderId,
            ReceiverId = sms.ReceiverId,
            SmsType = sms.SmsType,
            TemplateId = sms.TemplateId,
            Provider = sms.Provider,
            SmsStatus = sms.SmsStatus,
            ScheduledTime = sms.ScheduledTime,
            SendTime = sms.SendTime,
            RetryCount = sms.RetryCount,
            MaxRetryCount = sms.MaxRetryCount,
            Cost = sms.Cost,
            BusinessType = sms.BusinessType,
            BusinessId = sms.BusinessId,
            CreatedTime = sms.CreatedTime,
            ModifiedTime = sms.ModifiedTime
        };
    }

    /// <summary>
    /// 映射系统短信详情
    /// </summary>
    /// <param name="sms">系统短信实体</param>
    /// <returns>系统短信详情 DTO</returns>
    public static SmsDetailDto ToSmsDetailDto(SysSms sms)
    {
        ArgumentNullException.ThrowIfNull(sms);

        var item = ToSmsListItemDto(sms);
        return new SmsDetailDto
        {
            BasicId = item.BasicId,
            SenderId = item.SenderId,
            ReceiverId = item.ReceiverId,
            SmsType = item.SmsType,
            TemplateId = item.TemplateId,
            Provider = item.Provider,
            SmsStatus = item.SmsStatus,
            ScheduledTime = item.ScheduledTime,
            SendTime = item.SendTime,
            RetryCount = item.RetryCount,
            MaxRetryCount = item.MaxRetryCount,
            Cost = item.Cost,
            BusinessType = item.BusinessType,
            BusinessId = item.BusinessId,
            CreatedTime = item.CreatedTime,
            CreatedId = sms.CreatedId,
            CreatedBy = sms.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = sms.ModifiedId,
            ModifiedBy = sms.ModifiedBy
        };
    }
}
