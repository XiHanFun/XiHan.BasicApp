#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDelegationAppService
// Guid:1a7c4e9b-8f0d-4a2c-3d4e-7f5a6b8c9d0e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 权限委托命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限委托")]
public sealed class PermissionDelegationAppService
    : SaasApplicationService, IPermissionDelegationAppService
{
    /// <summary>
    /// 权限委托领域服务
    /// </summary>
    private readonly IPermissionDelegationDomainService _permissionDelegationDomainService;

    /// <summary>
    /// 权限委托查询服务
    /// </summary>
    private readonly IPermissionDelegationQueryService _permissionDelegationQueryService;

    /// <summary>
    /// 缓存失效器
    /// </summary>
    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 授权变更通知器
    /// </summary>
    private readonly IAuthorizationChangeNotifier _authorizationChangeNotifier;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionDelegationAppService(
        IPermissionDelegationDomainService permissionDelegationDomainService,
        IPermissionDelegationQueryService permissionDelegationQueryService,
        ISaasCacheInvalidator cacheInvalidator,
        IAuthorizationChangeNotifier authorizationChangeNotifier)
    {
        _permissionDelegationDomainService = permissionDelegationDomainService;
        _permissionDelegationQueryService = permissionDelegationQueryService;
        _cacheInvalidator = cacheInvalidator;
        _authorizationChangeNotifier = authorizationChangeNotifier;
    }

    /// <summary>
    /// 委托变更留痕：生效/待生效=获得委托授权，已撤销/已过期=委托授权收回。
    /// </summary>
    private Task NotifyDelegationChangeAsync(PermissionDelegationCommandResult result, CancellationToken cancellationToken)
    {
        return _authorizationChangeNotifier.NotifyAsync(
            result.IsActive ? PermissionChangeType.UserDelegateGrant : PermissionChangeType.UserDelegateRevoke,
            targetUserId: result.DelegateeUserId,
            targetRoleId: result.RoleId,
            permissionId: result.PermissionId,
            cancellationToken: cancellationToken);
    }

    #region PermissionDelegation

    /// <summary>
    /// 创建权限委托
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Create)]
    public async Task<PermissionDelegationDetailDto> CreatePermissionDelegationAsync(PermissionDelegationCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionDelegationDomainService.CreatePermissionDelegationAsync(PermissionDelegationApplicationMapper.ToCreateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await NotifyDelegationChangeAsync(result, cancellationToken);

        return await _permissionDelegationQueryService.GetPermissionDelegationDetailAsync(result.DelegationId, cancellationToken)
            ?? throw new InvalidOperationException("权限委托不存在。");
    }

    /// <summary>
    /// 撤销权限委托
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Revoke)]
    public async Task DeletePermissionDelegationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionDelegationDomainService.RevokePermissionDelegationAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await NotifyDelegationChangeAsync(result, cancellationToken);
    }

    /// <summary>
    /// 更新权限委托
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Update)]
    public async Task<PermissionDelegationDetailDto> UpdatePermissionDelegationAsync(PermissionDelegationUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionDelegationDomainService.UpdatePermissionDelegationAsync(PermissionDelegationApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await NotifyDelegationChangeAsync(result, cancellationToken);

        return await _permissionDelegationQueryService.GetPermissionDelegationDetailAsync(result.DelegationId, cancellationToken)
            ?? throw new InvalidOperationException("权限委托不存在。");
    }

    /// <summary>
    /// 更新权限委托状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Status)]
    public async Task<PermissionDelegationDetailDto> UpdatePermissionDelegationStatusAsync(PermissionDelegationStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionDelegationDomainService.UpdatePermissionDelegationStatusAsync(PermissionDelegationApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await NotifyDelegationChangeAsync(result, cancellationToken);

        return await _permissionDelegationQueryService.GetPermissionDelegationDetailAsync(result.DelegationId, cancellationToken)
            ?? throw new InvalidOperationException("权限委托不存在。");
    }

    #endregion PermissionDelegation
}
