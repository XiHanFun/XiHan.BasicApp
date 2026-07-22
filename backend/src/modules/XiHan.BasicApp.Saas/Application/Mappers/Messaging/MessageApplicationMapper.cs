// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统消息应用层映射器
/// </summary>
public static class MessageApplicationMapper
{
    /// <summary>
    /// 映射邮件创建命令
    /// </summary>
    public static EmailCreateCommand ToCreateCommand(EmailCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new EmailCreateCommand(
            input.SendUserId,
            input.ReceiveUserId,
            input.EmailType,
            input.FromEmail,
            input.FromName,
            input.ToEmail,
            input.CcEmail,
            input.BccEmail,
            input.Subject,
            input.Content,
            input.IsHtml,
            input.Attachments,
            input.TemplateCode,
            input.TemplateParams,
            input.ScheduledTime,
            input.MaxRetryCount,
            input.BusinessType,
            input.BusinessId,
            input.Remark);
    }

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
            TemplateCode = email.TemplateCode,
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
            TemplateCode = item.TemplateCode,
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
    /// 映射邮件更新命令
    /// </summary>
    public static EmailUpdateCommand ToUpdateCommand(EmailUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new EmailUpdateCommand(
            input.BasicId,
            input.ReceiveUserId,
            input.EmailType,
            input.FromEmail,
            input.FromName,
            input.ToEmail,
            input.CcEmail,
            input.BccEmail,
            input.Subject,
            input.Content,
            input.IsHtml,
            input.Attachments,
            input.TemplateCode,
            input.TemplateParams,
            input.ScheduledTime,
            input.MaxRetryCount,
            input.BusinessType,
            input.BusinessId,
            input.Remark);
    }

    /// <summary>
    /// 映射邮件状态更新命令
    /// </summary>
    public static EmailStatusUpdateCommand ToStatusCommand(EmailStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new EmailStatusUpdateCommand(
            input.BasicId,
            input.EmailStatus,
            input.SendTime,
            input.RetryCount,
            input.ErrorMessage,
            input.Remark);
    }

    /// <summary>
    /// 映射短信创建命令
    /// </summary>
    public static SmsCreateCommand ToCreateCommand(SmsCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new SmsCreateCommand(
            input.SenderId,
            input.ReceiverId,
            input.SmsType,
            input.ToPhone,
            input.Content,
            input.TemplateCode,
            input.TemplateParams,
            input.Provider,
            input.ScheduledTime,
            input.MaxRetryCount,
            input.BusinessType,
            input.BusinessId,
            input.Remark);
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
            TemplateCode = sms.TemplateCode,
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
            TemplateCode = item.TemplateCode,
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

    /// <summary>
    /// 映射短信更新命令
    /// </summary>
    public static SmsUpdateCommand ToUpdateCommand(SmsUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new SmsUpdateCommand(
            input.BasicId,
            input.ReceiverId,
            input.SmsType,
            input.ToPhone,
            input.Content,
            input.TemplateCode,
            input.TemplateParams,
            input.Provider,
            input.ScheduledTime,
            input.MaxRetryCount,
            input.BusinessType,
            input.BusinessId,
            input.Remark);
    }

    /// <summary>
    /// 映射短信状态更新命令
    /// </summary>
    public static SmsStatusUpdateCommand ToStatusCommand(SmsStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new SmsStatusUpdateCommand(
            input.BasicId,
            input.SmsStatus,
            input.SendTime,
            input.ProviderMessageId,
            input.RetryCount,
            input.Cost,
            input.ErrorMessage,
            input.Remark);
    }
}
