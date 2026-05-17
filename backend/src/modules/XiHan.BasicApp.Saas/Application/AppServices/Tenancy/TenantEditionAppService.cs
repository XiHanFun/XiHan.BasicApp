#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionAppService
// Guid:8b060026-a796-44ad-b557-ff3b44b99c59
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
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

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantEditionAppService(ITenantEditionDomainService tenantEditionDomainService)
    {
        _tenantEditionDomainService = tenantEditionDomainService;
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

        var result = await _tenantEditionDomainService.CreateTenantEditionAsync(ToCreateCommand(input), cancellationToken);
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
            new TenantEditionDefaultChangeCommand(input.BasicId),
            cancellationToken);
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

        var result = await _tenantEditionDomainService.UpdateTenantEditionAsync(ToUpdateCommand(input), cancellationToken);
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
            new TenantEditionStatusChangeCommand(input.BasicId, input.Status),
            cancellationToken);
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
            new TenantEditionPermissionGrantCommand(input.EditionId, input.PermissionId, input.Remark),
            cancellationToken);
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
        await _tenantEditionDomainService.RevokeTenantEditionPermissionAsync(id, cancellationToken);
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
            new TenantEditionPermissionStatusChangeCommand(input.BasicId, input.Status, input.Remark),
            cancellationToken);
        return TenantEditionPermissionApplicationMapper.ToDetailDto(result.EditionPermission, result.Permission);
    }

    private static TenantEditionCreateCommand ToCreateCommand(TenantEditionCreateDto input)
    {
        return new TenantEditionCreateCommand(
            input.EditionCode,
            input.EditionName,
            input.Description,
            input.UserLimit,
            input.StorageLimit,
            input.Price,
            input.BillingPeriodMonths,
            input.IsFree,
            input.IsDefault,
            input.Status,
            input.Sort,
            input.Remark);
    }

    private static TenantEditionUpdateCommand ToUpdateCommand(TenantEditionUpdateDto input)
    {
        return new TenantEditionUpdateCommand(
            input.BasicId,
            input.EditionName,
            input.Description,
            input.UserLimit,
            input.StorageLimit,
            input.Price,
            input.BillingPeriodMonths,
            input.IsFree,
            input.IsDefault,
            input.Sort,
            input.Remark);
    }
}
