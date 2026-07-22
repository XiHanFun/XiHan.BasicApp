// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Workflow.Application.Contracts;

/// <summary>
/// 工作流实例查询应用服务接口
/// </summary>
public interface IWorkflowInstanceQueryService : IApplicationService
{
    /// <summary>
    /// 分页查询实例
    /// </summary>
    Task<PageResultDtoBase<WorkflowInstanceListItemDto>> GetPageAsync(WorkflowInstancePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取实例详情（含变量、执行历史与待恢复等待点）
    /// </summary>
    Task<WorkflowInstanceDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 工作流实例命令应用服务接口
/// </summary>
public interface IWorkflowInstanceAppService : IApplicationService
{
    /// <summary>
    /// 发起实例（当前用户为发起人）
    /// </summary>
    Task<WorkflowInstanceListItemDto> StartAsync(WorkflowInstanceStartDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消实例（定义启用补偿时按执行逆序补偿）
    /// </summary>
    Task<WorkflowInstanceListItemDto> CancelAsync(WorkflowInstanceOperationDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 终止实例（不补偿）
    /// </summary>
    Task<WorkflowInstanceListItemDto> TerminateAsync(WorkflowInstanceOperationDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重试故障实例
    /// </summary>
    Task<WorkflowInstanceListItemDto> RetryAsync(WorkflowInstanceIdDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 挂起实例
    /// </summary>
    Task<WorkflowInstanceListItemDto> SuspendAsync(WorkflowInstanceOperationDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 恢复挂起实例
    /// </summary>
    Task<WorkflowInstanceListItemDto> ResumeAsync(WorkflowInstanceIdDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布信号（恢复匹配的等待信号节点）
    /// </summary>
    Task<WorkflowSignalPublishResultDto> PublishSignalAsync(WorkflowSignalPublishDto input, CancellationToken cancellationToken = default);
}
