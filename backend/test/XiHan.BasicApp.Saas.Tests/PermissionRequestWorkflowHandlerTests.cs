// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限申请审批处理器测试（审批人校验、结论回写、默认流程模板）。
/// </summary>
/// <remarks>
/// 权限申请的审批人由流程配置决定，越权防线只剩处理器：同意前校验审批人不是申请人本人、且有权授出申请对象；
/// 流程以 approved 结束才授权。任何一条松动都会让人借流程给自己授权。
/// </remarks>
public sealed class PermissionRequestWorkflowHandlerTests
{
    private readonly Mock<IPermissionRequestDomainService> _domain = new();
    private readonly Mock<IPermissionRequestQueryService> _query = new();
    private readonly Mock<IImpersonationPolicyService> _impersonation = new();
    private readonly Mock<ISuperAdminProtector> _superAdmin = new();
    private readonly Mock<ISaasCacheInvalidator> _cache = new();
    private readonly Mock<IAuthorizationChangeNotifier> _notifier = new();

    /// <summary>
    /// 审批人就是申请人时拒绝同意。
    /// </summary>
    [Fact]
    public async Task OnTaskCompleting_ApproveOwnRequest_ShouldThrow()
    {
        SetupRequest(requestUserId: 100L, roleId: 7L);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => CreateHandler().OnTaskCompletingAsync(CreateTaskContext(actorUserId: 100L, WorkflowUserTaskOutcomes.Approved)));

        Assert.Contains("不能审批自己", exception.Message, StringComparison.Ordinal);
        _superAdmin.Verify(value => value.EnsureCanAssignRoleAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 同意前按手工授权同一口径校验审批人能否授出申请的角色（超管保护 + 模仿权限准入）。
    /// </summary>
    [Fact]
    public async Task OnTaskCompleting_Approve_ShouldEnsureApproverCanGrantRole()
    {
        SetupRequest(requestUserId: 100L, roleId: 7L);

        await CreateHandler().OnTaskCompletingAsync(CreateTaskContext(actorUserId: 200L, WorkflowUserTaskOutcomes.Approved));

        _superAdmin.Verify(value => value.EnsureCanAssignRoleAsync(7L, It.IsAny<CancellationToken>()), Times.Once);
        _impersonation.Verify(value => value.EnsureCanGrantRoleIdsAsync(It.Is<IReadOnlyCollection<long>>(ids => ids.Single() == 7L), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 驳回不需要授出能力，也不查申请（申请人本人驳回等价于撤回，交给流程配置决定是否允许）。
    /// </summary>
    [Fact]
    public async Task OnTaskCompleting_Reject_ShouldSkipGrantChecks()
    {
        await CreateHandler().OnTaskCompletingAsync(CreateTaskContext(actorUserId: 100L, WorkflowUserTaskOutcomes.Rejected));

        _query.Verify(value => value.GetPermissionRequestDetailAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 流程以 approved 结束：自动授权、失效授权快照并记授权变更。
    /// </summary>
    [Fact]
    public async Task OnOutcome_Approved_ShouldGrantAndInvalidate()
    {
        _domain
            .Setup(value => value.ApplyApprovalAsync(12L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionRequestCommandResult(12L, 100L, 7L, null));

        await CreateHandler().OnOutcomeAsync(CreateOutcomeContext(WorkflowUserTaskOutcomes.Approved));

        _cache.Verify(value => value.InvalidateAuthorizationAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.Verify(value => value.NotifyAsync(
            PermissionChangeType.UserAssignRole, 100L, 7L, null, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()), Times.Once);
        _domain.Verify(value => value.ApplyRejectionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 流程以 rejected 结束只置为驳回，不授权。
    /// </summary>
    [Fact]
    public async Task OnOutcome_Rejected_ShouldOnlyReject()
    {
        await CreateHandler().OnOutcomeAsync(CreateOutcomeContext(WorkflowUserTaskOutcomes.Rejected));

        _domain.Verify(value => value.ApplyRejectionAsync(12L, It.IsAny<CancellationToken>()), Times.Once);
        _domain.Verify(value => value.ApplyApprovalAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 未知结论不得被当成通过或驳回。
    /// </summary>
    [Fact]
    public async Task OnOutcome_UnknownOutcome_ShouldThrow()
    {
        _ = await Assert.ThrowsAsync<BusinessException>(() => CreateHandler().OnOutcomeAsync(CreateOutcomeContext("returned")));

        _domain.Verify(value => value.ApplyApprovalAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        _domain.Verify(value => value.ApplyRejectionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 流程被撤回、取消或终止：待审批的申请置为已撤回。
    /// </summary>
    [Fact]
    public async Task OnAborted_ShouldMarkRequestAborted()
    {
        await CreateHandler().OnAbortedAsync(new WorkflowBusinessAbortedContext
        {
            BusinessCode = SaasWorkflowBusinessCodes.PermissionRequest,
            BusinessId = "12",
            InstanceId = 1L,
            Status = WorkflowInstanceStatus.Terminated,
            Reason = "管理员终止"
        });

        _domain.Verify(value => value.MarkAbortedAsync(12L, "管理员终止", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 默认流程模板：编码与业务一致，审批节点按系统角色找人且剔除申请人，通过/驳回各有结果结束节点。
    /// </summary>
    [Fact]
    public void Descriptor_DefaultDefinition_ShouldMatchBusinessContract()
    {
        var descriptor = CreateHandler().Descriptor;

        var definition = descriptor.DefaultDefinition!();

        Assert.Equal(SaasWorkflowBusinessCodes.PermissionRequest, definition.Code, StringComparer.Ordinal);
        var approve = Assert.Single(definition.Nodes, node => node.ActivityType == WorkflowActivityTypes.UserTask);
        Assert.Equal(true, approve.Properties["ExcludeStarter"]);
        var outcomes = definition.Nodes
            .Where(node => node.ActivityType == WorkflowActivityTypes.End)
            .Select(node => node.Properties["Outcome"] as string)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.Equal([WorkflowUserTaskOutcomes.Approved, WorkflowUserTaskOutcomes.Rejected], outcomes);
        Assert.Equal("identity/authorization/permission-request-approval", descriptor.FormComponent, StringComparer.Ordinal);
    }

    private PermissionRequestWorkflowHandler CreateHandler()
    {
        return new PermissionRequestWorkflowHandler(
            _domain.Object,
            _query.Object,
            _impersonation.Object,
            _superAdmin.Object,
            _cache.Object,
            _notifier.Object);
    }

    private void SetupRequest(long requestUserId, long? roleId = null, long? permissionId = null)
    {
        _query
            .Setup(value => value.GetPermissionRequestDetailAsync(12L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionRequestDetailDto
            {
                BasicId = 12L,
                RequestUserId = requestUserId,
                RoleId = roleId,
                PermissionId = permissionId
            });
    }

    private static WorkflowBusinessTaskContext CreateTaskContext(long actorUserId, string outcome)
    {
        return new WorkflowBusinessTaskContext
        {
            BusinessCode = SaasWorkflowBusinessCodes.PermissionRequest,
            BusinessId = "12",
            InstanceId = 1L,
            TaskId = "2",
            NodeId = "admin-approve",
            ActorUserId = actorUserId,
            Outcome = outcome
        };
    }

    private static WorkflowBusinessOutcomeContext CreateOutcomeContext(string outcome)
    {
        return new WorkflowBusinessOutcomeContext
        {
            BusinessCode = SaasWorkflowBusinessCodes.PermissionRequest,
            BusinessId = "12",
            InstanceId = 1L,
            Outcome = outcome,
            Variables = new Dictionary<string, object?>()
        };
    }
}
