#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageQueryService
// Guid:e6a9c3f8-5d27-4b14-9e0c-7f4b8a2d6c53
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
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
/// 系统消息查询应用服务（邮件/短信发送记录）
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "消息记录")]
public sealed class MessageQueryService
    : SaasApplicationService, IMessageQueryService
{
    private readonly IEmailRepository _emailRepository;

    private readonly ISmsRepository _smsRepository;

    /// <summary>
    /// 字段级安全（排序门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageQueryService(IEmailRepository emailRepository, ISmsRepository smsRepository, IFieldSecurityService fieldSecurityService)
    {
        _emailRepository = emailRepository;
        _smsRepository = smsRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<EmailListItemDto>> GetEmailPageAsync(EmailPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildEmailPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysEmail", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyEmailSorts(request);
        }

        var page = await _emailRepository.GetPagedAsync(request, cancellationToken);
        var items = page.Items.Select(MessageApplicationMapper.ToEmailListItemDto).ToList();
        return new PageResultDtoBase<EmailListItemDto>(items, page.Page);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.Message.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<SmsListItemDto>> GetSmsPageAsync(SmsPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildSmsPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysSms", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplySmsSorts(request);
        }

        var page = await _smsRepository.GetPagedAsync(request, cancellationToken);
        var items = page.Items.Select(MessageApplicationMapper.ToSmsListItemDto).ToList();
        return new PageResultDtoBase<SmsListItemDto>(items, page.Page);
    }

    /// <inheritdoc />
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
    /// 构建邮件分页请求
    /// </summary>
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
            request.Conditions.SetKeyword<SysEmail>(
                input.Keyword.Trim(),
                email => email.Subject,
                email => email.ToEmail,
                email => email.FromEmail,
                email => email.TemplateCode,
                email => email.BusinessType);
        }

        if (input.SendUserId.HasValue)
        {
            request.Conditions.AddFilter((SysEmail email) => email.SendUserId, input.SendUserId.Value);
        }

        if (input.ReceiveUserId.HasValue)
        {
            request.Conditions.AddFilter((SysEmail email) => email.ReceiveUserId, input.ReceiveUserId.Value);
        }

        if (input.EmailType.HasValue)
        {
            request.Conditions.AddFilter((SysEmail email) => email.EmailType, input.EmailType.Value);
        }

        if (input.EmailStatus.HasValue)
        {
            request.Conditions.AddFilter((SysEmail email) => email.EmailStatus, input.EmailStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.TemplateCode))
        {
            request.Conditions.AddFilter((SysEmail email) => email.TemplateCode, input.TemplateCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.BusinessType))
        {
            request.Conditions.AddFilter((SysEmail email) => email.BusinessType, input.BusinessType.Trim());
        }

        if (input.BusinessId.HasValue)
        {
            request.Conditions.AddFilter((SysEmail email) => email.BusinessId, input.BusinessId.Value);
        }

        if (input.ScheduledTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysEmail email) => email.ScheduledTime, input.ScheduledTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ScheduledTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysEmail email) => email.ScheduledTime, input.ScheduledTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        if (input.SendTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysEmail email) => email.SendTime, input.SendTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.SendTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysEmail email) => email.SendTime, input.SendTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }
        return request;
    }

    /// <summary>
    /// 应用邮件默认排序（无前端排序时的兜底）
    /// </summary>
    private static void ApplyEmailSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysEmail email) => email.CreatedTime, SortDirection.Descending, 0);
    }

    /// <summary>
    /// 构建短信分页请求
    /// </summary>
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
            request.Conditions.SetKeyword<SysSms>(
                input.Keyword.Trim(),
                sms => sms.ToPhone,
                sms => sms.Content,
                sms => sms.TemplateCode,
                sms => sms.Provider,
                sms => sms.BusinessType);
        }

        if (input.SenderId.HasValue)
        {
            request.Conditions.AddFilter((SysSms sms) => sms.SenderId, input.SenderId.Value);
        }

        if (input.ReceiverId.HasValue)
        {
            request.Conditions.AddFilter((SysSms sms) => sms.ReceiverId, input.ReceiverId.Value);
        }

        if (input.SmsType.HasValue)
        {
            request.Conditions.AddFilter((SysSms sms) => sms.SmsType, input.SmsType.Value);
        }

        if (input.SmsStatus.HasValue)
        {
            request.Conditions.AddFilter((SysSms sms) => sms.SmsStatus, input.SmsStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.TemplateCode))
        {
            request.Conditions.AddFilter((SysSms sms) => sms.TemplateCode, input.TemplateCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.Provider))
        {
            request.Conditions.AddFilter((SysSms sms) => sms.Provider, input.Provider.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.BusinessType))
        {
            request.Conditions.AddFilter((SysSms sms) => sms.BusinessType, input.BusinessType.Trim());
        }

        if (input.BusinessId.HasValue)
        {
            request.Conditions.AddFilter((SysSms sms) => sms.BusinessId, input.BusinessId.Value);
        }

        if (input.ScheduledTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysSms sms) => sms.ScheduledTime, input.ScheduledTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ScheduledTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysSms sms) => sms.ScheduledTime, input.ScheduledTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        if (input.SendTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysSms sms) => sms.SendTime, input.SendTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.SendTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysSms sms) => sms.SendTime, input.SendTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }
        return request;
    }

    /// <summary>
    /// 应用短信默认排序（无前端排序时的兜底）
    /// </summary>
    private static void ApplySmsSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysSms sms) => sms.CreatedTime, SortDirection.Descending, 0);
    }
}
