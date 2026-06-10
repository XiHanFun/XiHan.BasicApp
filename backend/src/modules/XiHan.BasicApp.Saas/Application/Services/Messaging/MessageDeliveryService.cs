#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageDeliveryService
// Guid:4e68012f-64da-4bb4-8df8-ad4ef0e92cd3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Options;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Messaging.Models;
using XiHan.Framework.Messaging.Options;


namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 系统消息投递服务实现
/// </summary>
public sealed class MessageDeliveryService
    : IMessageDeliveryService
{
    private readonly IMessageDispatcher _messageDispatcher;

    private readonly IMessageDomainService _messageDomainService;

    private readonly IMessageOutbox _messageOutbox;

    private readonly IMessageRecordQueryService _messageQueryService;

    private readonly XiHanMessagingOptions _messagingOptions;

    private readonly IMessageTemplateRenderer _messageTemplateRenderer;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageDeliveryService(
        IMessageDomainService messageDomainService,
        IMessageRecordQueryService messageQueryService,
        IMessageDispatcher messageDispatcher,
        IMessageOutbox messageOutbox,
        IMessageTemplateRenderer messageTemplateRenderer,
        IOptions<XiHanMessagingOptions> messagingOptions)
    {
        _messageDomainService = messageDomainService;
        _messageQueryService = messageQueryService;
        _messageDispatcher = messageDispatcher;
        _messageOutbox = messageOutbox;
        _messageTemplateRenderer = messageTemplateRenderer;
        _messagingOptions = messagingOptions.Value;
    }

    /// <inheritdoc />
    public async Task<EmailCommandResult> CreateEmailAsync(EmailCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var envelope = BuildEmailEnvelope(command);
        await RenderTemplateAsync(envelope);
        var renderedCommand = command with { Content = envelope.Content ?? command.Content };

        if (_messagingOptions.OutboxEnabled)
        {
            var result = await _messageDomainService.CreateOutboxEmailAsync(renderedCommand, cancellationToken);
            envelope.Metadata["EntityId"] = result.Email.BasicId.ToString();
            await _messageOutbox.EnqueueAsync(envelope, cancellationToken);
            return result;
        }

        var dispatchResult = await DispatchOneAsync(envelope, "邮件", cancellationToken);
        if (!long.TryParse(dispatchResult.ProviderMessageId, out var emailId) || emailId <= 0)
        {
            throw new InvalidOperationException("邮件分发未返回有效记录 ID。");
        }

        var savedEmail = await _messageQueryService.GetEmailOrThrowAsync(emailId, cancellationToken);
        return new EmailCommandResult(savedEmail);
    }

    /// <inheritdoc />
    public async Task<SmsCommandResult> CreateSmsAsync(SmsCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var envelope = BuildSmsEnvelope(command);
        await RenderTemplateAsync(envelope);
        var renderedCommand = command with { Content = envelope.Content ?? command.Content };

        if (_messagingOptions.OutboxEnabled)
        {
            var result = await _messageDomainService.CreateOutboxSmsAsync(renderedCommand, cancellationToken);
            envelope.Metadata["EntityId"] = result.Sms.BasicId.ToString();
            await _messageOutbox.EnqueueAsync(envelope, cancellationToken);
            return result;
        }

        var dispatchResult = await DispatchOneAsync(envelope, "短信", cancellationToken);
        if (!long.TryParse(dispatchResult.ProviderMessageId, out var smsId) || smsId <= 0)
        {
            throw new InvalidOperationException("短信分发未返回有效记录 ID。");
        }

        var savedSms = await _messageQueryService.GetSmsOrThrowAsync(smsId, cancellationToken);
        return new SmsCommandResult(savedSms);
    }

    private static MessageEnvelope BuildEmailEnvelope(EmailCreateCommand command)
    {
        return new MessageEnvelope
        {
            Channel = "email",
            TenantId = null,
            Subject = command.Subject ?? string.Empty,
            Content = command.Content,
            TemplateCode = command.TemplateCode,
            TemplateParams = ParseTemplateParams(command.TemplateParams),
            ScheduledTime = command.ScheduledTime,
            Recipients =
            [
                new MessageRecipient
                {
                    Address = command.ToEmail,
                    DisplayName = command.ToEmail
                }
            ],
            Metadata = new Dictionary<string, string?>
            {
                ["FromEmail"] = command.FromEmail,
                ["FromName"] = command.FromName,
                ["IsHtml"] = command.IsHtml.ToString(),
                ["CcEmail"] = command.CcEmail,
                ["BccEmail"] = command.BccEmail,
                ["Attachments"] = command.Attachments,
                ["EmailType"] = command.EmailType.ToString(),
                ["SendUserId"] = command.SendUserId?.ToString(),
                ["ReceiveUserId"] = command.ReceiveUserId?.ToString(),
                ["TemplateCode"] = command.TemplateCode,
                ["TemplateParams"] = command.TemplateParams,
                ["MaxRetryCount"] = command.MaxRetryCount.ToString(),
                ["BusinessType"] = command.BusinessType,
                ["BusinessId"] = command.BusinessId?.ToString(),
                ["Remark"] = command.Remark
            }
        };
    }

    private static MessageEnvelope BuildSmsEnvelope(SmsCreateCommand command)
    {
        return new MessageEnvelope
        {
            Channel = "sms",
            TenantId = null,
            Subject = string.Empty,
            Content = command.Content,
            TemplateCode = command.TemplateCode,
            TemplateParams = ParseTemplateParams(command.TemplateParams),
            ScheduledTime = command.ScheduledTime,
            Recipients =
            [
                new MessageRecipient
                {
                    Address = command.ToPhone,
                    DisplayName = command.ToPhone
                }
            ],
            Metadata = new Dictionary<string, string?>
            {
                ["SmsType"] = command.SmsType.ToString(),
                ["Provider"] = command.Provider,
                ["SenderId"] = command.SenderId?.ToString(),
                ["ReceiverId"] = command.ReceiverId?.ToString(),
                ["TemplateCode"] = command.TemplateCode,
                ["TemplateParams"] = command.TemplateParams,
                ["MaxRetryCount"] = command.MaxRetryCount.ToString(),
                ["BusinessType"] = command.BusinessType,
                ["BusinessId"] = command.BusinessId?.ToString(),
                ["Remark"] = command.Remark
            }
        };
    }

    private async Task<MessageSendResult> DispatchOneAsync(
        MessageEnvelope envelope,
        string messageName,
        CancellationToken cancellationToken)
    {
        var results = await _messageDispatcher.DispatchAsync(envelope, cancellationToken);
        var result = results.FirstOrDefault()
            ?? throw new InvalidOperationException($"{messageName}分发未返回结果。");

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"{messageName}分发失败: {result.ErrorMessage ?? "未知错误"}");
        }

        return result;
    }

    private static Dictionary<string, string?> ParseTemplateParams(string? templateParamsJson)
    {
        if (string.IsNullOrWhiteSpace(templateParamsJson))
        {
            return [];
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(templateParamsJson);
            return deserialized?.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// 按 TemplateCode 从模板库查找并渲染信封内容（租户模板优先回退全局；模板缺失/损坏保留原始内容）。
    /// </summary>
    private async Task RenderTemplateAsync(MessageEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.TemplateCode))
        {
            return;
        }

        var channel = MapChannel(envelope.Channel);
        var variables = envelope.TemplateParams.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
        var rendered = await _messageTemplateRenderer.RenderAsync(channel, envelope.TemplateCode, variables);
        if (rendered is null)
        {
            // 模板不存在/停用/渲染失败：保留原始内容，由发送链路继续处理
            return;
        }

        envelope.Content = rendered.Content;
        if (!string.IsNullOrWhiteSpace(rendered.Subject))
        {
            envelope.Subject = rendered.Subject;
        }
    }

    /// <summary>
    /// 信封渠道字符串映射为消息渠道枚举
    /// </summary>
    private static MessageChannel MapChannel(string channel)
    {
        return channel.ToLowerInvariant() switch
        {
            "email" => MessageChannel.Email,
            "sms" => MessageChannel.Sms,
            _ => MessageChannel.SiteNotification
        };
    }
}
