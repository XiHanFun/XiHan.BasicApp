#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsMessageSender
// Guid:4b5c6d7e-8f9a-4b0c-1d2e-3f4a5b6c7d8e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Messaging.Models;
using XiHan.BasicApp.Saas.Application.Services;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// 短信消息发送器
/// </summary>
/// <remarks>
/// 支持两种模式：
/// - 直接发送：信封元数据无 EntityId，发送器新建 SysSms 记录并发送，通过 ProviderMessageId 返回记录 ID。
/// - 发件箱重放：信封元数据含 EntityId，发送器加载已有 SysSms 记录，更新状态并发送。
/// </remarks>
public sealed class SmsMessageSender : IMessageSender
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISmsGatewayResolver _gatewayResolver;
    private readonly ILogger<SmsMessageSender> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂（用于解析 Scoped 服务）</param>
    /// <param name="gatewayResolver">短信网关解析器（按 SysSmsConfig 默认配置热构建客户端）</param>
    /// <param name="logger">日志记录器</param>
    public SmsMessageSender(
        IServiceScopeFactory scopeFactory,
        ISmsGatewayResolver gatewayResolver,
        ILogger<SmsMessageSender> logger)
    {
        _scopeFactory = scopeFactory;
        _gatewayResolver = gatewayResolver;
        _logger = logger;
    }

    /// <summary>
    /// 是否支持指定通道
    /// </summary>
    /// <param name="channel">消息通道</param>
    /// <returns>短信通道返回 true</returns>
    public bool CanHandle(string channel)
    {
        return string.Equals(channel?.Trim(), "sms", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 发送短信消息
    /// </summary>
    /// <param name="envelope">消息信封</param>
    /// <param name="recipient">接收人</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    public async Task<MessageSendResult> SendAsync(MessageEnvelope envelope, MessageRecipient recipient, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(recipient);
        cancellationToken.ThrowIfCancellationRequested();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var clientResolver = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>();
        var client = clientResolver.GetCurrentClient();

        // 渲染模板内容（如有模板编码和参数）
        var content = await RenderContentAsync(envelope, scope, cancellationToken);

        // 从信封元数据提取配置
        var smsType = envelope.Metadata.TryGetValue("SmsType", out var smsTypeStr) && Enum.TryParse<SmsType>(smsTypeStr, out var smsTypeParsed) ? smsTypeParsed : SmsType.Notification;
        var provider = envelope.Metadata.TryGetValue("Provider", out var providerVal) ? providerVal : null;
        var businessType = envelope.Metadata.TryGetValue("BusinessType", out var businessTypeVal) ? businessTypeVal : null;
        var businessId = envelope.Metadata.TryGetValue("BusinessId", out var businessIdStr) && long.TryParse(businessIdStr, out var businessIdParsed) ? businessIdParsed : (long?)null;
        var remark = envelope.Metadata.TryGetValue("Remark", out var remarkVal) ? remarkVal : null;
        var maxRetryCount = envelope.Metadata.TryGetValue("MaxRetryCount", out var maxRetryStr) && int.TryParse(maxRetryStr, out var maxRetryParsed) ? maxRetryParsed : 3;
        var senderId = envelope.Metadata.TryGetValue("SenderId", out var senderIdStr) && long.TryParse(senderIdStr, out var senderIdParsed) ? senderIdParsed : (long?)null;
        var receiverId = envelope.Metadata.TryGetValue("ReceiverId", out var receiverIdStr) && long.TryParse(receiverIdStr, out var receiverIdParsed) ? receiverIdParsed : (long?)null;
        var templateCode = envelope.Metadata.TryGetValue("TemplateCode", out var templateCodeVal) && !string.IsNullOrWhiteSpace(templateCodeVal) ? templateCodeVal : envelope.TemplateCode;
        var templateParams = envelope.Metadata.TryGetValue("TemplateParams", out var templateParamsVal) ? templateParamsVal : null;

        // 获取或创建 SysSms 记录
        long entityId = 0;
        var hasEntityId = envelope.Metadata.TryGetValue("EntityId", out var entityIdStr) && long.TryParse(entityIdStr, out entityId) && entityId > 0;
        SysSms sms;
        if (hasEntityId)
        {
            // 发件箱重放模式：加载已有记录
            sms = await client.Queryable<SysSms>().InSingleAsync(entityId)
                ?? throw new InvalidOperationException($"系统短信记录不存在。EntityId: {entityId}");

            sms.SmsStatus = SmsStatus.Sending;
            sms.Content = content ?? sms.Content;
            await client.Updateable(sms)
                .UpdateColumns(s => new { s.SmsStatus, s.Content })
                .WhereColumns(s => s.BasicId)
                .ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            // 直接发送模式：新建记录
            sms = new SysSms
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                SmsType = smsType,
                ToPhone = recipient.Address,
                Content = content ?? string.Empty,
                TemplateCode = templateCode,
                TemplateParams = templateParams,
                Provider = provider,
                SmsStatus = SmsStatus.Sending,
                ScheduledTime = envelope.ScheduledTime,
                MaxRetryCount = maxRetryCount,
                BusinessType = businessType,
                BusinessId = businessId,
                Remark = remark
            };

            sms = await client.Insertable(sms).ExecuteReturnEntityAsync();
        }

        try
        {
            // fail-closed：无「默认且启用」网关配置即拒绝发送（记 Failed 走发件箱重试），杜绝假成功
            var gateway = await _gatewayResolver.ResolveAsync(cancellationToken)
                ?? throw new InvalidOperationException("未配置默认短信网关（SysSmsConfig），已拒绝发送。");

            // 发件箱重放模式的信封收件人是 "outbox" 占位符，手机号一律以行数据 ToPhone 为准（多号逗号分隔）
            var phones = (sms.ToPhone ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (phones.Length == 0)
            {
                throw new InvalidOperationException("短信记录缺少接收手机号。");
            }

            var gatewayResult = await gateway.SendAsync(
                new SmsGatewayRequest(phones, sms.TemplateCode, sms.TemplateParams, sms.Content),
                cancellationToken);
            if (!gatewayResult.IsSuccess)
            {
                throw new InvalidOperationException(gatewayResult.ErrorMessage ?? "短信网关返回失败。");
            }

            // 发送成功，更新状态并回写服务商与回执ID
            var providerName = gateway.Provider.ToString();
            var providerMessageId = gatewayResult.ProviderMessageId;
            await client.Updateable<SysSms>()
                .SetColumns(s => s.SmsStatus == SmsStatus.Success)
                .SetColumns(s => s.SendTime == DateTimeOffset.UtcNow)
                .SetColumns(s => s.Provider == providerName)
                .SetColumns(s => s.ProviderMessageId == providerMessageId)
                .Where(s => s.BasicId == sms.BasicId)
                .ExecuteCommandAsync(cancellationToken);

            _logger.LogInformation("短信发送成功。MessageId: {MessageId}, To: {To}, Provider: {Provider}, ProviderMessageId: {ProviderMessageId}",
                envelope.MessageId, sms.ToPhone, providerName, providerMessageId);

            return new MessageSendResult
            {
                MessageId = envelope.MessageId,
                Channel = envelope.Channel,
                RecipientAddress = recipient.Address,
                IsSuccess = true,
                ProviderMessageId = sms.BasicId.ToString(),
                DispatchedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            // 发送失败，记录错误
            await client.Updateable<SysSms>()
                .SetColumns(s => s.SmsStatus == SmsStatus.Failed)
                .SetColumns(s => s.ErrorMessage == ex.Message)
                .SetColumns(s => s.RetryCount == s.RetryCount + 1)
                .Where(s => s.BasicId == sms.BasicId)
                .ExecuteCommandAsync(cancellationToken);

            _logger.LogError(ex, "短信发送失败。MessageId: {MessageId}, To: {To}", envelope.MessageId, recipient.Address);

            return new MessageSendResult
            {
                MessageId = envelope.MessageId,
                Channel = envelope.Channel,
                RecipientAddress = recipient.Address,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ProviderMessageId = sms.BasicId.ToString(),
                DispatchedAt = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// 渲染模板内容：按 TemplateCode 从模板库查找（租户优先回退全局）并渲染，模板缺失/损坏回退原始内容
    /// </summary>
    private async Task<string?> RenderContentAsync(MessageEnvelope envelope, AsyncServiceScope scope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(envelope.TemplateCode))
        {
            return envelope.Content;
        }

        var renderer = scope.ServiceProvider.GetRequiredService<IMessageTemplateRenderer>();
        var variables = new Dictionary<string, object?>();
        foreach (var kvp in envelope.TemplateParams)
        {
            variables[kvp.Key] = kvp.Value;
        }

        var rendered = await renderer.RenderAsync(MessageChannel.Sms, envelope.TemplateCode, variables, cancellationToken);
        return rendered?.Content ?? envelope.Content;
    }
}
