// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Workflow.Application.Contracts;

/// <summary>
/// 审批中心查询应用服务接口（我的已办 / 抄送我的 / 我发起的 / 审批详情，主体服务端锁定为当前用户）
/// </summary>
public interface IWorkflowApprovalQueryService : IApplicationService
{
    /// <summary>
    /// 分页查询我的已办（办理、转办、加签过的任务）
    /// </summary>
    Task<PageResultDtoBase<WorkflowApprovalRecordItemDto>> GetDonePageAsync(WorkflowApprovalPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询抄送我的
    /// </summary>
    Task<PageResultDtoBase<WorkflowApprovalRecordItemDto>> GetCcPageAsync(WorkflowApprovalPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询我发起的
    /// </summary>
    Task<PageResultDtoBase<WorkflowApprovalInstanceItemDto>> GetStartedPageAsync(WorkflowApprovalPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 审批详情（发起人、审批参与人、被抄送人或拥有流程查看权限者可看）
    /// </summary>
    /// <param name="instanceId">流程实例主键</param>
    /// <param name="taskId">待办任务标识（从待办打开时传入，详情据此返回办理所需的任务信息）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<WorkflowApprovalDetailDto> GetDetailAsync(long instanceId, string? taskId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 审批中心命令应用服务接口
/// </summary>
public interface IWorkflowApprovalAppService : IApplicationService
{
    /// <summary>
    /// 发起人撤回进行中的流程（审批业务同时回写单据）
    /// </summary>
    Task WithdrawAsync(WorkflowApprovalWithdrawDto input, CancellationToken cancellationToken = default);
}

/// <summary>
/// 审批人选择查询应用服务接口（设计器配置审批人、待办转办/加签选人）
/// </summary>
public interface IWorkflowDirectoryQueryService : IApplicationService
{
    /// <summary>
    /// 用户选项（当前上下文可见的启用用户；按关键字搜索或按主键回显）
    /// </summary>
    Task<List<WorkflowUserOptionDto>> GetUserOptionsAsync(WorkflowUserOptionQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 角色选项（启用角色，按编码引用）
    /// </summary>
    Task<List<WorkflowOrganizationOptionDto>> GetRoleOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 岗位选项（启用岗位，按编码引用）
    /// </summary>
    Task<List<WorkflowOrganizationOptionDto>> GetPositionOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 部门树（启用部门，按编码引用）
    /// </summary>
    Task<List<WorkflowDepartmentOptionDto>> GetDepartmentTreeAsync(CancellationToken cancellationToken = default);
}
