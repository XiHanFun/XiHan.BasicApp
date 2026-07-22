// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 租户版本命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户版本")]
public sealed class TenantEditionAppService
    : SaasApplicationService, ITenantEditionAppService
{
    private readonly ITenantEditionDomainService _tenantEditionDomainService;

    private readonly ITenantProvisionDomainService _tenantProvisionDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantEditionAppService(
        ITenantEditionDomainService tenantEditionDomainService,
        ITenantProvisionDomainService tenantProvisionDomainService,
        ISaasCacheInvalidator cacheInvalidator)
    {
        _tenantEditionDomainService = tenantEditionDomainService;
        _tenantProvisionDomainService = tenantProvisionDomainService;
        _cacheInvalidator = cacheInvalidator;
    }

    /// <summary>
    /// 创建租户版本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Create)]
    public async Task<TenantEditionDetailDto> CreateTenantEditionAsync(TenantEditionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantEditionDomainService.CreateTenantEditionAsync(TenantEditionApplicationMapper.ToCreateCommand(input), cancellationToken);

        // 版本变更影响已启用版本列表缓存，统一失效
        await _cacheInvalidator.InvalidateTenantEditionAsync(cancellationToken);

        return TenantEditionApplicationMapper.ToDetailDto(result.Edition);
    }

    /// <summary>
    /// 设置默认租户版本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Default)]
    public async Task<TenantEditionDetailDto> UpdateDefaultTenantEditionAsync(TenantEditionDefaultUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantEditionDomainService.UpdateDefaultTenantEditionAsync(
            TenantEditionApplicationMapper.ToDefaultCommand(input),
            cancellationToken);

        // 默认版本变更影响已启用版本列表排序，统一失效
        await _cacheInvalidator.InvalidateTenantEditionAsync(cancellationToken);

        return TenantEditionApplicationMapper.ToDetailDto(result.Edition);
    }

    /// <summary>
    /// 更新租户版本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Update)]
    public async Task<TenantEditionDetailDto> UpdateTenantEditionAsync(TenantEditionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantEditionDomainService.UpdateTenantEditionAsync(TenantEditionApplicationMapper.ToUpdateCommand(input), cancellationToken);

        // 版本变更影响已启用版本列表缓存，统一失效
        await _cacheInvalidator.InvalidateTenantEditionAsync(cancellationToken);

        return TenantEditionApplicationMapper.ToDetailDto(result.Edition);
    }

    /// <summary>
    /// 更新租户版本状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEdition.Status)]
    public async Task<TenantEditionDetailDto> UpdateTenantEditionStatusAsync(TenantEditionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantEditionDomainService.UpdateTenantEditionStatusAsync(
            TenantEditionApplicationMapper.ToStatusCommand(input),
            cancellationToken);

        // 版本启停直接影响已启用版本列表，统一失效
        await _cacheInvalidator.InvalidateTenantEditionAsync(cancellationToken);

        return TenantEditionApplicationMapper.ToDetailDto(result.Edition);
    }

    /// <summary>
    /// 授予租户版本权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Grant)]
    public async Task<TenantEditionPermissionDetailDto> GrantTenantEditionPermissionAsync(TenantEditionPermissionGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantEditionDomainService.GrantTenantEditionPermissionAsync(
            TenantEditionPermissionApplicationMapper.ToGrantCommand(input),
            cancellationToken);
        // 版本白名单变更：失效版本门控缓存（鉴权快照热路径，事务提交后生效）
        await _cacheInvalidator.InvalidateEditionGateAsync(cancellationToken);
        return TenantEditionPermissionApplicationMapper.ToDetailDto(result.EditionPermission, result.Permission);
    }

    /// <summary>
    /// 撤销租户版本权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Revoke)]
    public async Task RevokeTenantEditionPermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _tenantEditionDomainService.RevokeTenantEditionPermissionAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateEditionGateAsync(cancellationToken);
        // 白名单收窄：回收该版本下各租户超出白名单的存量角色/用户直授权限行（REQ-5.3）
        _ = await _tenantProvisionDomainService.ReconcileEditionTenantsAuthorizationAsync(result.EditionPermission.EditionId, cancellationToken);
    }

    /// <summary>
    /// 更新租户版本权限状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Update)]
    public async Task<TenantEditionPermissionDetailDto> UpdateTenantEditionPermissionStatusAsync(TenantEditionPermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantEditionDomainService.UpdateTenantEditionPermissionStatusAsync(
            TenantEditionPermissionApplicationMapper.ToStatusCommand(input),
            cancellationToken);
        await _cacheInvalidator.InvalidateEditionGateAsync(cancellationToken);

        // 映射停用等同白名单收窄：回收该版本下各租户的越界存量授权（REQ-5.3）；恢复有效无需回收
        if (result.EditionPermission.Status != ValidityStatus.Valid)
        {
            _ = await _tenantProvisionDomainService.ReconcileEditionTenantsAuthorizationAsync(result.EditionPermission.EditionId, cancellationToken);
        }

        return TenantEditionPermissionApplicationMapper.ToDetailDto(result.EditionPermission, result.Permission);
    }
}
