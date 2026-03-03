#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageAppService
// Guid:77d1f0f7-65ab-4d73-b005-2ef9f32d7545
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:36:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.UseCases.Commands;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Rbac.Application.AppServices.Implementations;

/// <summary>
/// 统一消息服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class MessageAppService : ApplicationServiceBase, IMessageAppService
{
    private readonly INotificationAppService _notificationAppService;
    private readonly IEmailAppService _emailAppService;
    private readonly ISmsAppService _smsAppService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly ISmsRepository _smsRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageAppService(
        INotificationAppService notificationAppService,
        IEmailAppService emailAppService,
        ISmsAppService smsAppService,
        IUserRepository userRepository,
        IEmailRepository emailRepository,
        ISmsRepository smsRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _notificationAppService = notificationAppService;
        _emailAppService = emailAppService;
        _smsAppService = smsAppService;
        _userRepository = userRepository;
        _emailRepository = emailRepository;
        _smsRepository = smsRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 发送统一消息（站内通知/邮件/短信）
    /// </summary>
    public async Task<MessageDispatchResultDto> SendAsync(SendMessageCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var channels = command.Channels;
        if (channels == 0)
        {
            throw new InvalidOperationException("至少需要指定一个消息通道");
        }

        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("消息标题不能为空");
        }

        var content = string.IsNullOrWhiteSpace(command.Content) ? null : command.Content.Trim();
        var hasSiteNotification = channels.HasFlag(MessageChannel.SiteNotification);
        var hasEmail = channels.HasFlag(MessageChannel.Email);
        var hasSms = channels.HasFlag(MessageChannel.Sms);

        if (command.IsGlobal && (hasEmail || hasSms))
        {
            throw new InvalidOperationException("邮件和短信暂不支持全员发送，请指定接收用户");
        }

        var recipientIds = command.RecipientUserIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (!command.IsGlobal && recipientIds.Length == 0)
        {
            throw new InvalidOperationException("非全员消息必须指定接收用户");
        }

        if ((hasEmail || hasSms) && recipientIds.Length == 0)
        {
            throw new InvalidOperationException("邮件或短信通道必须指定接收用户");
        }

        long? resolvedTenantId = command.TenantId;
        SysUser? sender = null;
        if (command.SendUserId.HasValue)
        {
            sender = await _userRepository.GetByIdAsync(command.SendUserId.Value)
                ?? throw new KeyNotFoundException($"未找到发送用户: {command.SendUserId.Value}");

            if (resolvedTenantId.HasValue && sender.TenantId != resolvedTenantId.Value)
            {
                throw new InvalidOperationException("发送用户与租户不一致");
            }

            resolvedTenantId ??= sender.TenantId;
        }

        var recipients = Array.Empty<SysUser>();
        if (recipientIds.Length > 0)
        {
            var users = await _userRepository.GetByIdsAsync(recipientIds);
            if (users.Count != recipientIds.Length)
            {
                throw new InvalidOperationException("存在无效接收用户 ID");
            }

            recipients = users.ToArray();
            if (resolvedTenantId.HasValue && recipients.Any(user => user.TenantId != resolvedTenantId.Value))
            {
                throw new InvalidOperationException("接收用户与租户不一致");
            }

            if (!resolvedTenantId.HasValue)
            {
                var tenantIds = recipients.Select(user => user.TenantId).Distinct().ToArray();
                if (tenantIds.Length > 1)
                {
                    throw new InvalidOperationException("跨租户群发必须显式指定租户");
                }

                resolvedTenantId = tenantIds[0];
            }
        }

        var result = new MessageDispatchResultDto();
        if (hasSiteNotification)
        {
            result.NotificationCount = await _notificationAppService.PushAsync(new PushNotificationCommand
            {
                RecipientUserIds = recipientIds,
                IsGlobal = command.IsGlobal,
                SendUserId = command.SendUserId,
                NotificationType = command.NotificationType,
                Title = title,
                Content = content,
                NeedConfirm = command.NeedConfirm,
                ExpireTime = command.ExpireTime,
                BusinessType = command.BusinessType,
                BusinessId = command.BusinessId,
                TenantId = resolvedTenantId,
                Remark = command.Remark
            });
        }

        if (hasEmail || hasSms)
        {
            using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

            if (hasEmail)
            {
                var missingEmailUsers = recipients.Where(user => string.IsNullOrWhiteSpace(user.Email)).Select(user => user.BasicId).ToArray();
                if (missingEmailUsers.Length > 0)
                {
                    throw new InvalidOperationException($"以下用户缺少邮箱，无法发送邮件: {string.Join(',', missingEmailUsers)}");
                }

                var fromEmail = sender?.Email?.Trim();
                if (string.IsNullOrWhiteSpace(fromEmail))
                {
                    fromEmail = "no-reply@xihan.local";
                }

                var fromName = sender?.RealName ?? sender?.UserName ?? "System";
                var subject = string.IsNullOrWhiteSpace(command.EmailSubject) ? title : command.EmailSubject.Trim();

                foreach (var recipient in recipients)
                {
                    await _emailRepository.AddAsync(new SysEmail
                    {
                        TenantId = resolvedTenantId,
                        SendUserId = command.SendUserId,
                        ReceiveUserId = recipient.BasicId,
                        EmailType = EmailType.Notification,
                        FromEmail = fromEmail,
                        FromName = fromName,
                        ToEmail = recipient.Email!.Trim(),
                        Subject = subject,
                        Content = content,
                        IsHtml = command.EmailIsHtml,
                        TemplateId = command.EmailTemplateId,
                        TemplateParams = command.EmailTemplateParams,
                        EmailStatus = EmailStatus.Pending,
                        ScheduledTime = null,
                        RetryCount = 0,
                        MaxRetryCount = 3,
                        BusinessType = command.BusinessType,
                        BusinessId = command.BusinessId,
                        Remark = command.Remark
                    });
                }

                result.EmailCount = recipients.Length;
            }

            if (hasSms)
            {
                var missingPhoneUsers = recipients.Where(user => string.IsNullOrWhiteSpace(user.Phone)).Select(user => user.BasicId).ToArray();
                if (missingPhoneUsers.Length > 0)
                {
                    throw new InvalidOperationException($"以下用户缺少手机号，无法发送短信: {string.Join(',', missingPhoneUsers)}");
                }

                var smsContent = string.IsNullOrWhiteSpace(content) ? title : content;
                foreach (var recipient in recipients)
                {
                    await _smsRepository.AddAsync(new SysSms
                    {
                        TenantId = resolvedTenantId,
                        SenderId = command.SendUserId,
                        ReceiverId = recipient.BasicId,
                        SmsType = SmsType.Notification,
                        ToPhone = recipient.Phone!.Trim(),
                        Content = smsContent,
                        TemplateId = command.SmsTemplateId,
                        TemplateParams = command.SmsTemplateParams,
                        SmsStatus = SmsStatus.Pending,
                        ScheduledTime = null,
                        RetryCount = 0,
                        MaxRetryCount = 3,
                        BusinessType = command.BusinessType,
                        BusinessId = command.BusinessId,
                        Remark = command.Remark
                    });
                }

                result.SmsCount = recipients.Length;
            }

            await uow.CompleteAsync();
        }

        return result;
    }

    /// <summary>
    /// 获取待分发邮件
    /// </summary>
    public Task<IReadOnlyList<EmailDto>> GetPendingEmailsAsync(int maxCount = 100, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _emailAppService.GetPendingAsync(maxCount, tenantId);
    }

    /// <summary>
    /// 获取待分发短信
    /// </summary>
    public Task<IReadOnlyList<SmsDto>> GetPendingSmsAsync(int maxCount = 100, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _smsAppService.GetPendingAsync(maxCount, tenantId);
    }

    /// <summary>
    /// 更新邮件分发状态
    /// </summary>
    public async Task UpdateEmailDispatchStatusAsync(UpdateEmailDispatchStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.EmailId <= 0)
        {
            throw new InvalidOperationException("邮件ID必须大于0");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var email = await _emailRepository.GetByIdAsync(command.EmailId)
            ?? throw new KeyNotFoundException($"未找到邮件: {command.EmailId}");

        if (command.IsSuccess)
        {
            email.EmailStatus = EmailStatus.Success;
            email.SendTime = DateTimeOffset.UtcNow;
            email.ErrorMessage = null;
        }
        else
        {
            email.RetryCount += 1;
            email.ErrorMessage = string.IsNullOrWhiteSpace(command.ErrorMessage) ? "发送失败" : command.ErrorMessage.Trim();

            var canRetry = command.AllowRetry && email.RetryCount < email.MaxRetryCount;
            email.EmailStatus = canRetry ? EmailStatus.Pending : EmailStatus.Failed;
            if (!canRetry)
            {
                email.SendTime = DateTimeOffset.UtcNow;
            }
        }

        await _emailRepository.UpdateAsync(email);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 更新短信分发状态
    /// </summary>
    public async Task UpdateSmsDispatchStatusAsync(UpdateSmsDispatchStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.SmsId <= 0)
        {
            throw new InvalidOperationException("短信ID必须大于0");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var sms = await _smsRepository.GetByIdAsync(command.SmsId)
            ?? throw new KeyNotFoundException($"未找到短信: {command.SmsId}");

        if (command.IsSuccess)
        {
            sms.SmsStatus = SmsStatus.Success;
            sms.SendTime = DateTimeOffset.UtcNow;
            sms.ErrorMessage = null;
        }
        else
        {
            sms.RetryCount += 1;
            sms.ErrorMessage = string.IsNullOrWhiteSpace(command.ErrorMessage) ? "发送失败" : command.ErrorMessage.Trim();

            var canRetry = command.AllowRetry && sms.RetryCount < sms.MaxRetryCount;
            sms.SmsStatus = canRetry ? SmsStatus.Pending : SmsStatus.Failed;
            if (!canRetry)
            {
                sms.SendTime = DateTimeOffset.UtcNow;
            }
        }

        await _smsRepository.UpdateAsync(sms);
        await uow.CompleteAsync();
    }
}
