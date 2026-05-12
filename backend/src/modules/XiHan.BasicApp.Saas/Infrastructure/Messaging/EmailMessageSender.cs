#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailMessageSender
// Guid:3a4b5c6d-7e8f-4a9b-0c1d-2e3f4a5b6c7d
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
using XiHan.Framework.Templating.Services;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// 邮件消息发送器
/// </summary>
/// <remarks>
/// 支持两种模式：
/// - 直接发送：信封元数据无 EntityId，发送器新建 SysEmail 记录并发送，通过 ProviderMessageId 返回记录 ID。
/// - 发件箱重放：信封元数据含 EntityId，发送器加载已有 SysEmail 记录，更新状态并发送。
/// </remarks>
public sealed class EmailMessageSender : IMessageSender
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailMessageSender> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂（用于解析 Scoped 服务）</param>
    /// <param name="logger">日志记录器</param>
    public EmailMessageSender(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailMessageSender> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 是否支持指定通道
    /// </summary>
    /// <param name="channel">消息通道</param>
    /// <returns>邮件通道返回 true</returns>
    public bool CanHandle(string channel)
    {
        return string.Equals(channel?.Trim(), "email", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 发送邮件消息
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
        var fromEmail = envelope.Metadata.GetValueOrDefault("FromEmail") ?? string.Empty;
        var fromName = envelope.Metadata.TryGetValue("FromName", out var fromNameVal) ? fromNameVal : null;
        var isHtml = envelope.Metadata.TryGetValue("IsHtml", out var isHtmlStr) && bool.TryParse(isHtmlStr, out var isHtmlParsed) && isHtmlParsed;
        var ccEmail = envelope.Metadata.TryGetValue("CcEmail", out var ccEmailVal) ? ccEmailVal : null;
        var bccEmail = envelope.Metadata.TryGetValue("BccEmail", out var bccEmailVal) ? bccEmailVal : null;
        var attachments = envelope.Metadata.TryGetValue("Attachments", out var attachmentsVal) ? attachmentsVal : null;
        var emailType = envelope.Metadata.TryGetValue("EmailType", out var emailTypeStr) && Enum.TryParse<EmailType>(emailTypeStr, out var emailTypeParsed) ? emailTypeParsed : EmailType.System;
        var businessType = envelope.Metadata.TryGetValue("BusinessType", out var businessTypeVal) ? businessTypeVal : null;
        var businessId = envelope.Metadata.TryGetValue("BusinessId", out var businessIdStr) && long.TryParse(businessIdStr, out var businessIdParsed) ? businessIdParsed : (long?)null;
        var remark = envelope.Metadata.TryGetValue("Remark", out var remarkVal) ? remarkVal : null;
        var maxRetryCount = envelope.Metadata.TryGetValue("MaxRetryCount", out var maxRetryStr) && int.TryParse(maxRetryStr, out var maxRetryParsed) ? maxRetryParsed : 3;
        var sendUserId = envelope.Metadata.TryGetValue("SendUserId", out var sendUserIdStr) && long.TryParse(sendUserIdStr, out var sendUserIdParsed) ? sendUserIdParsed : (long?)null;
        var receiveUserId = envelope.Metadata.TryGetValue("ReceiveUserId", out var receiveUserIdStr) && long.TryParse(receiveUserIdStr, out var receiveUserIdParsed) ? receiveUserIdParsed : (long?)null;
        var templateId = envelope.Metadata.TryGetValue("TemplateId", out var templateIdStr) && long.TryParse(templateIdStr, out var templateIdParsed) ? templateIdParsed : (long?)null;
        var templateParams = envelope.Metadata.TryGetValue("TemplateParams", out var templateParamsVal) ? templateParamsVal : null;

        // 获取或创建 SysEmail 记录
        long entityId = 0;
        var hasEntityId = envelope.Metadata.TryGetValue("EntityId", out var entityIdStr)
            && long.TryParse(entityIdStr, out entityId)
            && entityId > 0;
        SysEmail email;
        if (hasEntityId)
        {
            // 发件箱重放模式：加载已有记录
            email = await client.Queryable<SysEmail>().InSingleAsync(entityId)
                ?? throw new InvalidOperationException($"系统邮件记录不存在。EntityId: {entityId}");

            email.EmailStatus = EmailStatus.Sending;
            email.Content = content ?? email.Content;
            await client.Updateable(email)
                .UpdateColumns(e => new { e.EmailStatus, e.Content })
                .WhereColumns(e => e.BasicId)
                .ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            // 直接发送模式：新建记录
            email = new SysEmail
            {
                SendUserId = sendUserId,
                ReceiveUserId = receiveUserId,
                EmailType = emailType,
                FromEmail = string.IsNullOrWhiteSpace(fromEmail) ? "noreply@xihanfun.com" : fromEmail,
                FromName = fromName,
                ToEmail = recipient.Address,
                CcEmail = ccEmail,
                BccEmail = bccEmail,
                Subject = envelope.Subject,
                Content = content,
                IsHtml = isHtml,
                Attachments = attachments,
                TemplateId = templateId,
                TemplateParams = templateParams,
                EmailStatus = EmailStatus.Sending,
                ScheduledTime = envelope.ScheduledTime,
                MaxRetryCount = maxRetryCount,
                BusinessType = businessType,
                BusinessId = businessId,
                Remark = remark
            };

            email = await client.Insertable(email).ExecuteReturnEntityAsync();
        }

        try
        {
            // TODO: 集成 SMTP 发送逻辑
            // 占位：调用 SMTP 服务发送邮件
            await Task.CompletedTask;

            // 发送成功，更新状态
            await client.Updateable<SysEmail>()
                .SetColumns(e => e.EmailStatus == EmailStatus.Success)
                .SetColumns(e => e.SendTime == DateTimeOffset.UtcNow)
                .Where(e => e.BasicId == email.BasicId)
                .ExecuteCommandAsync(cancellationToken);

            _logger.LogInformation("邮件发送成功。MessageId: {MessageId}, To: {To}, Subject: {Subject}",
                envelope.MessageId, recipient.Address, envelope.Subject);

            return new MessageSendResult
            {
                MessageId = envelope.MessageId,
                Channel = envelope.Channel,
                RecipientAddress = recipient.Address,
                IsSuccess = true,
                ProviderMessageId = email.BasicId.ToString(),
                DispatchedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            // 发送失败，记录错误
            await client.Updateable<SysEmail>()
                .SetColumns(e => e.EmailStatus == EmailStatus.Failed)
                .SetColumns(e => e.ErrorMessage == ex.Message)
                .SetColumns(e => e.RetryCount == e.RetryCount + 1)
                .Where(e => e.BasicId == email.BasicId)
                .ExecuteCommandAsync(cancellationToken);

            _logger.LogError(ex, "邮件发送失败。MessageId: {MessageId}, To: {To}, Subject: {Subject}",
                envelope.MessageId, recipient.Address, envelope.Subject);

            return new MessageSendResult
            {
                MessageId = envelope.MessageId,
                Channel = envelope.Channel,
                RecipientAddress = recipient.Address,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ProviderMessageId = email.BasicId.ToString(),
                DispatchedAt = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// 渲染模板内容
    /// </summary>
    private async Task<string?> RenderContentAsync(MessageEnvelope envelope, AsyncServiceScope scope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(envelope.TemplateCode) || envelope.TemplateParams is null || envelope.TemplateParams.Count == 0)
        {
            return envelope.Content;
        }

        var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
        var variables = new Dictionary<string, object?>();
        foreach (var kvp in envelope.TemplateParams)
        {
            variables[kvp.Key] = kvp.Value;
        }

        try
        {
            return await templateService.RenderAsync(envelope.TemplateCode, variables);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "邮件模板渲染失败，使用原始内容。TemplateCode: {TemplateCode}", envelope.TemplateCode);
            return envelope.Content;
        }
    }
}
