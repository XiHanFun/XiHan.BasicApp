#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationFanoutService
// Guid:6e0f3a92-b8d1-4c57-9a24-71fce5d08b3a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Messaging;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Bot.Email.Abstractions;
using XiHan.Framework.Bot.Email.Options;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Messaging.Models;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 系统通知多渠道扇出服务实现
/// </summary>
/// <remarks>
/// 编排语义（队列承载工作，发件箱天然削峰）：
/// - 邮箱位：域服务解析收件人（偏好门控 Email 渠道）→ 取 Email 非空用户 → 通知级渲染模板一次（notification-email，
///   变量均为通知级，避免逐行重复渲染）→ 逐用户 <see cref="IMessageDeliveryService.CreateEmailAsync"/> 落行走发件箱；
///   模板缺失回退通知纯内容（IsHtml 按通知正文格式）。
/// - 短信位：偏好门控 Sms 渠道 → 取 Phone 非空且已验证（SysUserSecurity.PhoneVerified）用户 →
///   逐用户 <see cref="IMessageDeliveryService.CreateSmsAsync"/>；行保留内部模板码 notification-sms，
///   云厂商发送须在短信配置 TemplateMap 登记该编码到服务商模板码的映射，未映射将按 Content 纯文本尝试发送。
/// - 机器人位：通知级广播（机器人无用户绑定，用户偏好 Bot 开关留作未来按用户投递的门）：
///   UoW 提交后（照 DbMessageOutbox.OnCompleted 先例）经 <see cref="IMessageDispatcher"/> 以 bot 通道直发，
///   失败仅记日志，不回滚已提交的通知发布。
/// 邮箱/短信落行发生在发布事务内，事务回滚则扇出行一并回滚；发送本身由发件箱异步承载，不阻塞发布接口。
/// </remarks>
public sealed class NotificationFanoutService
    : INotificationFanoutService
{
    /// <summary>
    /// 默认品牌名（邮件配置缺失/未配发件人显示名时回退，与验证码链路一致）
    /// </summary>
    private const string DefaultBrand = "XiHan BasicApp";

    /// <summary>
    /// 机器人广播占位收件地址（bot 通道无收件地址语义，仅用于信封结构完整）
    /// </summary>
    private const string BotBroadcastRecipientAddress = "broadcast";

    /// <summary>
    /// 邮件/短信发送最大重试次数（与验证码等既有投递链路一致）
    /// </summary>
    private const int FanoutMaxRetryCount = 3;

    private readonly INotificationDomainService _notificationDomainService;

    private readonly IMessageDeliveryService _messageDeliveryService;

    private readonly IMessageTemplateRenderer _messageTemplateRenderer;

    private readonly IMessageDispatcher _messageDispatcher;

    private readonly IEmailConfigStore _emailConfigStore;

    private readonly IUserRepository _userRepository;

    private readonly IUserSecurityRepository _userSecurityRepository;

    private readonly IUnitOfWorkManager _unitOfWorkManager;

    private readonly ILogger<NotificationFanoutService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationFanoutService(
        INotificationDomainService notificationDomainService,
        IMessageDeliveryService messageDeliveryService,
        IMessageTemplateRenderer messageTemplateRenderer,
        IMessageDispatcher messageDispatcher,
        IEmailConfigStore emailConfigStore,
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<NotificationFanoutService> logger)
    {
        _notificationDomainService = notificationDomainService;
        _messageDeliveryService = messageDeliveryService;
        _messageTemplateRenderer = messageTemplateRenderer;
        _messageDispatcher = messageDispatcher;
        _emailConfigStore = emailConfigStore;
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task FanoutAsync(SysNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        if (!notification.IsPublished)
        {
            throw new InvalidOperationException("通知尚未发布，不能执行多渠道扇出。");
        }

        var channels = notification.DeliveryChannels;
        var needEmail = channels.HasFlag(MessageChannel.Email);
        var needSms = channels.HasFlag(MessageChannel.Sms);
        var needBot = channels.HasFlag(MessageChannel.Bot);
        if (!needEmail && !needSms && !needBot)
        {
            return;
        }

        var emailCount = 0;
        var smsCount = 0;
        if (needEmail || needSms)
        {
            var recipients = await _notificationDomainService.ResolveChannelRecipientsAsync(notification, cancellationToken);
            var emailConfig = await _emailConfigStore.GetAsync(cancellationToken);
            var brand = ResolveBrand(emailConfig);
            if (needEmail && recipients.EmailUserIds.Count > 0)
            {
                emailCount = await FanoutEmailAsync(notification, recipients.EmailUserIds, emailConfig, brand, cancellationToken);
            }

            if (needSms && recipients.SmsUserIds.Count > 0)
            {
                smsCount = await FanoutSmsAsync(notification, recipients.SmsUserIds, brand, cancellationToken);
            }
        }

        if (needBot)
        {
            await ScheduleBotBroadcastAsync(notification);
        }

        // 量级留痕：扇出规模记 Info（发件箱异步消费天然削峰，不做分页批处理）
        _logger.LogInformation(
            "通知多渠道扇出完成：NotificationId={NotificationId}, Channels={Channels}, Email={EmailCount}, Sms={SmsCount}, Bot={Bot}",
            notification.BasicId, channels, emailCount, smsCount, needBot);
    }

    /// <summary>
    /// 解析品牌名（发件人显示名 FromName，未配置回退默认品牌；短信/邮件共用）
    /// </summary>
    private static string ResolveBrand(EmailOptions? emailConfig)
    {
        var fromName = emailConfig?.From.FromName;
        return string.IsNullOrWhiteSpace(fromName) ? DefaultBrand : fromName;
    }

    /// <summary>
    /// 邮箱位扇出：取 Email 非空用户，通知级渲染一次后逐用户落 SysEmail 行（发件箱异步发送）
    /// </summary>
    /// <returns>实际落行的邮件数</returns>
    private async Task<int> FanoutEmailAsync(
        SysNotification notification,
        IReadOnlyCollection<long> userIds,
        EmailOptions? emailConfig,
        string brand,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetListAsync(user => userIds.Contains(user.BasicId), cancellationToken);
        var targets = users.Where(user => !string.IsNullOrWhiteSpace(user.Email)).ToList();
        if (targets.Count == 0)
        {
            return 0;
        }

        // 通知级渲染一次（title/content/brand 均为通知级变量，无用户维度），逐用户仅落行，避免逐行重复渲染
        var variables = new Dictionary<string, object?>
        {
            ["title"] = notification.Title,
            ["content"] = notification.Content ?? string.Empty,
            ["brand"] = brand,
        };
        var rendered = await _messageTemplateRenderer.RenderAsync(
            MessageChannel.Email, SaasMessageTemplateCodes.Notification.Email, variables, cancellationToken);

        string subject;
        string content;
        bool isHtml;
        if (rendered is not null)
        {
            subject = string.IsNullOrWhiteSpace(rendered.Subject) ? notification.Title : rendered.Subject;
            content = rendered.Content;
            isHtml = rendered.IsHtml;
        }
        else
        {
            // 模板缺失/停用回退：主题=通知标题、正文=通知纯内容，IsHtml 按通知正文格式
            subject = notification.Title;
            content = notification.Content ?? notification.Title;
            isHtml = notification.ContentFormat == NotificationContentFormat.Html;
        }

        foreach (var user in targets)
        {
            _ = await _messageDeliveryService.CreateEmailAsync(
                new EmailCreateCommand(
                    SendUserId: notification.SendUserId,
                    ReceiveUserId: user.BasicId,
                    EmailType: EmailType.Notification,
                    FromEmail: emailConfig?.From.FromMail ?? string.Empty,
                    FromName: emailConfig?.From.FromName,
                    ToEmail: user.Email!,
                    CcEmail: null,
                    BccEmail: null,
                    Subject: subject,
                    Content: content,
                    IsHtml: isHtml,
                    Attachments: null,
                    // 内容已通知级渲染完毕，不再携带模板码，避免投递服务逐行重复渲染
                    TemplateCode: null,
                    TemplateParams: null,
                    ScheduledTime: null,
                    MaxRetryCount: FanoutMaxRetryCount,
                    BusinessType: SaasMessageBusinessTypes.Notification,
                    BusinessId: notification.BasicId,
                    Remark: null),
                cancellationToken);
        }

        return targets.Count;
    }

    /// <summary>
    /// 短信位扇出：取 Phone 非空且已验证用户，逐用户落 SysSms 行（发件箱异步发送）
    /// </summary>
    /// <remarks>
    /// 行保留内部模板码 <see cref="SaasMessageTemplateCodes.Notification.Sms"/>：云厂商网关按短信配置
    /// TemplateMap 将其映射为服务商模板码 + TemplateParams 变量发送；Content 为渲染兜底纯文本。
    /// </remarks>
    /// <returns>实际落行的短信数</returns>
    private async Task<int> FanoutSmsAsync(
        SysNotification notification,
        IReadOnlyCollection<long> userIds,
        string brand,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetListAsync(user => userIds.Contains(user.BasicId), cancellationToken);
        var phoneUsers = users.Where(user => !string.IsNullOrWhiteSpace(user.Phone)).ToList();
        if (phoneUsers.Count == 0)
        {
            return 0;
        }

        // 仅向已验证手机号投递（未验证号码不可信，fail-closed 跳过）
        var phoneUserIds = phoneUsers.Select(user => user.BasicId).ToArray();
        var securities = await _userSecurityRepository.GetListAsync(
            security => phoneUserIds.Contains(security.UserId), cancellationToken);
        var verifiedUserIds = securities
            .Where(security => security.PhoneVerified)
            .Select(security => security.UserId)
            .ToHashSet();
        var targets = phoneUsers.Where(user => verifiedUserIds.Contains(user.BasicId)).ToList();
        if (targets.Count == 0)
        {
            return 0;
        }

        var templateParams = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["title"] = notification.Title,
            ["brand"] = brand,
        });
        // 纯文本兜底内容（模板缺失/损坏时使用），文案与种子模板一致
        var fallbackContent = $"【{brand}】您有新的通知：{notification.Title}，详情请登录查看。";

        foreach (var user in targets)
        {
            _ = await _messageDeliveryService.CreateSmsAsync(
                new SmsCreateCommand(
                    SenderId: notification.SendUserId,
                    ReceiverId: user.BasicId,
                    SmsType: SmsType.Notification,
                    ToPhone: user.Phone!,
                    Content: fallbackContent,
                    TemplateCode: SaasMessageTemplateCodes.Notification.Sms,
                    TemplateParams: templateParams,
                    Provider: null,
                    ScheduledTime: null,
                    MaxRetryCount: FanoutMaxRetryCount,
                    BusinessType: SaasMessageBusinessTypes.Notification,
                    BusinessId: notification.BasicId,
                    Remark: null),
                cancellationToken);
        }

        return targets.Count;
    }

    /// <summary>
    /// 机器人位：通知级广播（标题+内容），UoW 提交后经 bot 通道直发；无环境 UoW 时立即直发
    /// </summary>
    private Task ScheduleBotBroadcastAsync(SysNotification notification)
    {
        var envelope = new MessageEnvelope
        {
            Channel = SaasMessageChannelNames.Bot,
            Subject = notification.Title,
            Content = notification.Content ?? notification.Title,
            Recipients =
            [
                new MessageRecipient
                {
                    Address = BotBroadcastRecipientAddress,
                    DisplayName = BotBroadcastRecipientAddress
                }
            ]
        };

        var uow = _unitOfWorkManager.Current;
        if (uow is not null)
        {
            // 事务提交后才广播（照 DbMessageOutbox.OnCompleted 先例）：确保外部可见时通知已落库；
            // 失败仅记日志，不回滚已提交的通知发布（IMessageDispatcher 为 Singleton，闭包捕获安全）
            uow.OnCompleted(() => DispatchBotAsync(envelope, notification.BasicId));
            return Task.CompletedTask;
        }

        // 无环境 UoW：直接投递（DispatchBotAsync 内部吞异常记日志）
        return DispatchBotAsync(envelope, notification.BasicId);
    }

    /// <summary>
    /// 执行机器人广播投递：任何失败只记日志，不向上抛出
    /// </summary>
    private async Task DispatchBotAsync(MessageEnvelope envelope, long notificationId)
    {
        try
        {
            var results = await _messageDispatcher.DispatchAsync(envelope);
            var errors = results
                .Where(result => !result.IsSuccess)
                .Select(result => result.ErrorMessage)
                .Where(error => !string.IsNullOrWhiteSpace(error))
                .ToArray();
            if (errors.Length > 0)
            {
                _logger.LogWarning(
                    "通知机器人广播失败（不影响通知发布）：NotificationId={NotificationId}, 失败明细：{Errors}",
                    notificationId, string.Join("; ", errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "通知机器人广播异常（不影响通知发布）：NotificationId={NotificationId}", notificationId);
        }
    }
}
