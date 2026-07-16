#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowDefinitionQueryService
// Guid:2e58d1b7-c904-4f63-a2e5-80d17c94b6f2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Application.Mappers;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Workflow.Application.QueryServices;

/// <summary>
/// 工作流定义查询应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务", Tag = "流程定义")]
public sealed class WorkflowDefinitionQueryService : WorkflowApplicationService, IWorkflowDefinitionQueryService
{
    private readonly IWorkflowDefinitionRepository _definitionRepository;

    /// <summary>
    /// 字段级安全（排序/过滤门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowDefinitionQueryService(IWorkflowDefinitionRepository definitionRepository, IFieldSecurityService fieldSecurityService)
    {
        _definitionRepository = definitionRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<WorkflowDefinitionListItemDto>> GetPageAsync(WorkflowDefinitionPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);

        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysWorkflowDefinition", cancellationToken);
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysWorkflowDefinition", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            request.Conditions.AddSort((SysWorkflowDefinition definition) => definition.Code, SortDirection.Ascending, 0);
            request.Conditions.AddSort((SysWorkflowDefinition definition) => definition.Version, SortDirection.Descending, 1);
        }

        var page = await _definitionRepository.GetPagedAsync(request, cancellationToken);
        var items = page.Items.Select(WorkflowApplicationMapper.ToListItemDto).ToList();
        return new PageResultDtoBase<WorkflowDefinitionListItemDto>(items, page.Page)
        {
            ExtendDatas = page.ExtendDatas
        };
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Read)]
    public async Task<WorkflowDefinitionDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "定义主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var entity = await _definitionRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : WorkflowApplicationMapper.ToDetailDto(entity);
    }

    /// <summary>
    /// 构建定义分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(WorkflowDefinitionPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysWorkflowDefinition>(
                input.Keyword.Trim(),
                definition => definition.Code,
                definition => definition.Name);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysWorkflowDefinition definition) => definition.Status, input.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Category))
        {
            request.Conditions.AddFilter((SysWorkflowDefinition definition) => definition.Category, input.Category.Trim());
        }

        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        return request;
    }
}
