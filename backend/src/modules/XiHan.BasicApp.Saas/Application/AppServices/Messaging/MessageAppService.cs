#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageAppService
// Guid:c530bd34-c2b1-4ad5-9b8c-651d807a5593
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统消息命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统消息")]
public sealed class MessageAppService(
    IEmailRepository emailRepository,
    ISmsRepository smsRepository)
    : SaasApplicationService, IMessageAppService
{
    private readonly IEmailRepository _emailRepository = emailRepository;
    private readonly ISmsRepository _smsRepository = smsRepository;

    /// <summary>
    /// 创建系统邮件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Create)]
    public async Task<EmailDetailDto> CreateEmailAsync(EmailCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateEmailInput(input.EmailType, input.FromEmail, input.FromName, input.ToEmail, input.CcEmail, input.BccEmail, input.Subject, input.MaxRetryCount, input.BusinessType, input.BusinessId, input.Remark);
        var email = new SysEmail
        {
            SendUserId = input.SendUserId,
            ReceiveUserId = input.ReceiveUserId,
            EmailType = input.EmailType,
            FromEmail = Required(input.FromEmail, 200, nameof(input.FromEmail), "发件邮箱不能超过 200 个字符。"),
            FromName = Optional(input.FromName, 100, nameof(input.FromName), "发件人不能超过 100 个字符。"),
            ToEmail = Required(input.ToEmail, 500, nameof(input.ToEmail), "收件邮箱不能超过 500 个字符。"),
            CcEmail = Optional(input.CcEmail, 500, nameof(input.CcEmail), "抄送邮箱不能超过 500 个字符。"),
            BccEmail = Optional(input.BccEmail, 500, nameof(input.BccEmail), "密送邮箱不能超过 500 个字符。"),
            Subject = Required(input.Subject, 200, nameof(input.Subject), "邮件主题不能超过 200 个字符。"),
            Content = NormalizeNullable(input.Content),
            IsHtml = input.IsHtml,
            Attachments = OptionalJson(input.Attachments, "邮件附件必须是合法 JSON。"),
            TemplateId = input.TemplateId,
            TemplateParams = OptionalJson(input.TemplateParams, "邮件模板参数必须是合法 JSON。"),
            EmailStatus = EmailStatus.Pending,
            ScheduledTime = input.ScheduledTime,
            MaxRetryCount = input.MaxRetryCount,
            BusinessType = Optional(input.BusinessType, 100, nameof(input.BusinessType), "业务类型不能超过 100 个字符。"),
            BusinessId = input.BusinessId,
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。")
        };

        var savedEmail = await _emailRepository.AddAsync(email, cancellationToken);
        return MessageApplicationMapper.ToEmailDetailDto(savedEmail);
    }

    /// <summary>
    /// 更新系统邮件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Update)]
    public async Task<EmailDetailDto> UpdateEmailAsync(EmailUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统邮件主键必须大于 0。");
        ValidateEmailInput(input.EmailType, input.FromEmail, input.FromName, input.ToEmail, input.CcEmail, input.BccEmail, input.Subject, input.MaxRetryCount, input.BusinessType, input.BusinessId, input.Remark);
        var email = await GetEmailOrThrowAsync(input.BasicId, cancellationToken);
        EnsureMessageEditable(email.EmailStatus, "邮件");
        email.ReceiveUserId = input.ReceiveUserId;
        email.EmailType = input.EmailType;
        email.FromEmail = Required(input.FromEmail, 200, nameof(input.FromEmail), "发件邮箱不能超过 200 个字符。");
        email.FromName = Optional(input.FromName, 100, nameof(input.FromName), "发件人不能超过 100 个字符。");
        email.ToEmail = Required(input.ToEmail, 500, nameof(input.ToEmail), "收件邮箱不能超过 500 个字符。");
        email.CcEmail = Optional(input.CcEmail, 500, nameof(input.CcEmail), "抄送邮箱不能超过 500 个字符。");
        email.BccEmail = Optional(input.BccEmail, 500, nameof(input.BccEmail), "密送邮箱不能超过 500 个字符。");
        email.Subject = Required(input.Subject, 200, nameof(input.Subject), "邮件主题不能超过 200 个字符。");
        email.Content = NormalizeNullable(input.Content);
        email.IsHtml = input.IsHtml;
        email.Attachments = OptionalJson(input.Attachments, "邮件附件必须是合法 JSON。");
        email.TemplateId = input.TemplateId;
        email.TemplateParams = OptionalJson(input.TemplateParams, "邮件模板参数必须是合法 JSON。");
        email.ScheduledTime = input.ScheduledTime;
        email.MaxRetryCount = input.MaxRetryCount;
        email.BusinessType = Optional(input.BusinessType, 100, nameof(input.BusinessType), "业务类型不能超过 100 个字符。");
        email.BusinessId = input.BusinessId;
        email.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var savedEmail = await _emailRepository.UpdateAsync(email, cancellationToken);
        return MessageApplicationMapper.ToEmailDetailDto(savedEmail);
    }

    /// <summary>
    /// 更新系统邮件状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Status)]
    public async Task<EmailDetailDto> UpdateEmailStatusAsync(EmailStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统邮件主键必须大于 0。");
        EnsureEnum(input.EmailStatus, nameof(input.EmailStatus));
        EnsureOptionalNonNegative(input.RetryCount, nameof(input.RetryCount), "重试次数不能小于 0。");
        var email = await GetEmailOrThrowAsync(input.BasicId, cancellationToken);
        email.EmailStatus = input.EmailStatus;
        email.SendTime = input.EmailStatus == EmailStatus.Success
            ? input.SendTime ?? DateTimeOffset.UtcNow
            : input.SendTime;
        email.RetryCount = input.RetryCount ?? email.RetryCount;
        email.ErrorMessage = Optional(input.ErrorMessage, 1000, nameof(input.ErrorMessage), "错误信息不能超过 1000 个字符。");
        email.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? email.Remark;

        var savedEmail = await _emailRepository.UpdateAsync(email, cancellationToken);
        return MessageApplicationMapper.ToEmailDetailDto(savedEmail);
    }

    /// <summary>
    /// 删除系统邮件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Delete)]
    public async Task DeleteEmailAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = await GetEmailOrThrowAsync(id, cancellationToken);
        if (email.EmailStatus == EmailStatus.Sending)
        {
            throw new InvalidOperationException("发送中的邮件不能删除。");
        }

        if (!await _emailRepository.DeleteAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("系统邮件删除失败。");
        }
    }

    /// <summary>
    /// 创建系统短信
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Create)]
    public async Task<SmsDetailDto> CreateSmsAsync(SmsCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateSmsInput(input.SmsType, input.ToPhone, input.Content, input.Provider, input.MaxRetryCount, input.BusinessType, input.BusinessId, input.Remark);
        var sms = new SysSms
        {
            SenderId = input.SenderId,
            ReceiverId = input.ReceiverId,
            SmsType = input.SmsType,
            ToPhone = Required(input.ToPhone, 50, nameof(input.ToPhone), "手机号不能超过 50 个字符。"),
            Content = Required(input.Content, 1000, nameof(input.Content), "短信内容不能超过 1000 个字符。"),
            TemplateId = input.TemplateId,
            TemplateParams = OptionalJson(input.TemplateParams, "短信模板参数必须是合法 JSON。"),
            Provider = Optional(input.Provider, 50, nameof(input.Provider), "短信服务商不能超过 50 个字符。"),
            SmsStatus = SmsStatus.Pending,
            ScheduledTime = input.ScheduledTime,
            MaxRetryCount = input.MaxRetryCount,
            BusinessType = Optional(input.BusinessType, 100, nameof(input.BusinessType), "业务类型不能超过 100 个字符。"),
            BusinessId = input.BusinessId,
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。")
        };

        var savedSms = await _smsRepository.AddAsync(sms, cancellationToken);
        return MessageApplicationMapper.ToSmsDetailDto(savedSms);
    }

    /// <summary>
    /// 更新系统短信
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Update)]
    public async Task<SmsDetailDto> UpdateSmsAsync(SmsUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统短信主键必须大于 0。");
        ValidateSmsInput(input.SmsType, input.ToPhone, input.Content, input.Provider, input.MaxRetryCount, input.BusinessType, input.BusinessId, input.Remark);
        var sms = await GetSmsOrThrowAsync(input.BasicId, cancellationToken);
        EnsureMessageEditable(sms.SmsStatus, "短信");
        sms.ReceiverId = input.ReceiverId;
        sms.SmsType = input.SmsType;
        sms.ToPhone = Required(input.ToPhone, 50, nameof(input.ToPhone), "手机号不能超过 50 个字符。");
        sms.Content = Required(input.Content, 1000, nameof(input.Content), "短信内容不能超过 1000 个字符。");
        sms.TemplateId = input.TemplateId;
        sms.TemplateParams = OptionalJson(input.TemplateParams, "短信模板参数必须是合法 JSON。");
        sms.Provider = Optional(input.Provider, 50, nameof(input.Provider), "短信服务商不能超过 50 个字符。");
        sms.ScheduledTime = input.ScheduledTime;
        sms.MaxRetryCount = input.MaxRetryCount;
        sms.BusinessType = Optional(input.BusinessType, 100, nameof(input.BusinessType), "业务类型不能超过 100 个字符。");
        sms.BusinessId = input.BusinessId;
        sms.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var savedSms = await _smsRepository.UpdateAsync(sms, cancellationToken);
        return MessageApplicationMapper.ToSmsDetailDto(savedSms);
    }

    /// <summary>
    /// 更新系统短信状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Status)]
    public async Task<SmsDetailDto> UpdateSmsStatusAsync(SmsStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统短信主键必须大于 0。");
        EnsureEnum(input.SmsStatus, nameof(input.SmsStatus));
        EnsureOptionalNonNegative(input.RetryCount, nameof(input.RetryCount), "重试次数不能小于 0。");
        EnsureOptionalNonNegative(input.Cost, nameof(input.Cost), "短信成本不能小于 0。");
        var sms = await GetSmsOrThrowAsync(input.BasicId, cancellationToken);
        sms.SmsStatus = input.SmsStatus;
        sms.SendTime = input.SmsStatus == SmsStatus.Success
            ? input.SendTime ?? DateTimeOffset.UtcNow
            : input.SendTime;
        sms.ProviderMessageId = Optional(input.ProviderMessageId, 100, nameof(input.ProviderMessageId), "服务商消息主键不能超过 100 个字符。");
        sms.RetryCount = input.RetryCount ?? sms.RetryCount;
        sms.Cost = input.Cost ?? sms.Cost;
        sms.ErrorMessage = Optional(input.ErrorMessage, 1000, nameof(input.ErrorMessage), "错误信息不能超过 1000 个字符。");
        sms.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? sms.Remark;

        var savedSms = await _smsRepository.UpdateAsync(sms, cancellationToken);
        return MessageApplicationMapper.ToSmsDetailDto(savedSms);
    }

    /// <summary>
    /// 删除系统短信
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Delete)]
    public async Task DeleteSmsAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sms = await GetSmsOrThrowAsync(id, cancellationToken);
        if (sms.SmsStatus == SmsStatus.Sending)
        {
            throw new InvalidOperationException("发送中的短信不能删除。");
        }

        if (!await _smsRepository.DeleteAsync(sms, cancellationToken))
        {
            throw new InvalidOperationException("系统短信删除失败。");
        }
    }

    private async Task<SysEmail> GetEmailOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统邮件主键必须大于 0。");
        return await _emailRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统邮件不存在。");
    }

    private async Task<SysSms> GetSmsOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统短信主键必须大于 0。");
        return await _smsRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统短信不存在。");
    }

    private static void ValidateEmailInput(
        EmailType emailType,
        string fromEmail,
        string? fromName,
        string toEmail,
        string? ccEmail,
        string? bccEmail,
        string subject,
        int maxRetryCount,
        string? businessType,
        long? businessId,
        string? remark)
    {
        EnsureEnum(emailType, nameof(emailType));
        _ = Required(fromEmail, 200, nameof(fromEmail), "发件邮箱不能超过 200 个字符。");
        _ = Optional(fromName, 100, nameof(fromName), "发件人不能超过 100 个字符。");
        _ = Required(toEmail, 500, nameof(toEmail), "收件邮箱不能超过 500 个字符。");
        _ = Optional(ccEmail, 500, nameof(ccEmail), "抄送邮箱不能超过 500 个字符。");
        _ = Optional(bccEmail, 500, nameof(bccEmail), "密送邮箱不能超过 500 个字符。");
        _ = Required(subject, 200, nameof(subject), "邮件主题不能超过 200 个字符。");
        _ = Optional(businessType, 100, nameof(businessType), "业务类型不能超过 100 个字符。");
        _ = Optional(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        EnsureOptionalId(businessId, nameof(businessId), "业务主键必须大于 0。");
        EnsureNonNegative(maxRetryCount, nameof(maxRetryCount), "最大重试次数不能小于 0。");
    }

    private static void ValidateSmsInput(
        SmsType smsType,
        string toPhone,
        string content,
        string? provider,
        int maxRetryCount,
        string? businessType,
        long? businessId,
        string? remark)
    {
        EnsureEnum(smsType, nameof(smsType));
        _ = Required(toPhone, 50, nameof(toPhone), "手机号不能超过 50 个字符。");
        _ = Required(content, 1000, nameof(content), "短信内容不能超过 1000 个字符。");
        _ = Optional(provider, 50, nameof(provider), "短信服务商不能超过 50 个字符。");
        _ = Optional(businessType, 100, nameof(businessType), "业务类型不能超过 100 个字符。");
        _ = Optional(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        EnsureOptionalId(businessId, nameof(businessId), "业务主键必须大于 0。");
        EnsureNonNegative(maxRetryCount, nameof(maxRetryCount), "最大重试次数不能小于 0。");
    }

    private static void EnsureMessageEditable(EmailStatus status, string messageName)
    {
        if (status is EmailStatus.Sending or EmailStatus.Success)
        {
            throw new InvalidOperationException($"{messageName}已发送或发送中，不能直接更新。");
        }
    }

    private static void EnsureMessageEditable(SmsStatus status, string messageName)
    {
        if (status is SmsStatus.Sending or SmsStatus.Success)
        {
            throw new InvalidOperationException($"{messageName}已发送或发送中，不能直接更新。");
        }
    }
}
