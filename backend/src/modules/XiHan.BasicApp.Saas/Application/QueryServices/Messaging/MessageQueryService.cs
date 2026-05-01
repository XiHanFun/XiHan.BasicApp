#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageQueryService
// Guid:d69f673c-5904-4f27-acb4-b167a04910df
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 系统消息查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统消息")]
public sealed class MessageQueryService(
    IEmailRepository emailRepository,
    ISmsRepository smsRepository)
    : SaasApplicationService, IMessageQueryService
{
    /// <summary>
    /// 系统邮件仓储
    /// </summary>
    private readonly IEmailRepository _emailRepository = emailRepository;

    /// <summary>
    /// 系统短信仓储
    /// </summary>
    private readonly ISmsRepository _smsRepository = smsRepository;

    /// <summary>
    /// 获取系统邮件分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统邮件分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    public async Task<PageResultDtoBase<EmailListItemDto>> GetEmailPageAsync(EmailPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildEmailPageRequest(input);
        var emails = await _emailRepository.GetPagedAsync(request, cancellationToken);
        return emails.Map(MessageApplicationMapper.ToEmailListItemDto);
    }

    /// <summary>
    /// 获取系统邮件详情
    /// </summary>
    /// <param name="id">系统邮件主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统邮件详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    public async Task<EmailDetailDto?> GetEmailDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统邮件主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var email = await _emailRepository.GetByIdAsync(id, cancellationToken);
        return email is null ? null : MessageApplicationMapper.ToEmailDetailDto(email);
    }

    /// <summary>
    /// 获取系统短信分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统短信分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    public async Task<PageResultDtoBase<SmsListItemDto>> GetSmsPageAsync(SmsPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildSmsPageRequest(input);
        var smsPage = await _smsRepository.GetPagedAsync(request, cancellationToken);
        return smsPage.Map(MessageApplicationMapper.ToSmsListItemDto);
    }

    /// <summary>
    /// 获取系统短信详情
    /// </summary>
    /// <param name="id">系统短信主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统短信详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    public async Task<SmsDetailDto?> GetSmsDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统短信主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sms = await _smsRepository.GetByIdAsync(id, cancellationToken);
        return sms is null ? null : MessageApplicationMapper.ToSmsDetailDto(sms);
    }

    /// <summary>
    /// 构建系统邮件分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统邮件分页请求</returns>
    private static BasicAppPRDto BuildEmailPageRequest(EmailPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysEmail.Subject),
                nameof(SysEmail.BusinessType));
        }

        if (input.SendUserId.HasValue && input.SendUserId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysEmail.SendUserId), input.SendUserId.Value);
        }

        if (input.ReceiveUserId.HasValue && input.ReceiveUserId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysEmail.ReceiveUserId), input.ReceiveUserId.Value);
        }

        if (input.EmailType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysEmail.EmailType), input.EmailType.Value);
        }

        if (input.EmailStatus.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysEmail.EmailStatus), input.EmailStatus.Value);
        }

        if (input.TemplateId.HasValue && input.TemplateId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysEmail.TemplateId), input.TemplateId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.BusinessType))
        {
            request.Conditions.AddFilter(nameof(SysEmail.BusinessType), input.BusinessType.Trim());
        }

        if (input.BusinessId.HasValue && input.BusinessId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysEmail.BusinessId), input.BusinessId.Value);
        }

        AddTimeRange(request, nameof(SysEmail.ScheduledTime), input.ScheduledTimeStart, input.ScheduledTimeEnd);
        AddTimeRange(request, nameof(SysEmail.SendTime), input.SendTimeStart, input.SendTimeEnd);
        request.Conditions.AddSort(nameof(SysEmail.SendTime), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysEmail.CreatedTime), SortDirection.Descending, 1);
        return request;
    }

    /// <summary>
    /// 构建系统短信分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统短信分页请求</returns>
    private static BasicAppPRDto BuildSmsPageRequest(SmsPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysSms.Provider),
                nameof(SysSms.BusinessType));
        }

        if (input.SenderId.HasValue && input.SenderId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysSms.SenderId), input.SenderId.Value);
        }

        if (input.ReceiverId.HasValue && input.ReceiverId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysSms.ReceiverId), input.ReceiverId.Value);
        }

        if (input.SmsType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysSms.SmsType), input.SmsType.Value);
        }

        if (input.SmsStatus.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysSms.SmsStatus), input.SmsStatus.Value);
        }

        if (input.TemplateId.HasValue && input.TemplateId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysSms.TemplateId), input.TemplateId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Provider))
        {
            request.Conditions.AddFilter(nameof(SysSms.Provider), input.Provider.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.BusinessType))
        {
            request.Conditions.AddFilter(nameof(SysSms.BusinessType), input.BusinessType.Trim());
        }

        if (input.BusinessId.HasValue && input.BusinessId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysSms.BusinessId), input.BusinessId.Value);
        }

        AddTimeRange(request, nameof(SysSms.ScheduledTime), input.ScheduledTimeStart, input.ScheduledTimeEnd);
        AddTimeRange(request, nameof(SysSms.SendTime), input.SendTimeStart, input.SendTimeEnd);
        request.Conditions.AddSort(nameof(SysSms.SendTime), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysSms.CreatedTime), SortDirection.Descending, 1);
        return request;
    }

    /// <summary>
    /// 添加时间范围筛选
    /// </summary>
    private static void AddTimeRange(BasicAppPRDto request, string fieldName, DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start.HasValue)
        {
            request.Conditions.AddFilter(fieldName, start.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (end.HasValue)
        {
            request.Conditions.AddFilter(fieldName, end.Value, QueryOperator.LessThanOrEqual);
        }
    }
}
