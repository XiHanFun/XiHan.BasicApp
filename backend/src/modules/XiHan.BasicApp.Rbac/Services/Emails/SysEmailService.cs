#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysEmailService
// Guid:v1w2x3y4-z5a6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.Emails;
using XiHan.BasicApp.Rbac.Services.Emails.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Emails;

/// <summary>
/// 系统邮件服务实现
/// </summary>
public class SysEmailService : CrudApplicationServiceBase<SysEmail, EmailDto, XiHanBasicAppIdType, CreateEmailDto, UpdateEmailDto>, ISysEmailService
{
    private readonly ISysEmailRepository _emailRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysEmailService(ISysEmailRepository emailRepository) : base(emailRepository)
    {
        _emailRepository = emailRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据邮件状态获取邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetByStatusAsync(EmailStatus emailStatus)
    {
        var emails = await _emailRepository.GetByStatusAsync(emailStatus);
        return emails.ToDto();
    }

    /// <summary>
    /// 根据邮件类型获取邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetByTypeAsync(EmailType emailType)
    {
        var emails = await _emailRepository.GetByTypeAsync(emailType);
        return emails.ToDto();
    }

    /// <summary>
    /// 根据接收邮箱获取邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetByToEmailAsync(string toEmail)
    {
        var emails = await _emailRepository.GetByToEmailAsync(toEmail);
        return emails.ToDto();
    }

    /// <summary>
    /// 根据发送者ID获取邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetBySenderIdAsync(XiHanBasicAppIdType senderId)
    {
        var emails = await _emailRepository.GetBySenderIdAsync(senderId);
        return emails.ToDto();
    }

    /// <summary>
    /// 根据接收者ID获取邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetByReceiverIdAsync(XiHanBasicAppIdType receiverId)
    {
        var emails = await _emailRepository.GetByReceiverIdAsync(receiverId);
        return emails.ToDto();
    }

    /// <summary>
    /// 获取待发送的邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetPendingEmailsAsync(int count = 100)
    {
        var emails = await _emailRepository.GetPendingEmailsAsync(count);
        return emails.ToDto();
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<EmailDto> MapToEntityDtoAsync(SysEmail entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 EmailDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysEmail> MapToEntityAsync(EmailDto dto)
    {
        var entity = new SysEmail
        {
            TenantId = dto.TenantId,
            SenderId = dto.SenderId,
            ReceiverId = dto.ReceiverId,
            EmailType = dto.EmailType,
            FromEmail = dto.FromEmail,
            FromName = dto.FromName,
            ToEmail = dto.ToEmail,
            CcEmail = dto.CcEmail,
            BccEmail = dto.BccEmail,
            Subject = dto.Subject,
            Content = dto.Content,
            IsHtml = dto.IsHtml,
            Attachments = dto.Attachments,
            TemplateId = dto.TemplateId,
            TemplateParams = dto.TemplateParams,
            EmailStatus = dto.EmailStatus,
            ScheduledTime = dto.ScheduledTime,
            SendTime = dto.SendTime,
            RetryCount = dto.RetryCount,
            MaxRetryCount = dto.MaxRetryCount,
            ErrorMessage = dto.ErrorMessage,
            BusinessType = dto.BusinessType,
            BusinessId = dto.BusinessId,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 EmailDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(EmailDto dto, SysEmail entity)
    {
        entity.EmailStatus = dto.EmailStatus;
        entity.ScheduledTime = dto.ScheduledTime;
        entity.SendTime = dto.SendTime;
        entity.RetryCount = dto.RetryCount;
        entity.ErrorMessage = dto.ErrorMessage;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysEmail> MapToEntityAsync(CreateEmailDto createDto)
    {
        var entity = new SysEmail
        {
            TenantId = createDto.TenantId,
            SenderId = createDto.SenderId,
            ReceiverId = createDto.ReceiverId,
            EmailType = createDto.EmailType,
            FromEmail = createDto.FromEmail,
            FromName = createDto.FromName,
            ToEmail = createDto.ToEmail,
            CcEmail = createDto.CcEmail,
            BccEmail = createDto.BccEmail,
            Subject = createDto.Subject,
            Content = createDto.Content,
            IsHtml = createDto.IsHtml,
            Attachments = createDto.Attachments,
            TemplateId = createDto.TemplateId,
            TemplateParams = createDto.TemplateParams,
            ScheduledTime = createDto.ScheduledTime,
            MaxRetryCount = createDto.MaxRetryCount,
            BusinessType = createDto.BusinessType,
            BusinessId = createDto.BusinessId,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateEmailDto updateDto, SysEmail entity)
    {
        if (updateDto.EmailStatus.HasValue) entity.EmailStatus = updateDto.EmailStatus.Value;
        if (updateDto.ScheduledTime.HasValue) entity.ScheduledTime = updateDto.ScheduledTime;
        if (updateDto.ErrorMessage != null) entity.ErrorMessage = updateDto.ErrorMessage;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}

