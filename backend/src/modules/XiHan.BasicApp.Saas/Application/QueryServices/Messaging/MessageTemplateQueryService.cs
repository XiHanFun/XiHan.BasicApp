#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageTemplateQueryService
// Guid:9f0e1633-7acf-4011-ae83-f61ed9eaa01e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
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
/// 消息模板查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "消息模板")]
public sealed class MessageTemplateQueryService
    : SaasApplicationService, IMessageTemplateQueryService
{
    private readonly IMessageTemplateRepository _messageTemplateRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageTemplateQueryService(IMessageTemplateRepository messageTemplateRepository)
    {
        _messageTemplateRepository = messageTemplateRepository;
    }

    /// <summary>
    /// 获取消息模板分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息模板分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.MessageTemplate.Read)]
    public async Task<PageResultDtoBase<MessageTemplateListItemDto>> GetMessageTemplatePageAsync(MessageTemplatePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);
        var templates = await _messageTemplateRepository.GetPagedAsync(request, cancellationToken);
        var items = templates.Items
            .Select(MessageTemplateApplicationMapper.ToListItemDto)
            .ToList();

        return new PageResultDtoBase<MessageTemplateListItemDto>(items, templates.Page)
        {
            ExtendDatas = templates.ExtendDatas
        };
    }

    /// <summary>
    /// 获取消息模板详情
    /// </summary>
    /// <param name="id">模板主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息模板详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.MessageTemplate.Read)]
    public async Task<MessageTemplateDetailDto?> GetMessageTemplateDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "模板主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var template = await _messageTemplateRepository.GetByIdAsync(id, cancellationToken);
        return template is null ? null : MessageTemplateApplicationMapper.ToDetailDto(template);
    }

    /// <summary>
    /// 构建分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(MessageTemplatePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysMessageTemplate>(
                input.Keyword.Trim(),
                template => template.TemplateCode,
                template => template.TemplateName,
                template => template.Description,
                template => template.Remark);
        }

        if (input.Channel.HasValue)
        {
            request.Conditions.AddFilter((SysMessageTemplate template) => template.Channel, input.Channel.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysMessageTemplate template) => template.Status, input.Status.Value);
        }

        request.Conditions.AddSort((SysMessageTemplate template) => template.Channel, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysMessageTemplate template) => template.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysMessageTemplate template) => template.TemplateCode, SortDirection.Ascending, 2);
        return request;
    }
}
