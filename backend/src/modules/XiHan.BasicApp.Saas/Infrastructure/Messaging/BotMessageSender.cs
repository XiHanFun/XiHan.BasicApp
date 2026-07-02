#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BotMessageSender
// Guid:ab2d5178-4cfd-4423-82b1-5d6b1fcc69da
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Messaging;
using XiHan.Framework.Bot.Clients;
using XiHan.Framework.Bot.Enums;
using XiHan.Framework.Bot.Models;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Messaging.Models;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// 机器人消息发送器
/// </summary>
/// <remarks>
/// 仅支持直发模式：渲染模板（缺失回退信封内容）后经框架 <see cref="IBotClient"/> 投递
/// （完整经过框架管道：环境过滤/限流/重试/日志 + 广播等策略）。
/// - 不落 SysEmail/SysSms 类业务行，无发件箱重放语义：信封元数据含 EntityId 时视为误路由，直接抛出。
/// - 渠道选择：信封元数据 <c>Channels</c>（逗号分隔的框架渠道/提供者名）有值则定向投递，否则广播全部提供者。
/// - 结果映射：<see cref="BotDispatchResult.IsSuccess"/> 为 false（无提供者/被跳过/任一失败）
///   → <see cref="MessageSendResult.IsSuccess"/> 为 false（fail-closed）。
/// </remarks>
public sealed class BotMessageSender : IMessageSender
{
    /// <summary>
    /// 信封元数据键：定向投递的框架渠道/提供者名列表（逗号分隔）；缺省广播全部提供者
    /// </summary>
    public const string ChannelsMetadataKey = "Channels";

    /// <summary>
    /// 信封元数据键：发件箱重放实体主键（bot 通道不支持，出现即抛出）
    /// </summary>
    public const string EntityIdMetadataKey = "EntityId";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBotClient _botClient;
    private readonly ILogger<BotMessageSender> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂（用于解析 Scoped 的模板渲染服务）</param>
    /// <param name="botClient">框架 Bot 客户端（Singleton，经调度器走完整管道并回传聚合结果）</param>
    /// <param name="logger">日志记录器</param>
    public BotMessageSender(
        IServiceScopeFactory scopeFactory,
        IBotClient botClient,
        ILogger<BotMessageSender> logger)
    {
        _scopeFactory = scopeFactory;
        _botClient = botClient;
        _logger = logger;
    }

    /// <summary>
    /// 是否支持指定通道
    /// </summary>
    /// <param name="channel">消息通道</param>
    /// <returns>机器人通道返回 true</returns>
    public bool CanHandle(string channel)
    {
        return string.Equals(channel?.Trim(), SaasMessageChannelNames.Bot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 发送机器人消息（仅直发，不支持发件箱重放）
    /// </summary>
    /// <param name="envelope">消息信封</param>
    /// <param name="recipient">接收人（bot 通道无收件地址语义，仅回填结果）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    public async Task<MessageSendResult> SendAsync(MessageEnvelope envelope, MessageRecipient recipient, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(recipient);
        cancellationToken.ThrowIfCancellationRequested();

        // bot 通道无业务行事实源，发件箱重放（EntityId）无从加载：视为误路由，fail-closed 直接抛出
        if (envelope.Metadata.TryGetValue(EntityIdMetadataKey, out var entityIdStr)
            && long.TryParse(entityIdStr, out var entityId)
            && entityId > 0)
        {
            throw new InvalidOperationException("bot 通道不支持发件箱重放。");
        }

        // 渲染模板内容（租户优先回退全局；模板缺失/损坏回退信封内容）
        var (subject, content) = await RenderContentAsync(envelope, cancellationToken);

        var botMessage = new BotMessage
        {
            Title = string.IsNullOrWhiteSpace(subject) ? null : subject,
            Content = content ?? string.Empty,
            Type = BotMessageType.Text
        };

        // 定向渠道：元数据 Channels（逗号分隔）有值则按框架渠道/提供者名投递，否则广播全部提供者
        var channels = ResolveTargetChannels(envelope);
        BotDispatchResult dispatchResult;
        try
        {
            dispatchResult = channels.Count == 0
                ? await _botClient.SendAsync(botMessage)
                : await _botClient.SendAsync(botMessage, [.. channels]);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 框架 ThrowWhenNoProvider=true 或重试耗尽后的最终异常
            _logger.LogError(ex, "机器人消息调度异常。MessageId: {MessageId}", envelope.MessageId);
            return BuildResult(envelope, recipient, isSuccess: false, ex.Message);
        }

        if (!dispatchResult.IsSuccess)
        {
            _logger.LogWarning("机器人消息发送失败。MessageId: {MessageId}, 失败明细: {Error}",
                envelope.MessageId, dispatchResult.ErrorMessage);
            return BuildResult(envelope, recipient, isSuccess: false,
                dispatchResult.ErrorMessage ?? "机器人消息发送失败。");
        }

        var providers = string.Join(",", dispatchResult.Results.Select(result => result.Provider));
        _logger.LogInformation("机器人消息发送成功。MessageId: {MessageId}, Providers: {Providers}",
            envelope.MessageId, providers);

        return BuildResult(envelope, recipient, isSuccess: true, errorMessage: null, providerMessageId: providers);
    }

    /// <summary>
    /// 解析定向投递渠道：元数据 Channels（逗号分隔）→ 去空白拆分；缺省空列表 = 广播全部提供者
    /// </summary>
    private static IReadOnlyList<string> ResolveTargetChannels(MessageEnvelope envelope)
    {
        if (!envelope.Metadata.TryGetValue(ChannelsMetadataKey, out var channelsValue)
            || string.IsNullOrWhiteSpace(channelsValue))
        {
            return [];
        }

        return channelsValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// 构建统一发送结果
    /// </summary>
    private static MessageSendResult BuildResult(
        MessageEnvelope envelope,
        MessageRecipient recipient,
        bool isSuccess,
        string? errorMessage,
        string? providerMessageId = null)
    {
        return new MessageSendResult
        {
            MessageId = envelope.MessageId,
            Channel = envelope.Channel,
            RecipientAddress = recipient.Address,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            ProviderMessageId = providerMessageId,
            DispatchedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 渲染模板内容：按 TemplateCode 从模板库查找（租户优先回退全局）并渲染，模板缺失/损坏回退信封主题与内容
    /// </summary>
    private async Task<(string? Subject, string? Content)> RenderContentAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        var templateCode = envelope.Metadata.TryGetValue("TemplateCode", out var templateCodeVal) && !string.IsNullOrWhiteSpace(templateCodeVal)
            ? templateCodeVal
            : envelope.TemplateCode;
        if (string.IsNullOrWhiteSpace(templateCode))
        {
            return (envelope.Subject, envelope.Content);
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var renderer = scope.ServiceProvider.GetRequiredService<IMessageTemplateRenderer>();
        var variables = new Dictionary<string, object?>();
        foreach (var kvp in envelope.TemplateParams)
        {
            variables[kvp.Key] = kvp.Value;
        }

        var rendered = await renderer.RenderAsync(MessageChannel.Bot, templateCode, variables, cancellationToken);
        if (rendered is null)
        {
            return (envelope.Subject, envelope.Content);
        }

        return (
            string.IsNullOrWhiteSpace(rendered.Subject) ? envelope.Subject : rendered.Subject,
            rendered.Content);
    }
}
