#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowInstanceAppService
// Guid:0b74e6d2-c918-4f50-a3e7-86d25c91f4b0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:29:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Globalization;
using System.Text.Json;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Application.Mappers;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Application.AppServices;

/// <summary>
/// 工作流实例命令应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务", Tag = "流程实例")]
public sealed class WorkflowInstanceAppService : WorkflowApplicationService, IWorkflowInstanceAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IWorkflowEngine _engine;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowInstanceAppService(IWorkflowEngine engine, ICurrentUser currentUser)
    {
        _engine = engine;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Execute)]
    public async Task<WorkflowInstanceListItemDto> StartAsync(WorkflowInstanceStartDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.DefinitionCode);

        var request = new WorkflowStartRequest
        {
            DefinitionCode = input.DefinitionCode.Trim(),
            DefinitionVersion = input.DefinitionVersion,
            Name = input.Name,
            CorrelationId = input.CorrelationId,
            StarterId = _currentUser.UserId?.ToString(CultureInfo.InvariantCulture),
            Variables = ParseJsonObject(input.VariablesJson, "启动变量")
        };

        var instance = await TranslateAsync(() => _engine.StartAsync(request, cancellationToken));
        return WorkflowApplicationMapper.ToListItemDto(instance);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Execute)]
    public async Task<WorkflowInstanceListItemDto> CancelAsync(WorkflowInstanceOperationDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var instance = await TranslateAsync(() => _engine.CancelAsync(ToWorkflowId(input.BasicId), input.Reason, cancellationToken));
        return WorkflowApplicationMapper.ToListItemDto(instance);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Execute)]
    public async Task<WorkflowInstanceListItemDto> TerminateAsync(WorkflowInstanceOperationDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var instance = await TranslateAsync(() => _engine.TerminateAsync(ToWorkflowId(input.BasicId), input.Reason, cancellationToken));
        return WorkflowApplicationMapper.ToListItemDto(instance);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Execute)]
    public async Task<WorkflowInstanceListItemDto> RetryAsync(WorkflowInstanceIdDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var instance = await TranslateAsync(() => _engine.RetryAsync(ToWorkflowId(input.BasicId), cancellationToken));
        return WorkflowApplicationMapper.ToListItemDto(instance);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Update)]
    public async Task<WorkflowInstanceListItemDto> SuspendAsync(WorkflowInstanceOperationDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var instance = await TranslateAsync(() => _engine.SuspendAsync(ToWorkflowId(input.BasicId), input.Reason, cancellationToken));
        return WorkflowApplicationMapper.ToListItemDto(instance);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Update)]
    public async Task<WorkflowInstanceListItemDto> ResumeAsync(WorkflowInstanceIdDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var instance = await TranslateAsync(() => _engine.ResumeAsync(ToWorkflowId(input.BasicId), cancellationToken));
        return WorkflowApplicationMapper.ToListItemDto(instance);
    }

    /// <inheritdoc />
    [PermissionAuthorize(WorkflowPermissionCodes.Execute)]
    public async Task<WorkflowSignalPublishResultDto> PublishSignalAsync(WorkflowSignalPublishDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.SignalName);

        var resumedCount = await TranslateAsync(() => _engine.PublishSignalAsync(
            input.SignalName.Trim(),
            ParseJsonObject(input.PayloadJson, "信号载荷"),
            input.CorrelationId,
            cancellationToken));
        return new WorkflowSignalPublishResultDto { ResumedCount = resumedCount };
    }

    /// <summary>
    /// 解析 JSON 对象文本（非法 JSON 转业务异常）
    /// </summary>
    private static Dictionary<string, object?> ParseJsonObject(string? json, string what)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions)
                ?? throw new BusinessException(message: $"{what}必须是 JSON 对象");
        }
        catch (JsonException ex)
        {
            throw new BusinessException(message: $"{what} JSON 非法：{ex.Message}");
        }
    }

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
