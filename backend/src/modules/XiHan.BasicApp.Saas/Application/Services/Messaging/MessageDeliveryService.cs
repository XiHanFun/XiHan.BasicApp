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

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Messaging;
using XiHan.BasicApp.Saas.Infrastructure.Messaging;
using XiHan.Framework.Messaging.Models;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 系统消息投递服务实现
/// </summary>
/// <remarks>
/// 统一异步：先渲染模板、落 SysEmail/SysSms 为 Pending，再把消息入业务层发件箱（Redis 延迟队列），由后台拉取发送，不阻塞调用接口。
/// 框架 Messaging 仅负责路由；发送编排（发件箱）在业务层（见 <see cref="DbMessageOutbox"/> + MessageOutboxHostedService）。
/// </remarks>
public sealed class MessageDeliveryService
    : IMessageDeliveryService
{
    private readonly IMessageDomainService _messageDomainService;

    private readonly DbMessageOutbox _outbox;

    private readonly IMessageTemplateRenderer _messageTemplateRenderer;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageDeliveryService(
        IMessageDomainService messageDomainService,
        DbMessageOutbox outbox,
        IMessageTemplateRenderer messageTemplateRenderer)
    {
        _messageDomainService = messageDomainService;
        _outbox = outbox;
        _messageTemplateRenderer = messageTemplateRenderer;
    }

    /// <inheritdoc />
    public async Task<EmailCommandResult> CreateEmailAsync(EmailCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var envelope = BuildEmailEnvelope(command);
        await RenderTemplateAsync(envelope);
        var renderedCommand = command with { Content = envelope.Content ?? command.Content };

        // 落库为 Pending，再入业务发件箱（事务提交后）由后台异步发送
        var result = await _messageDomainService.CreateOutboxEmailAsync(renderedCommand, cancellationToken);
        await _outbox.EnqueueAsync(SaasMessageChannelNames.Email, result.Email.BasicId, cancellationToken);
        return result;
    }

    /// <inheritdoc />
    public async Task<SmsCommandResult> CreateSmsAsync(SmsCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var envelope = BuildSmsEnvelope(command);
        await RenderTemplateAsync(envelope);
        var renderedCommand = command with { Content = envelope.Content ?? command.Content };

        // 落库为 Pending，再入业务发件箱（事务提交后）由后台异步发送
        var result = await _messageDomainService.CreateOutboxSmsAsync(renderedCommand, cancellationToken);
        await _outbox.EnqueueAsync(SaasMessageChannelNames.Sms, result.Sms.BasicId, cancellationToken);
        return result;
    }

    private static MessageEnvelope BuildEmailEnvelope(EmailCreateCommand command)
    {
        return new MessageEnvelope
        {
            Channel = SaasMessageChannelNames.Email,
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
            ]
        };
    }

    private static MessageEnvelope BuildSmsEnvelope(SmsCreateCommand command)
    {
        return new MessageEnvelope
        {
            Channel = SaasMessageChannelNames.Sms,
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
            ]
        };
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
    /// 信封渠道字符串映射为消息渠道枚举
    /// </summary>
    private static MessageChannel MapChannel(string channel)
    {
        return channel.ToLowerInvariant() switch
        {
            SaasMessageChannelNames.Email => MessageChannel.Email,
            SaasMessageChannelNames.Sms => MessageChannel.Sms,
            SaasMessageChannelNames.Bot => MessageChannel.Bot,
            _ => MessageChannel.SiteNotification
        };
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
}
