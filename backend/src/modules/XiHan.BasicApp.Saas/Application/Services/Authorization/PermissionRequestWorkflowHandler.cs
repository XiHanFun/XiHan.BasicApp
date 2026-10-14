// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Application.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Builders;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 权限申请审批处理器（权限申请接入工作流）
/// </summary>
/// <remarks>
/// 申请提交即发起 <see cref="SaasWorkflowBusinessCodes.PermissionRequest"/> 流程，审批人按流程定义配置；
/// 每位审批人点「同意」前在其请求上下文里校验：不能审批自己的申请，且本人必须有权授出申请的角色/权限
/// （模仿权限准入、超管角色保护与手工授权同一口径）。流程以 approved 结束即自动授权，以 rejected 结束即驳回。
/// </remarks>
public sealed class PermissionRequestWorkflowHandler : IWorkflowBusinessHandler
{
    private static readonly WorkflowBusinessDescriptor PermissionRequestDescriptor = new()
    {
        Code = SaasWorkflowBusinessCodes.PermissionRequest,
        Name = "权限申请",
        Description = "用户申请角色或权限，流程审批通过后自动授予",
        FormComponent = "identity/authorization/permission-request-approval",
        Variables =
        [
            new("requestUserId", "申请人", "user", "申请人用户主键"),
            new("roleId", "申请角色", "number", "未申请角色时为空"),
            new("roleCode", "申请角色编码", "string", "未申请角色时为空"),
            new("permissionId", "申请权限", "number", "未申请权限时为空"),
            new("permissionCode", "申请权限编码", "string", "未申请权限时为空")
        ],
        DefaultDefinition = BuildDefaultDefinition
    };

    private readonly IPermissionRequestDomainService _permissionRequestDomainService;
    private readonly IPermissionRequestQueryService _permissionRequestQueryService;
    private readonly IImpersonationPolicyService _impersonationPolicyService;
    private readonly ISuperAdminProtector _superAdminProtector;
    private readonly ISaasCacheInvalidator _cacheInvalidator;
    private readonly IAuthorizationChangeNotifier _authorizationChangeNotifier;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionRequestWorkflowHandler(
        IPermissionRequestDomainService permissionRequestDomainService,
        IPermissionRequestQueryService permissionRequestQueryService,
        IImpersonationPolicyService impersonationPolicyService,
        ISuperAdminProtector superAdminProtector,
        ISaasCacheInvalidator cacheInvalidator,
        IAuthorizationChangeNotifier authorizationChangeNotifier)
    {
        _permissionRequestDomainService = permissionRequestDomainService;
        _permissionRequestQueryService = permissionRequestQueryService;
        _impersonationPolicyService = impersonationPolicyService;
        _superAdminProtector = superAdminProtector;
        _cacheInvalidator = cacheInvalidator;
        _authorizationChangeNotifier = authorizationChangeNotifier;
    }

    /// <summary>
    /// 业务描述
    /// </summary>
    public WorkflowBusinessDescriptor Descriptor => PermissionRequestDescriptor;

    /// <summary>
    /// 同意前校验审批人：不能审批自己的申请，且必须有权授出申请的角色/权限
    /// </summary>
    public async Task OnTaskCompletingAsync(WorkflowBusinessTaskContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!string.Equals(context.Outcome, WorkflowUserTaskOutcomes.Approved, StringComparison.Ordinal))
        {
            return;
        }

        var request = await _permissionRequestQueryService.GetPermissionRequestDetailAsync(ParseId(context.BusinessId), cancellationToken)
            ?? throw new UserFriendlyException("权限申请不存在。");

        if (request.RequestUserId == context.ActorUserId)
        {
            throw new UserFriendlyException("不能审批自己提交的权限申请。");
        }

        if (request.PermissionId is > 0)
        {
            await _impersonationPolicyService.EnsureCanGrantPermissionIdsAsync([request.PermissionId.Value], cancellationToken);
        }

        if (request.RoleId is > 0)
        {
            await _superAdminProtector.EnsureCanAssignRoleAsync(request.RoleId.Value, cancellationToken);
            await _impersonationPolicyService.EnsureCanGrantRoleIdsAsync([request.RoleId.Value], cancellationToken);
        }
    }

    /// <summary>
    /// 流程结论回写：通过即自动授权，驳回即置为已驳回
    /// </summary>
    public async Task OnOutcomeAsync(WorkflowBusinessOutcomeContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestId = ParseId(context.BusinessId);
        switch (context.Outcome)
        {
            case WorkflowUserTaskOutcomes.Approved:
            {
                var result = await _permissionRequestDomainService.ApplyApprovalAsync(requestId, cancellationToken);
                await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
                await NotifyGrantedAsync(result, cancellationToken);
                break;
            }

            case WorkflowUserTaskOutcomes.Rejected:
                _ = await _permissionRequestDomainService.ApplyRejectionAsync(requestId, cancellationToken);
                break;

            default:
                throw new BusinessException(message: $"权限申请流程不支持结果 {context.Outcome}（仅支持 approved / rejected）");
        }
    }

    /// <summary>
    /// 流程中止回写：待审批的申请置为已撤回
    /// </summary>
    public async Task OnAbortedAsync(WorkflowBusinessAbortedContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _permissionRequestDomainService.MarkAbortedAsync(
            ParseId(context.BusinessId),
            string.IsNullOrWhiteSpace(context.Reason) ? "审批流程已中止" : context.Reason,
            cancellationToken);
    }

    /// <summary>
    /// 审批抽屉展示的申请详情
    /// </summary>
    public async Task<object?> GetFormDataAsync(string businessId, CancellationToken cancellationToken = default)
    {
        return await _permissionRequestQueryService.GetPermissionRequestDetailAsync(ParseId(businessId), cancellationToken);
    }

    private async Task NotifyGrantedAsync(PermissionRequestCommandResult result, CancellationToken cancellationToken)
    {
        if (result.GrantedUserId is not > 0)
        {
            return;
        }

        var reason = $"权限申请[{result.RequestId}]审批通过自动授权";
        if (result.GrantedRoleId is > 0)
        {
            await _authorizationChangeNotifier.NotifyAsync(
                PermissionChangeType.UserAssignRole,
                targetUserId: result.GrantedUserId,
                targetRoleId: result.GrantedRoleId,
                permissionId: null,
                reason: reason,
                cancellationToken: cancellationToken);
        }

        if (result.GrantedPermissionId is > 0)
        {
            await _authorizationChangeNotifier.NotifyAsync(
                PermissionChangeType.UserGrantPermission,
                targetUserId: result.GrantedUserId,
                targetRoleId: null,
                permissionId: result.GrantedPermissionId,
                reason: reason,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 默认流程：租户所有者（平台为超级管理员）审批，申请人不审批自己的申请
    /// </summary>
    private static WorkflowDefinition BuildDefaultDefinition()
    {
        return WorkflowDefinitionBuilder.Create(SaasWorkflowBusinessCodes.PermissionRequest, "权限申请")
            .WithDescription("默认流程：租户所有者（平台为超级管理员）审批，申请人不审批自己的申请；可在流程定义里新建版本调整")
            .AddStart()
            .AddUserTask("admin-approve", "管理员审批", node => node
                .WithAssigneeRule(OrganizationAssigneeRuleTypes.Role, SaasRoleCodes.TenantOwner, SaasRoleCodes.SuperAdmin)
                .WithProperty("ExcludeStarter", true))
            .AddOutcomeEnd("end-approved", WorkflowUserTaskOutcomes.Approved, "通过")
            .AddOutcomeEnd("end-rejected", WorkflowUserTaskOutcomes.Rejected, "驳回")
            .AddTransition("start", "admin-approve")
            .AddTransition("admin-approve", "end-approved", "outcome == 'approved'")
            .AddTransition("admin-approve", "end-rejected", "outcome == 'rejected'")
            .Build();
    }

    private static long ParseId(string businessId)
    {
        return long.TryParse(businessId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0
            ? id
            : throw new BusinessException(message: $"权限申请标识 {businessId} 无效");
    }
}
