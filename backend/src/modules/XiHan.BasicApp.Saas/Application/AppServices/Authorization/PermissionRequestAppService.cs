// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 权限申请命令应用服务
/// </summary>
/// <remarks>
/// 审批走工作流「权限申请」流程：提交即发起流程，修改即撤回旧流程并重新发起，撤回或手工终结即撤回流程。
/// 流程发起与申请写入在同一工作单元内：流程未发布、首个节点解析不到审批人等发起失败时申请一并回滚。
/// 审批通过后的自动授权由 <see cref="PermissionRequestWorkflowHandler"/> 在流程结束时完成。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限申请")]
public sealed class PermissionRequestAppService
    : SaasApplicationService, IPermissionRequestAppService
{
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionRequestDomainService _permissionRequestDomainService;
    private readonly IPermissionRequestQueryService _permissionRequestQueryService;
    private readonly IWorkflowBusinessService _workflowBusinessService;
    private readonly ISaasCacheInvalidator _cacheInvalidator;
    private readonly IImpersonationPolicyService _impersonationPolicyService;
    private readonly ISuperAdminProtector _superAdminProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionRequestAppService(
        IPermissionRequestDomainService permissionRequestDomainService,
        IPermissionRequestQueryService permissionRequestQueryService,
        IWorkflowBusinessService workflowBusinessService,
        ICurrentUser currentUser,
        ISaasCacheInvalidator cacheInvalidator,
        IImpersonationPolicyService impersonationPolicyService,
        ISuperAdminProtector superAdminProtector)
    {
        _permissionRequestDomainService = permissionRequestDomainService;
        _permissionRequestQueryService = permissionRequestQueryService;
        _workflowBusinessService = workflowBusinessService;
        _currentUser = currentUser;
        _cacheInvalidator = cacheInvalidator;
        _impersonationPolicyService = impersonationPolicyService;
        _superAdminProtector = superAdminProtector;
    }

    #region PermissionRequest

    /// <summary>
    /// 提交权限申请并发起审批流程
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Create)]
    public async Task<PermissionRequestDetailDto> CreatePermissionRequestAsync(PermissionRequestCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var requestUserId = GetCurrentUserIdOrThrow();
        var result = await _permissionRequestDomainService.CreatePermissionRequestAsync(
            PermissionRequestApplicationMapper.ToCreateCommand(input, requestUserId),
            cancellationToken);

        await StartWorkflowAsync(result.RequestId, cancellationToken);

        // 流程可能在发起时即走完（如自动通过），保守失效授权快照
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        return await GetDetailOrThrowAsync(result.RequestId, cancellationToken);
    }

    /// <summary>
    /// 撤回权限申请（同时撤回进行中的审批流程）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Withdraw)]
    public async Task DeletePermissionRequestAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestUserId = GetCurrentUserIdOrThrow();
        await _permissionRequestDomainService.WithdrawPermissionRequestAsync(id, requestUserId, cancellationToken);
        _ = await _workflowBusinessService.WithdrawAsync(
            SaasWorkflowBusinessCodes.PermissionRequest, ToBusinessId(id), "申请人撤回", cancellationToken);

        // 保守失效授权快照：撤回与审批结论可能并发，统一重建
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 修改权限申请并重新提交（撤回原审批流程后重新发起，已有的审批意见作废）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Update)]
    public async Task<PermissionRequestDetailDto> UpdatePermissionRequestAsync(PermissionRequestUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var requestUserId = GetCurrentUserIdOrThrow();
        var result = await _permissionRequestDomainService.UpdatePermissionRequestAsync(
            PermissionRequestApplicationMapper.ToUpdateCommand(input, requestUserId),
            cancellationToken);

        // 申请内容变了，审批人此前看到的不再是同一份申请：撤回旧流程、按新内容重新发起
        _ = await _workflowBusinessService.WithdrawAsync(
            SaasWorkflowBusinessCodes.PermissionRequest, ToBusinessId(result.RequestId), "申请人修改后重新提交", cancellationToken);
        await StartWorkflowAsync(result.RequestId, cancellationToken);

        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        return await GetDetailOrThrowAsync(result.RequestId, cancellationToken);
    }

    /// <summary>
    /// 手工变更权限申请状态（驳回或撤回，同时撤回进行中的审批流程；不能直接置为已批准）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Status)]
    public async Task<PermissionRequestDetailDto> UpdatePermissionRequestStatusAsync(PermissionRequestStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureOperableAsync(input.BasicId, cancellationToken);
        var result = await _permissionRequestDomainService.UpdatePermissionRequestStatusAsync(
            PermissionRequestApplicationMapper.ToStatusCommand(input, GetCurrentUserIdOrThrow()),
            cancellationToken);

        if (input.RequestStatus != PermissionRequestStatus.Pending)
        {
            _ = await _workflowBusinessService.WithdrawAsync(
                SaasWorkflowBusinessCodes.PermissionRequest,
                ToBusinessId(result.RequestId),
                string.IsNullOrWhiteSpace(input.Remark) ? "管理员变更申请状态" : input.Remark,
                cancellationToken);
        }

        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        return await GetDetailOrThrowAsync(result.RequestId, cancellationToken);
    }

    #endregion PermissionRequest

    /// <summary>
    /// 发起审批流程并回填实例主键
    /// </summary>
    private async Task StartWorkflowAsync(long requestId, CancellationToken cancellationToken)
    {
        var detail = await GetDetailOrThrowAsync(requestId, cancellationToken);
        var started = await _workflowBusinessService.StartAsync(new WorkflowBusinessStartRequest
        {
            BusinessCode = SaasWorkflowBusinessCodes.PermissionRequest,
            BusinessId = ToBusinessId(requestId),
            Title = BuildTitle(detail),
            Variables = new Dictionary<string, object?>
            {
                ["requestUserId"] = detail.RequestUserId.ToString(CultureInfo.InvariantCulture),
                ["roleId"] = detail.RoleId,
                ["roleCode"] = detail.RoleCode,
                ["permissionId"] = detail.PermissionId,
                ["permissionCode"] = detail.PermissionCode
            }
        }, cancellationToken);

        await _permissionRequestDomainService.AttachWorkflowInstanceAsync(requestId, started.InstanceId, cancellationToken);
    }

    private async Task<PermissionRequestDetailDto> GetDetailOrThrowAsync(long requestId, CancellationToken cancellationToken)
    {
        return await _permissionRequestQueryService.GetPermissionRequestDetailAsync(requestId, cancellationToken)
            ?? throw new UserFriendlyException("权限申请不存在。");
    }

    /// <summary>
    /// 手工终结前校验操作人（与审批人同一口径：不能处理自己的申请，且须有权授出申请对象）
    /// </summary>
    private async Task EnsureOperableAsync(long requestId, CancellationToken cancellationToken)
    {
        var request = await GetDetailOrThrowAsync(requestId, cancellationToken);

        if (request.RequestUserId == GetCurrentUserIdOrThrow())
        {
            throw new UserFriendlyException("不能处理自己提交的权限申请。");
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

    private long GetCurrentUserIdOrThrow()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("当前用户未登录。");
        }

        return _currentUser.UserId.Value;
    }

    private static string BuildTitle(PermissionRequestDetailDto request)
    {
        var applicant = string.IsNullOrWhiteSpace(request.RequestUserDisplayName)
            ? request.RequestUserId.ToString(CultureInfo.InvariantCulture)
            : request.RequestUserDisplayName;

        var targets = new List<string>(2);
        if (request.RoleId is > 0)
        {
            targets.Add($"角色「{request.RoleName ?? request.RoleCode ?? request.RoleId.Value.ToString(CultureInfo.InvariantCulture)}」");
        }

        if (request.PermissionId is > 0)
        {
            targets.Add($"权限「{request.PermissionName ?? request.PermissionCode ?? request.PermissionId.Value.ToString(CultureInfo.InvariantCulture)}」");
        }

        return $"权限申请：{applicant} 申请{string.Join("、", targets)}";
    }

    private static string ToBusinessId(long requestId)
    {
        return requestId.ToString(CultureInfo.InvariantCulture);
    }
}
