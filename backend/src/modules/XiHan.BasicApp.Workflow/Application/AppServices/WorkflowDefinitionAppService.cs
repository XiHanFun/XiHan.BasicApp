#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowDefinitionAppService
// Guid:85f2c61d-49e0-4b37-a5d8-20c94e17f6b3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:28:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Globalization;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Application.Mappers;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Builders;

namespace XiHan.BasicApp.Workflow.Application.AppServices;

/// <summary>
/// 工作流定义命令应用服务
/// </summary>
/// <remarks>
/// 生命周期规则（草稿可改可删、发布不可变、版本自增）由框架定义管理器执行；
/// 本服务负责 DTO 转换与工作流协议异常到业务异常的翻译。
/// </remarks>
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务", Tag = "流程定义")]
public sealed class WorkflowDefinitionAppService : WorkflowApplicationService, IWorkflowDefinitionAppService
{
    private readonly IWorkflowDefinitionManager _definitionManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowDefinitionAppService(IWorkflowDefinitionManager definitionManager)
    {
        _definitionManager = definitionManager;
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Create)]
    public async Task<WorkflowDefinitionDetailDto> CreateAsync(WorkflowDefinitionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var definition = ParseDefinition(input.DefinitionJson);
        var created = await TranslateAsync(() => _definitionManager.CreateAsync(definition, cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(created);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Update)]
    public async Task<WorkflowDefinitionDetailDto> UpdateDraftAsync(WorkflowDefinitionUpdateDraftDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var definition = ParseDefinition(input.DefinitionJson);
        definition.Id = ToWorkflowId(input.BasicId);
        var updated = await TranslateAsync(() => _definitionManager.UpdateDraftAsync(definition, cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(updated);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Update)]
    public async Task<WorkflowDefinitionDetailDto> PublishAsync(WorkflowDefinitionIdDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var published = await TranslateAsync(() => _definitionManager.PublishAsync(ToWorkflowId(input.BasicId), cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(published);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Create)]
    public async Task<WorkflowDefinitionDetailDto> NewVersionAsync(WorkflowDefinitionNewVersionDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Code);

        var draft = await TranslateAsync(() => _definitionManager.CreateNewVersionAsync(input.Code.Trim(), cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(draft);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Update)]
    public async Task<WorkflowDefinitionDetailDto> DisableAsync(WorkflowDefinitionIdDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var disabled = await TranslateAsync(() => _definitionManager.DisableAsync(ToWorkflowId(input.BasicId), cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(disabled);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Update)]
    public async Task<WorkflowDefinitionDetailDto> ArchiveAsync(WorkflowDefinitionIdDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var archived = await TranslateAsync(() => _definitionManager.ArchiveAsync(ToWorkflowId(input.BasicId), cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(archived);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "定义主键必须大于 0。");
        }

        await TranslateAsync(async () =>
        {
            await _definitionManager.DeleteAsync(ToWorkflowId(id), cancellationToken);
            return true;
        });
    }

    /// <summary>
    /// 解析设计器定义 JSON（非法 JSON 转业务异常）
    /// </summary>
    private static WorkflowDefinition ParseDefinition(string definitionJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionJson);

        try
        {
            return WorkflowDefinitionJsonSerializer.Deserialize(definitionJson);
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: ex.Message);
        }
    }

    /// <summary>
    /// 工作流协议异常转业务异常（校验失败/状态非法等对调用方是可纠正错误）
    /// </summary>
    private static async Task<T> TranslateAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: ex.Message);
        }
    }

    private static string ToWorkflowId(long id)
    {
        return id.ToString(CultureInfo.InvariantCulture);
    }
}
