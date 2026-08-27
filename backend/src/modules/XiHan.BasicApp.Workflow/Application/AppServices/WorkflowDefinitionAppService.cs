// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    /// <summary>
    /// 创建定义草稿（版本号自动取编码下最大版本 + 1）
    /// </summary>
    [PermissionAuthorize(WorkflowPermissionCodes.Create)]
    public async Task<WorkflowDefinitionDetailDto> CreateAsync(WorkflowDefinitionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var definition = ParseDefinition(input.DefinitionJson);
        var created = await TranslateAsync(() => _definitionManager.CreateAsync(definition, cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(created);
    }

    /// <summary>
    /// 更新草稿定义
    /// </summary>
    [PermissionAuthorize(WorkflowPermissionCodes.Update)]
    public async Task<WorkflowDefinitionDetailDto> UpdateDraftAsync(WorkflowDefinitionUpdateDraftDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        RequirePositiveKey(input.BasicId, nameof(input));

        var definition = ParseDefinition(input.DefinitionJson);
        definition.Id = ToWorkflowId(input.BasicId);
        var updated = await TranslateAsync(() => _definitionManager.UpdateDraftAsync(definition, cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(updated);
    }

    /// <summary>
    /// 发布定义（发布前结构校验）
    /// </summary>
    [PermissionAuthorize(WorkflowPermissionCodes.Update)]
    public async Task<WorkflowDefinitionDetailDto> PublishAsync(WorkflowDefinitionIdDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        RequirePositiveKey(input.BasicId, nameof(input));

        var published = await TranslateAsync(() => _definitionManager.PublishAsync(ToWorkflowId(input.BasicId), cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(published);
    }

    /// <summary>
    /// 基于最新版本创建新草稿版本
    /// </summary>
    [PermissionAuthorize(WorkflowPermissionCodes.Create)]
    public async Task<WorkflowDefinitionDetailDto> NewVersionAsync(WorkflowDefinitionNewVersionDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Code);

        var draft = await TranslateAsync(() => _definitionManager.CreateNewVersionAsync(input.Code.Trim(), cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(draft);
    }

    /// <summary>
    /// 停用定义
    /// </summary>
    [PermissionAuthorize(WorkflowPermissionCodes.Update)]
    public async Task<WorkflowDefinitionDetailDto> DisableAsync(WorkflowDefinitionIdDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        RequirePositiveKey(input.BasicId, nameof(input));

        var disabled = await TranslateAsync(() => _definitionManager.DisableAsync(ToWorkflowId(input.BasicId), cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(disabled);
    }

    /// <summary>
    /// 归档定义
    /// </summary>
    [PermissionAuthorize(WorkflowPermissionCodes.Update)]
    public async Task<WorkflowDefinitionDetailDto> ArchiveAsync(WorkflowDefinitionIdDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        RequirePositiveKey(input.BasicId, nameof(input));

        var archived = await TranslateAsync(() => _definitionManager.ArchiveAsync(ToWorkflowId(input.BasicId), cancellationToken));
        return WorkflowApplicationMapper.ToDetailDto(archived);
    }

    /// <summary>
    /// 删除草稿定义
    /// </summary>
    [PermissionAuthorize(WorkflowPermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        RequirePositiveKey(id, nameof(id));

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

    /// <summary>
    /// 按主键操作的统一入参守卫：定义主键必须为正数。
    /// </summary>
    /// <remarks>
    /// 0 与负数会被 <see cref="ToWorkflowId"/> 原样拼成 "0" / "-1" 下探给定义管理器，
    /// 再由框架侧以 NumberStyles.None 解析失败抛协议异常，最终翻译成一条带框架内部标识文本的业务异常，
    /// 调用方无从判断是自己漏传了主键。在入口拒绝，错误就停在参数层。
    /// </remarks>
    /// <param name="id">定义主键</param>
    /// <param name="paramName">对外暴露的参数名</param>
    private static void RequirePositiveKey(long id, string paramName)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "定义主键必须大于 0。");
        }
    }

    private static string ToWorkflowId(long id)
    {
        return id.ToString(CultureInfo.InvariantCulture);
    }
}
