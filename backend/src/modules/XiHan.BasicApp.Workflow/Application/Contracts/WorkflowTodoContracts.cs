#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowTodoContracts
// Guid:d92c67e5-04a8-4f31-b6d9-58e13c70f2a4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:23:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Workflow.Application.Contracts;

/// <summary>
/// 我的待办查询应用服务接口
/// </summary>
public interface IWorkflowTodoQueryService : IApplicationService
{
    /// <summary>
    /// 分页查询当前用户待办（受理人归属服务端锁定为当前用户）
    /// </summary>
    Task<PageResultDtoBase<WorkflowTodoListItemDto>> GetPageAsync(WorkflowTodoPageQueryDto input, CancellationToken cancellationToken = default);
}

/// <summary>
/// 我的待办命令应用服务接口（办理人服务端锁定为当前用户，无需额外权限码）
/// </summary>
public interface IWorkflowTodoAppService : IApplicationService
{
    /// <summary>
    /// 办理待办（同意/拒绝/自定义结果）
    /// </summary>
    Task<WorkflowTodoCompleteResultDto> CompleteAsync(WorkflowTodoCompleteDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 转办待办
    /// </summary>
    Task TransferAsync(WorkflowTodoTransferDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加签
    /// </summary>
    Task AddAssigneesAsync(WorkflowTodoAddAssigneesDto input, CancellationToken cancellationToken = default);
}
