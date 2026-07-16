#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowInstanceQueryService
// Guid:71c04d8e-395f-4b26-a8d1-64e50c92f7b3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:26:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using System.Globalization;
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
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Stores;

namespace XiHan.BasicApp.Workflow.Application.QueryServices;

/// <summary>
/// 工作流实例查询应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务", Tag = "流程实例")]
public sealed class WorkflowInstanceQueryService : WorkflowApplicationService, IWorkflowInstanceQueryService
{
    private readonly IWorkflowInstanceRepository _instanceRepository;
    private readonly IWorkflowInstanceStore _instanceStore;
    private readonly IWorkflowBookmarkStore _bookmarkStore;

    /// <summary>
    /// 字段级安全（排序/过滤门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowInstanceQueryService(
        IWorkflowInstanceRepository instanceRepository,
        IWorkflowInstanceStore instanceStore,
        IWorkflowBookmarkStore bookmarkStore,
        IFieldSecurityService fieldSecurityService)
    {
        _instanceRepository = instanceRepository;
        _instanceStore = instanceStore;
        _bookmarkStore = bookmarkStore;
        _fieldSecurity = fieldSecurityService;
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<WorkflowInstanceListItemDto>> GetPageAsync(WorkflowInstancePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);

        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysWorkflowInstance", cancellationToken);
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysWorkflowInstance", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            request.Conditions.AddSort((SysWorkflowInstance instance) => instance.CreationTime, SortDirection.Descending, 0);
        }

        var page = await _instanceRepository.GetPagedAsync(request, cancellationToken);
        var items = page.Items.Select(WorkflowApplicationMapper.ToListItemDto).ToList();
        return new PageResultDtoBase<WorkflowInstanceListItemDto>(items, page.Page)
        {
            ExtendDatas = page.ExtendDatas
        };
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Read)]
    public async Task<WorkflowInstanceDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "实例主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var instanceId = id.ToString(CultureInfo.InvariantCulture);
        var instance = await _instanceStore.FindAsync(instanceId, cancellationToken);
        if (instance is null)
        {
            return null;
        }

        var nodeInstances = await _instanceStore.GetNodeInstancesAsync(instanceId, cancellationToken);
        var bookmarks = await _bookmarkStore.GetByInstanceAsync(instanceId, cancellationToken);
        return WorkflowApplicationMapper.ToDetailDto(instance, nodeInstances, bookmarks);
    }

    /// <summary>
    /// 构建实例分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(WorkflowInstancePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysWorkflowInstance>(
                input.Keyword.Trim(),
                instance => instance.Name,
                instance => instance.CorrelationId!);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysWorkflowInstance instance) => instance.Status, input.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.DefinitionCode))
        {
            request.Conditions.AddFilter((SysWorkflowInstance instance) => instance.DefinitionCode, input.DefinitionCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.CorrelationId))
        {
            request.Conditions.AddFilter((SysWorkflowInstance instance) => instance.CorrelationId, input.CorrelationId.Trim());
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
