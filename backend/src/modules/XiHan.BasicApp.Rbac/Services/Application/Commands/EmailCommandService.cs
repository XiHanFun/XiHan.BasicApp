#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailCommandService
// Guid:a7b8c9d0-e1f2-4a3b-4c5d-6e7f8a9b0c1d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// 邮件命令服务（处理邮件的写操作）
/// </summary>
public class EmailCommandService : CrudApplicationServiceBase<SysEmail, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IEmailRepository _emailRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EmailCommandService(IEmailRepository emailRepository)
        : base(emailRepository)
    {
        _emailRepository = emailRepository;
    }

    /// <summary>
    /// 发送邮件（创建邮件记录并标记为待发送）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        var email = input.Adapt<SysEmail>();

        // 设置初始状态
        email.Status = "Pending";
        email.SendTime = DateTimeOffset.UtcNow;
        email.RetryCount = 0;

        email = await _emailRepository.AddAsync(email);

        // TODO: 触发邮件发送任务
        // await _emailSender.SendAsync(email);

        return await MapToEntityDtoAsync(email);
    }

    /// <summary>
    /// 更新邮件发送状态
    /// </summary>
    /// <param name="emailId">邮件ID</param>
    /// <param name="status">发送状态</param>
    /// <param name="errorMessage">错误信息</param>
    public async Task<bool> UpdateSendStatusAsync(long emailId, string status, string? errorMessage = null)
    {
        var email = await _emailRepository.GetByIdAsync(emailId);
        if (email == null)
        {
            return false;
        }

        email.Status = status;
        if (!string.IsNullOrEmpty(errorMessage))
        {
            email.ErrorMessage = errorMessage;
        }

        await _emailRepository.UpdateAsync(email);
        return true;
    }

    /// <summary>
    /// 重试发送失败的邮件
    /// </summary>
    /// <param name="emailId">邮件ID</param>
    public async Task<bool> RetryFailedEmailAsync(long emailId)
    {
        var email = await _emailRepository.GetByIdAsync(emailId);
        if (email == null || email.Status != "Failed")
        {
            return false;
        }

        email.Status = "Pending";
        email.RetryCount++;
        email.SendTime = DateTimeOffset.UtcNow;

        await _emailRepository.UpdateAsync(email);

        // TODO: 触发邮件发送任务
        // await _emailSender.SendAsync(email);

        return true;
    }

    /// <summary>
    /// 批量删除已发送的旧邮件
    /// </summary>
    /// <param name="beforeDate">删除此日期之前的邮件</param>
    public async Task<int> DeleteSentEmailsBeforeDateAsync(DateTimeOffset beforeDate)
    {
        return await _emailRepository.DeleteSentEmailsBeforeDateAsync(beforeDate);
    }
}
