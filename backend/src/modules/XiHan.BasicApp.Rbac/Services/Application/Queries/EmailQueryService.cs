#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailQueryService
// Guid:b8c9d0e1-f2a3-4b4c-5d6e-7f8a9b0c1d2e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 邮件查询服务（处理邮件的读操作 - CQRS）
/// </summary>
public class EmailQueryService : ApplicationServiceBase
{
    private readonly IEmailRepository _emailRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EmailQueryService(IEmailRepository emailRepository)
    {
        _emailRepository = emailRepository;
    }

    /// <summary>
    /// 根据ID获取邮件
    /// </summary>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var email = await _emailRepository.GetByIdAsync(id);
        return email?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据发送状态获取邮件列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetBySendStatusAsync(SmsStatus status)
    {
        var emails = await _emailRepository.GetBySendStatusAsync(status);
        return emails.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取待发送的邮件列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetPendingEmailsAsync()
    {
        var emails = await _emailRepository.GetPendingEmailsAsync();
        return emails.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取发送失败的邮件列表
    /// </summary>
    public async Task<List<RbacDtoBase>> GetFailedEmailsAsync()
    {
        var emails = await _emailRepository.GetFailedEmailsAsync();
        return emails.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _emailRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
