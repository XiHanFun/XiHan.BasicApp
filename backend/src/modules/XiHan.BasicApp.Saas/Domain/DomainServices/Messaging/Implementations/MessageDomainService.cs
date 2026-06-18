#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageDomainService
// Guid:7c130680-0ec8-49ac-b54d-2284f89a23ff
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 系统消息领域服务实现
/// </summary>
public sealed class MessageDomainService
    : IMessageDomainService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageDomainService(IEmailRepository emailRepository, ISmsRepository smsRepository)
    {
        _emailRepository = emailRepository;
        _smsRepository = smsRepository;
    }

    private readonly IEmailRepository _emailRepository;
    private readonly ISmsRepository _smsRepository;

    /// <inheritdoc />
    public async Task<EmailCommandResult> CreateOutboxEmailAsync(EmailCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateEmailInput(command.EmailType, command.FromEmail, command.FromName, command.ToEmail, command.CcEmail, command.BccEmail, command.Subject, command.MaxRetryCount, command.BusinessType, command.BusinessId, command.Remark);
        var email = new SysEmail
        {
            SendUserId = command.SendUserId,
            ReceiveUserId = command.ReceiveUserId,
            EmailType = command.EmailType,
            FromEmail = Required(command.FromEmail, 200, nameof(command.FromEmail), "发件邮箱不能超过 200 个字符。"),
            FromName = Optional(command.FromName, 100, nameof(command.FromName), "发件人不能超过 100 个字符。"),
            ToEmail = Required(command.ToEmail, 500, nameof(command.ToEmail), "收件邮箱不能超过 500 个字符。"),
            CcEmail = Optional(command.CcEmail, 500, nameof(command.CcEmail), "抄送邮箱不能超过 500 个字符。"),
            BccEmail = Optional(command.BccEmail, 500, nameof(command.BccEmail), "密送邮箱不能超过 500 个字符。"),
            Subject = Required(command.Subject, 200, nameof(command.Subject), "邮件主题不能超过 200 个字符。"),
            Content = NormalizeNullable(command.Content),
            IsHtml = command.IsHtml,
            Attachments = OptionalJson(command.Attachments, "邮件附件必须是合法 JSON。"),
            TemplateCode = command.TemplateCode,
            TemplateParams = OptionalJson(command.TemplateParams, "邮件模板参数必须是合法 JSON。"),
            EmailStatus = EmailStatus.Pending,
            ScheduledTime = command.ScheduledTime,
            MaxRetryCount = command.MaxRetryCount,
            BusinessType = Optional(command.BusinessType, 100, nameof(command.BusinessType), "业务类型不能超过 100 个字符。"),
            BusinessId = command.BusinessId,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        return new EmailCommandResult(await _emailRepository.AddAsync(email, cancellationToken));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<EmailCommandResult> UpdateEmailAsync(EmailUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统邮件主键必须大于 0。");
        ValidateEmailInput(command.EmailType, command.FromEmail, command.FromName, command.ToEmail, command.CcEmail, command.BccEmail, command.Subject, command.MaxRetryCount, command.BusinessType, command.BusinessId, command.Remark);
        var email = await GetEmailOrThrowAsync(command.BasicId, cancellationToken);
        EnsureMessageEditable(email.EmailStatus, "邮件");

        email.ReceiveUserId = command.ReceiveUserId;
        email.EmailType = command.EmailType;
        email.FromEmail = Required(command.FromEmail, 200, nameof(command.FromEmail), "发件邮箱不能超过 200 个字符。");
        email.FromName = Optional(command.FromName, 100, nameof(command.FromName), "发件人不能超过 100 个字符。");
        email.ToEmail = Required(command.ToEmail, 500, nameof(command.ToEmail), "收件邮箱不能超过 500 个字符。");
        email.CcEmail = Optional(command.CcEmail, 500, nameof(command.CcEmail), "抄送邮箱不能超过 500 个字符。");
        email.BccEmail = Optional(command.BccEmail, 500, nameof(command.BccEmail), "密送邮箱不能超过 500 个字符。");
        email.Subject = Required(command.Subject, 200, nameof(command.Subject), "邮件主题不能超过 200 个字符。");
        email.Content = NormalizeNullable(command.Content);
        email.IsHtml = command.IsHtml;
        email.Attachments = OptionalJson(command.Attachments, "邮件附件必须是合法 JSON。");
        email.TemplateCode = command.TemplateCode;
        email.TemplateParams = OptionalJson(command.TemplateParams, "邮件模板参数必须是合法 JSON。");
        email.ScheduledTime = command.ScheduledTime;
        email.MaxRetryCount = command.MaxRetryCount;
        email.BusinessType = Optional(command.BusinessType, 100, nameof(command.BusinessType), "业务类型不能超过 100 个字符。");
        email.BusinessId = command.BusinessId;
        email.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new EmailCommandResult(await _emailRepository.UpdateAsync(email, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<EmailCommandResult> UpdateEmailStatusAsync(EmailStatusUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统邮件主键必须大于 0。");
        EnsureEnum(command.EmailStatus, nameof(command.EmailStatus));
        EnsureOptionalNonNegative(command.RetryCount, nameof(command.RetryCount), "重试次数不能小于 0。");
        var email = await GetEmailOrThrowAsync(command.BasicId, cancellationToken);

        email.EmailStatus = command.EmailStatus;
        if (command.EmailStatus == EmailStatus.Pending)
        {
            // 重新入队（重发）：给一份全新的重试预算、清空上次错误与发送时间，由发件箱后台重新投递
            email.RetryCount = command.RetryCount ?? 0;
            email.ErrorMessage = null;
            email.SendTime = null;
        }
        else
        {
            email.SendTime = command.EmailStatus == EmailStatus.Success
                ? command.SendTime ?? DateTimeOffset.UtcNow
                : command.SendTime;
            email.RetryCount = command.RetryCount ?? email.RetryCount;
            email.ErrorMessage = Optional(command.ErrorMessage, 1000, nameof(command.ErrorMessage), "错误信息不能超过 1000 个字符。");
        }
        email.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? email.Remark;

        return new EmailCommandResult(await _emailRepository.UpdateAsync(email, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<SmsCommandResult> CreateOutboxSmsAsync(SmsCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateSmsInput(command.SmsType, command.ToPhone, command.Content, command.Provider, command.MaxRetryCount, command.BusinessType, command.BusinessId, command.Remark);
        var sms = new SysSms
        {
            SenderId = command.SenderId,
            ReceiverId = command.ReceiverId,
            SmsType = command.SmsType,
            ToPhone = Required(command.ToPhone, 50, nameof(command.ToPhone), "手机号不能超过 50 个字符。"),
            Content = Required(command.Content, 1000, nameof(command.Content), "短信内容不能超过 1000 个字符。"),
            TemplateCode = command.TemplateCode,
            TemplateParams = OptionalJson(command.TemplateParams, "短信模板参数必须是合法 JSON。"),
            Provider = Optional(command.Provider, 50, nameof(command.Provider), "短信服务商不能超过 50 个字符。"),
            SmsStatus = SmsStatus.Pending,
            ScheduledTime = command.ScheduledTime,
            MaxRetryCount = command.MaxRetryCount,
            BusinessType = Optional(command.BusinessType, 100, nameof(command.BusinessType), "业务类型不能超过 100 个字符。"),
            BusinessId = command.BusinessId,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        return new SmsCommandResult(await _smsRepository.AddAsync(sms, cancellationToken));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<SmsCommandResult> UpdateSmsAsync(SmsUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统短信主键必须大于 0。");
        ValidateSmsInput(command.SmsType, command.ToPhone, command.Content, command.Provider, command.MaxRetryCount, command.BusinessType, command.BusinessId, command.Remark);
        var sms = await GetSmsOrThrowAsync(command.BasicId, cancellationToken);
        EnsureMessageEditable(sms.SmsStatus, "短信");

        sms.ReceiverId = command.ReceiverId;
        sms.SmsType = command.SmsType;
        sms.ToPhone = Required(command.ToPhone, 50, nameof(command.ToPhone), "手机号不能超过 50 个字符。");
        sms.Content = Required(command.Content, 1000, nameof(command.Content), "短信内容不能超过 1000 个字符。");
        sms.TemplateCode = command.TemplateCode;
        sms.TemplateParams = OptionalJson(command.TemplateParams, "短信模板参数必须是合法 JSON。");
        sms.Provider = Optional(command.Provider, 50, nameof(command.Provider), "短信服务商不能超过 50 个字符。");
        sms.ScheduledTime = command.ScheduledTime;
        sms.MaxRetryCount = command.MaxRetryCount;
        sms.BusinessType = Optional(command.BusinessType, 100, nameof(command.BusinessType), "业务类型不能超过 100 个字符。");
        sms.BusinessId = command.BusinessId;
        sms.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new SmsCommandResult(await _smsRepository.UpdateAsync(sms, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<SmsCommandResult> UpdateSmsStatusAsync(SmsStatusUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统短信主键必须大于 0。");
        EnsureEnum(command.SmsStatus, nameof(command.SmsStatus));
        EnsureOptionalNonNegative(command.RetryCount, nameof(command.RetryCount), "重试次数不能小于 0。");
        EnsureOptionalNonNegative(command.Cost, nameof(command.Cost), "短信成本不能小于 0。");
        var sms = await GetSmsOrThrowAsync(command.BasicId, cancellationToken);

        sms.SmsStatus = command.SmsStatus;
        sms.SendTime = command.SmsStatus == SmsStatus.Success
            ? command.SendTime ?? DateTimeOffset.UtcNow
            : command.SendTime;
        sms.ProviderMessageId = Optional(command.ProviderMessageId, 100, nameof(command.ProviderMessageId), "服务商消息主键不能超过 100 个字符。");
        sms.RetryCount = command.RetryCount ?? sms.RetryCount;
        sms.Cost = command.Cost ?? sms.Cost;
        sms.ErrorMessage = Optional(command.ErrorMessage, 1000, nameof(command.ErrorMessage), "错误信息不能超过 1000 个字符。");
        sms.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? sms.Remark;

        return new SmsCommandResult(await _smsRepository.UpdateAsync(sms, cancellationToken));
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

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static void EnsureNonNegative(long value, string paramName, string message)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void EnsureOptionalId(long? id, string paramName, string message)
    {
        if (id is <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void EnsureOptionalNonNegative(long? value, string paramName, string message)
    {
        if (value is < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string? OptionalJson(string? value, string message)
    {
        var normalized = NormalizeNullable(value);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(message, exception);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }
}
