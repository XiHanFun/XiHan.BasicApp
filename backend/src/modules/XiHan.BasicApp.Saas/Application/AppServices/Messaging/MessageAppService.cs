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
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Messaging.Models;
using XiHan.Framework.Messaging.Options;
using XiHan.Framework.Templating.Services;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统消息命令应用服务
/// </summary>
/// <remarks>
/// 创建邮件/短信时通过框架 <see cref="IMessageDispatcher"/> 分发至对应通道发送器，
/// 由发送器负责实体持久化与外部发送。发件箱启用时则先落库再入队，由后台处理器异步发送。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统消息")]
public sealed class MessageAppService : SaasApplicationService, IMessageAppService
{
    private readonly IMessageDomainService _messageDomainService;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly IMessageOutbox _messageOutbox;
    private readonly IMessageRecordQueryService _messageQueryService;
    private readonly XiHanMessagingOptions _messagingOptions;
    private readonly ITemplateService _templateService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageAppService(
        IMessageDomainService messageDomainService,
        IMessageRecordQueryService messageQueryService,
        IMessageDispatcher messageDispatcher,
        IMessageOutbox messageOutbox,
        ITemplateService templateService,
        IOptions<XiHanMessagingOptions> messagingOptions)
        : base()
    {
        _messageDomainService = messageDomainService;
        _messageQueryService = messageQueryService;
        _messageDispatcher = messageDispatcher;
        _messageOutbox = messageOutbox;
        _templateService = templateService;
        _messagingOptions = messagingOptions.Value;
    }

    #region 系统邮件

    /// <summary>
    /// 创建系统邮件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Create)]
    public async Task<EmailDetailDto> CreateEmailAsync(EmailCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 构建消息信封
        var envelope = BuildEmailEnvelope(input);

        // 渲染模板内容（如有模板编码和参数）
        if (!string.IsNullOrWhiteSpace(envelope.TemplateCode) && envelope.TemplateParams?.Count > 0)
        {
            var variables = new Dictionary<string, object?>();
            foreach (var kvp in envelope.TemplateParams)
            {
                variables[kvp.Key] = kvp.Value;
            }

            try
            {
                envelope.Content = await _templateService.RenderAsync(envelope.TemplateCode, variables);
            }
            catch
            {
                // 渲染失败保留原始内容
            }
        }

        if (_messagingOptions.OutboxEnabled)
        {
            // 发件箱模式：先创建实体（Pending），入队后由后台异步发送
            var result = await _messageDomainService.CreateOutboxEmailAsync(ToCreateCommand(input, envelope.Content), cancellationToken);
            var savedEmail = result.Email;
            envelope.Metadata["EntityId"] = savedEmail.BasicId.ToString();
            await _messageOutbox.EnqueueAsync(envelope, cancellationToken);
            return MessageApplicationMapper.ToEmailDetailDto(savedEmail);
        }
        else
        {
            // 直接分发模式：通过调度器路由至 EmailMessageSender 创建实体并发送
            var results = await _messageDispatcher.DispatchAsync(envelope, cancellationToken);
            var result = results.FirstOrDefault()
                ?? throw new InvalidOperationException("邮件分发未返回结果。");

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"邮件分发失败: {result.ErrorMessage ?? "未知错误"}");
            }

            if (!long.TryParse(result.ProviderMessageId, out var emailId) || emailId <= 0)
            {
                throw new InvalidOperationException("邮件分发未返回有效记录 ID。");
            }

            var savedEmail = await _messageQueryService.GetEmailOrThrowAsync(emailId, cancellationToken);
            return MessageApplicationMapper.ToEmailDetailDto(savedEmail);
        }
    }

    /// <summary>
    /// 删除系统邮件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Delete)]
    public async Task DeleteEmailAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _messageDomainService.DeleteEmailAsync(id, cancellationToken);
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

        var result = await _messageDomainService.UpdateEmailAsync(ToUpdateCommand(input), cancellationToken);
        return MessageApplicationMapper.ToEmailDetailDto(result.Email);
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

        var result = await _messageDomainService.UpdateEmailStatusAsync(
            new EmailStatusUpdateCommand(input.BasicId, input.EmailStatus, input.SendTime, input.RetryCount, input.ErrorMessage, input.Remark),
            cancellationToken);
        return MessageApplicationMapper.ToEmailDetailDto(result.Email);
    }

    #endregion

    #region 系统短信

    /// <summary>
    /// 创建系统短信
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Create)]
    public async Task<SmsDetailDto> CreateSmsAsync(SmsCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 构建消息信封
        var envelope = BuildSmsEnvelope(input);

        // 渲染模板内容（如有模板编码和参数）
        if (!string.IsNullOrWhiteSpace(envelope.TemplateCode) && envelope.TemplateParams?.Count > 0)
        {
            var variables = new Dictionary<string, object?>();
            foreach (var kvp in envelope.TemplateParams)
            {
                variables[kvp.Key] = kvp.Value;
            }

            try
            {
                envelope.Content = await _templateService.RenderAsync(envelope.TemplateCode, variables);
            }
            catch
            {
                // 渲染失败保留原始内容
            }
        }

        if (_messagingOptions.OutboxEnabled)
        {
            // 发件箱模式：先创建实体（Pending），入队后由后台异步发送
            var result = await _messageDomainService.CreateOutboxSmsAsync(ToCreateCommand(input, envelope.Content), cancellationToken);
            var savedSms = result.Sms;
            envelope.Metadata["EntityId"] = savedSms.BasicId.ToString();
            await _messageOutbox.EnqueueAsync(envelope, cancellationToken);
            return MessageApplicationMapper.ToSmsDetailDto(savedSms);
        }
        else
        {
            // 直接分发模式：通过调度器路由至 SmsMessageSender 创建实体并发送
            var results = await _messageDispatcher.DispatchAsync(envelope, cancellationToken);
            var result = results.FirstOrDefault()
                ?? throw new InvalidOperationException("短信分发未返回结果。");

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"短信分发失败: {result.ErrorMessage ?? "未知错误"}");
            }

            if (!long.TryParse(result.ProviderMessageId, out var smsId) || smsId <= 0)
            {
                throw new InvalidOperationException("短信分发未返回有效记录 ID。");
            }

            var savedSms = await _messageQueryService.GetSmsOrThrowAsync(smsId, cancellationToken);
            return MessageApplicationMapper.ToSmsDetailDto(savedSms);
        }
    }

    /// <summary>
    /// 删除系统短信
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Delete)]
    public async Task DeleteSmsAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _messageDomainService.DeleteSmsAsync(id, cancellationToken);
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

        var result = await _messageDomainService.UpdateSmsAsync(ToUpdateCommand(input), cancellationToken);
        return MessageApplicationMapper.ToSmsDetailDto(result.Sms);
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

        var result = await _messageDomainService.UpdateSmsStatusAsync(
            new SmsStatusUpdateCommand(input.BasicId, input.SmsStatus, input.SendTime, input.ProviderMessageId, input.RetryCount, input.Cost, input.ErrorMessage, input.Remark),
            cancellationToken);
        return MessageApplicationMapper.ToSmsDetailDto(result.Sms);
    }

    #endregion

    #region 私有方法

    private static EmailCreateCommand ToCreateCommand(EmailCreateDto input, string? renderedContent)
    {
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
            renderedContent ?? input.Content,
            input.IsHtml,
            input.Attachments,
            input.TemplateId,
            input.TemplateParams,
            input.ScheduledTime,
            input.MaxRetryCount,
            input.BusinessType,
            input.BusinessId,
            input.Remark);
    }

    private static EmailUpdateCommand ToUpdateCommand(EmailUpdateDto input)
    {
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
            input.TemplateId,
            input.TemplateParams,
            input.ScheduledTime,
            input.MaxRetryCount,
            input.BusinessType,
            input.BusinessId,
            input.Remark);
    }

    private static SmsCreateCommand ToCreateCommand(SmsCreateDto input, string? renderedContent)
    {
        return new SmsCreateCommand(
            input.SenderId,
            input.ReceiverId,
            input.SmsType,
            input.ToPhone,
            renderedContent ?? input.Content,
            input.TemplateId,
            input.TemplateParams,
            input.Provider,
            input.ScheduledTime,
            input.MaxRetryCount,
            input.BusinessType,
            input.BusinessId,
            input.Remark);
    }

    private static SmsUpdateCommand ToUpdateCommand(SmsUpdateDto input)
    {
        return new SmsUpdateCommand(
            input.BasicId,
            input.ReceiverId,
            input.SmsType,
            input.ToPhone,
            input.Content,
            input.TemplateId,
            input.TemplateParams,
            input.Provider,
            input.ScheduledTime,
            input.MaxRetryCount,
            input.BusinessType,
            input.BusinessId,
            input.Remark);
    }

    /// <summary>
    /// 构建邮件消息信封
    /// </summary>
    private static MessageEnvelope BuildEmailEnvelope(EmailCreateDto input)
    {
        return new MessageEnvelope
        {
            Channel = "email",
            TenantId = null, // 由多租户中间件自动注入
            Subject = input.Subject ?? string.Empty,
            Content = input.Content,
            TemplateCode = input.TemplateId?.ToString(),
            TemplateParams = ParseTemplateParams(input.TemplateParams),
            ScheduledTime = input.ScheduledTime,
            Recipients =
            [
                new MessageRecipient
                {
                    Address = input.ToEmail,
                    DisplayName = input.ToEmail
                }
            ],
            Metadata = new Dictionary<string, string?>
            {
                ["FromEmail"] = input.FromEmail,
                ["FromName"] = input.FromName,
                ["IsHtml"] = input.IsHtml.ToString(),
                ["CcEmail"] = input.CcEmail,
                ["BccEmail"] = input.BccEmail,
                ["Attachments"] = input.Attachments,
                ["EmailType"] = input.EmailType.ToString(),
                ["SendUserId"] = input.SendUserId?.ToString(),
                ["ReceiveUserId"] = input.ReceiveUserId?.ToString(),
                ["TemplateId"] = input.TemplateId?.ToString(),
                ["TemplateParams"] = input.TemplateParams,
                ["MaxRetryCount"] = input.MaxRetryCount.ToString(),
                ["BusinessType"] = input.BusinessType,
                ["BusinessId"] = input.BusinessId?.ToString(),
                ["Remark"] = input.Remark
            }
        };
    }

    /// <summary>
    /// 构建短信消息信封
    /// </summary>
    private static MessageEnvelope BuildSmsEnvelope(SmsCreateDto input)
    {
        return new MessageEnvelope
        {
            Channel = "sms",
            TenantId = null, // 由多租户中间件自动注入
            Subject = string.Empty,
            Content = input.Content,
            TemplateCode = input.TemplateId?.ToString(),
            TemplateParams = ParseTemplateParams(input.TemplateParams),
            ScheduledTime = input.ScheduledTime,
            Recipients =
            [
                new MessageRecipient
                {
                    Address = input.ToPhone,
                    DisplayName = input.ToPhone
                }
            ],
            Metadata = new Dictionary<string, string?>
            {
                ["SmsType"] = input.SmsType.ToString(),
                ["Provider"] = input.Provider,
                ["SenderId"] = input.SenderId?.ToString(),
                ["ReceiverId"] = input.ReceiverId?.ToString(),
                ["TemplateId"] = input.TemplateId?.ToString(),
                ["TemplateParams"] = input.TemplateParams,
                ["MaxRetryCount"] = input.MaxRetryCount.ToString(),
                ["BusinessType"] = input.BusinessType,
                ["BusinessId"] = input.BusinessId?.ToString(),
                ["Remark"] = input.Remark
            }
        };
    }

    /// <summary>
    /// 解析模板参数字符串为字典
    /// </summary>
    private static Dictionary<string, string?> ParseTemplateParams(string? templateParamsJson)
    {
        if (string.IsNullOrWhiteSpace(templateParamsJson))
        {
            return [];
        }

        try
        {
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(templateParamsJson);
            return deserialized?.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value) ?? [];
        }
        catch
        {
            return [];
        }
    }

    #endregion
}
