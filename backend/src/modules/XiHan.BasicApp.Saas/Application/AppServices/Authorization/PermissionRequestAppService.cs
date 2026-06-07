#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestAppService
// Guid:3c9e6a1d-0b2f-4c4e-5f6a-9b7c8d0e1f2a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 权限申请命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限申请")]
public sealed class PermissionRequestAppService
    : SaasApplicationService, IPermissionRequestAppService
{
    /// <summary>
    /// 当前用户
    /// </summary>
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 权限申请领域服务
    /// </summary>
    private readonly IPermissionRequestDomainService _permissionRequestDomainService;

    /// <summary>
    /// 权限申请查询服务
    /// </summary>
    private readonly IPermissionRequestQueryService _permissionRequestQueryService;

    /// <summary>
    /// SaaS 缓存失效器
    /// </summary>
    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionRequestAppService(
        IPermissionRequestDomainService permissionRequestDomainService,
        IPermissionRequestQueryService permissionRequestQueryService,
        ICurrentUser currentUser,
        ISaasCacheInvalidator cacheInvalidator)
    {
        _permissionRequestDomainService = permissionRequestDomainService;
        _permissionRequestQueryService = permissionRequestQueryService;
        _currentUser = currentUser;
        _cacheInvalidator = cacheInvalidator;
    }

    #region PermissionRequest

    /// <summary>
    /// 创建权限申请
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

        // 保守失效授权快照：避免自动审批等场景下创建即授权造成脏权限
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        return await _permissionRequestQueryService.GetPermissionRequestDetailAsync(result.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

    /// <summary>
    /// 撤回权限申请
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Withdraw)]
    public async Task DeletePermissionRequestAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestUserId = GetCurrentUserIdOrThrow();
        await _permissionRequestDomainService.WithdrawPermissionRequestAsync(id, requestUserId, cancellationToken);

        // 保守失效授权快照：撤回可能回收已生效权限，需重建快照
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 更新权限申请
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

        // 保守失效授权快照：更新可能调整申请的目标角色/权限范围，影响最终授权
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        return await _permissionRequestQueryService.GetPermissionRequestDetailAsync(result.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

    /// <summary>
    /// 更新权限申请状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Status)]
    public async Task<PermissionRequestDetailDto> UpdatePermissionRequestStatusAsync(PermissionRequestStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionRequestDomainService.UpdatePermissionRequestStatusAsync(
            PermissionRequestApplicationMapper.ToStatusCommand(input, GetCurrentUserIdOrThrow()),
            cancellationToken);

        // 失效授权快照：状态流转可能进入已批准并授权，必须重建快照
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        return await _permissionRequestQueryService.GetPermissionRequestDetailAsync(result.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

    /// <summary>
    /// 审批通过权限申请（自动授予角色/权限）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Status)]
    public async Task<PermissionRequestDetailDto> ApprovePermissionRequestAsync(PermissionRequestApprovalDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionRequestDomainService.ApprovePermissionRequestAsync(
            PermissionRequestApplicationMapper.ToApprovalCommand(input, GetCurrentUserIdOrThrow()),
            cancellationToken);

        // 失效授权快照：审批通过会自动授予角色/权限，必须重建快照避免脏权限
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        return await _permissionRequestQueryService.GetPermissionRequestDetailAsync(result.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

    /// <summary>
    /// 驳回权限申请
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Status)]
    public async Task<PermissionRequestDetailDto> RejectPermissionRequestAsync(PermissionRequestApprovalDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionRequestDomainService.RejectPermissionRequestAsync(
            PermissionRequestApplicationMapper.ToApprovalCommand(input, GetCurrentUserIdOrThrow()),
            cancellationToken);

        // 保守失效授权快照：驳回理论上不授权，但状态变更可能撤销先前临时授权，多失效无害
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        return await _permissionRequestQueryService.GetPermissionRequestDetailAsync(result.RequestId, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

    /// <summary>
    /// 获取当前用户主键
    /// </summary>
    private long GetCurrentUserIdOrThrow()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("当前用户未登录。");
        }

        return _currentUser.UserId.Value;
    }

    #endregion PermissionRequest
}
