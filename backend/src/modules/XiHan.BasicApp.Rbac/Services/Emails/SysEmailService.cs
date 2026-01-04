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

using Mapster;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
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
        return emails.Adapt<List<EmailDto>>();
    }

    /// <summary>
    /// 根据邮件类型获取邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetByTypeAsync(EmailType emailType)
    {
        var emails = await _emailRepository.GetByTypeAsync(emailType);
        return emails.Adapt<List<EmailDto>>();
    }

    /// <summary>
    /// 根据接收邮箱获取邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetByToEmailAsync(string toEmail)
    {
        var emails = await _emailRepository.GetByToEmailAsync(toEmail);
        return emails.Adapt<List<EmailDto>>();
    }

    /// <summary>
    /// 根据发送者ID获取邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetBySenderIdAsync(XiHanBasicAppIdType senderId)
    {
        var emails = await _emailRepository.GetBySenderIdAsync(senderId);
        return emails.Adapt<List<EmailDto>>();
    }

    /// <summary>
    /// 根据接收者ID获取邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetByReceiverIdAsync(XiHanBasicAppIdType receiverId)
    {
        var emails = await _emailRepository.GetByReceiverIdAsync(receiverId);
        return emails.Adapt<List<EmailDto>>();
    }

    /// <summary>
    /// 获取待发送的邮件列表
    /// </summary>
    public async Task<List<EmailDto>> GetPendingEmailsAsync(int count = 100)
    {
        var emails = await _emailRepository.GetPendingEmailsAsync(count);
        return emails.Adapt<List<EmailDto>>();
    }

    #endregion 业务特定方法
}
