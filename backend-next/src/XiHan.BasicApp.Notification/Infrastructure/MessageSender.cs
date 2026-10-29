// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 有新的发送记录时提前唤醒发送循环（单例）。记录可能还没提交，循环取不到就等下一轮。
/// </summary>
public sealed class MessageDispatchSignal : IDisposable
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    /// <summary>
    /// 唤醒。
    /// </summary>
    public void Pulse()
    {
        if (_signal.CurrentCount == 0)
        {
            try
            {
                _signal.Release();
            }
            catch (SemaphoreFullException)
            {
                // 已经有一次唤醒在等着
            }
        }
    }

    /// <summary>
    /// 等到被唤醒或超时。
    /// </summary>
    /// <param name="timeout">最长等多久。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>等待。</returns>
    public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => _signal.WaitAsync(timeout, cancellationToken);

    /// <inheritdoc />
    public void Dispose() => _signal.Dispose();
}

/// <summary>
/// <see cref="IMessageSender"/> 的实现：按模板编码取当前一侧（没有取平台）的模板渲染主题与正文，确认有可用的配置，写一条排队中的发送记录
/// （在调用的工作单元里，随业务一起提交），再唤醒发送循环。短信另存调用方给的参数（品牌名只用于本地渲染，不交给服务商，免得参数个数对不上）。
/// </summary>
/// <param name="templates">模板服务。</param>
/// <param name="configs">配置服务。</param>
/// <param name="emails">邮件记录仓储。</param>
/// <param name="sms">短信记录仓储。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="options">选项。</param>
/// <param name="signal">唤醒信号。</param>
public sealed class MessageSender(
    MessageTemplateService templates,
    ChannelConfigService configs,
    IRepository<SysEmail, Guid> emails,
    IRepository<SysSms, Guid> sms,
    IIdGenerator<Guid> ids,
    NotificationOptions options,
    MessageDispatchSignal signal) : IMessageSender
{
    private const string BrandParameter = "brand";

    /// <inheritdoc />
    public async ValueTask<bool> CanSendAsync(InvocationContext context, MessageKind kind, string templateCode)
    {
        ArgumentNullException.ThrowIfNull(context);
        var owner = SideRows.SideOf(context);
        var channel = kind == MessageKind.Email ? TemplateChannel.Email : TemplateChannel.Sms;
        var configured = kind == MessageKind.Email
            ? await configs.ResolveEmailAsync(owner, context.CancellationToken).ConfigureAwait(false) is not null
            : await configs.ResolveSmsAsync(owner, context.CancellationToken).ConfigureAwait(false) is not null;
        return configured && await templates.ResolveAsync(owner, channel, templateCode, context.CancellationToken).ConfigureAwait(false) is not null;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Guid>> EnqueueAsync(InvocationContext context, OutgoingMessage message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        var owner = SideRows.SideOf(context);
        var channel = message.Kind == MessageKind.Email ? TemplateChannel.Email : TemplateChannel.Sms;
        var template = await templates.ResolveAsync(owner, channel, message.TemplateCode, context.CancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return Error.Unavailable(NotificationErrors.TemplateMissing, NotificationMessages.TemplateMissing, MessageArguments.Of(("code", message.TemplateCode)));
        }

        var configured = message.Kind == MessageKind.Email
            ? await configs.ResolveEmailAsync(owner, context.CancellationToken).ConfigureAwait(false) is not null
            : await configs.ResolveSmsAsync(owner, context.CancellationToken).ConfigureAwait(false) is not null;
        if (!configured)
        {
            return Error.Unavailable(NotificationErrors.ChannelUnavailable, NotificationMessages.ChannelUnavailable, MessageArguments.Of(("kind", message.Kind.ToString())));
        }

        var parameters = message.Parameters.Any(static p => string.Equals(p.Key, BrandParameter, StringComparison.Ordinal))
            ? message.Parameters
            : [.. message.Parameters, new KeyValuePair<string, string?>(BrandParameter, options.Brand)];
        if (!TemplateRenderer.TryRender(template.Content, parameters, template.IsHtml, out var content, out var contentError))
        {
            return Invalid(contentError);
        }

        var sender = Guid.TryParse(context.GetSecurity().Principal.Id, out var caller) ? caller : (Guid?)null;
        var id = ids.NewId();
        Result added;
        if (message.Kind == MessageKind.Email)
        {
            if (!TemplateRenderer.TryRender(template.Subject, parameters, html: false, out var subject, out var subjectError))
            {
                return Invalid(subjectError);
            }

            added = await emails.AddAsync(new SysEmail
            {
                Id = id,
                OwnerTenantId = owner,
                SenderId = sender,
                ReceiverId = message.ReceiverId,
                ToEmail = message.To.Trim(),
                Subject = Clip(subject.Trim(), 512),
                Content = content,
                IsHtml = template.IsHtml,
                TemplateCode = template.Code,
                MaxAttempts = options.MaxAttempts,
                BusinessType = message.BusinessType,
                BusinessId = message.BusinessId,
            }, context.CancellationToken).ConfigureAwait(false);
        }
        else
        {
            var providerParameters = new JsonObject();
            foreach (var (name, value) in message.Parameters)
            {
                providerParameters[name] = value;
            }

            added = await sms.AddAsync(new SysSms
            {
                Id = id,
                OwnerTenantId = owner,
                SenderId = sender,
                ReceiverId = message.ReceiverId,
                ToPhone = message.To.Trim(),
                Content = Clip(content.Trim(), 1000),
                TemplateCode = template.Code,
                TemplateParams = providerParameters.ToJsonString(),
                MaxAttempts = options.MaxAttempts,
                BusinessType = message.BusinessType,
                BusinessId = message.BusinessId,
            }, context.CancellationToken).ConfigureAwait(false);
        }

        if (added.IsFailure)
        {
            return added.Error!;
        }

        signal.Pulse();
        return id;
    }

    private static Error Invalid(string? detail) => Error.Validation(NotificationErrors.TemplateInvalid, NotificationMessages.TemplateInvalid, MessageArguments.Of(("detail", detail ?? string.Empty)));

    private static string Clip(string value, int max) => value.Length <= max ? value : value[..max];
}
