#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantAppService
// Guid:ed0a68f3-7ab5-4ebe-947d-08ef1d83fedc
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
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 租户命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户")]
public sealed class TenantAppService
    : SaasApplicationService, ITenantAppService
{
    private readonly ICurrentUser _currentUser;
    private readonly ITenantDomainService _tenantDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantAppService(
        ITenantDomainService tenantDomainService,
        ICurrentUser currentUser)
    {
        _tenantDomainService = tenantDomainService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Tenant.Create)]
    public async Task<TenantDetailDto> CreateTenantAsync(TenantCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantDomainService.CreateTenantAsync(ToCreateCommand(input), cancellationToken);
        return TenantApplicationMapper.ToDetailDto(result.Tenant, result.Now);
    }

    /// <summary>
    /// 更新租户基础资料
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Tenant.Update)]
    public async Task<TenantDetailDto> UpdateTenantAsync(TenantUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantDomainService.UpdateTenantAsync(ToUpdateCommand(input), cancellationToken);
        return TenantApplicationMapper.ToDetailDto(result.Tenant, result.Now);
    }

    /// <summary>
    /// 更新租户状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Tenant.Status)]
    public async Task<TenantDetailDto> UpdateTenantStatusAsync(TenantStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantDomainService.UpdateTenantStatusAsync(
            new TenantStatusChangeCommand(input.BasicId, input.TenantStatus, input.Reason, _currentUser.UserId),
            cancellationToken);
        return TenantApplicationMapper.ToDetailDto(result.Tenant, result.Now);
    }

    /// <summary>
    /// 撤销租户成员
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Revoke)]
    public async Task DeleteTenantMemberAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _tenantDomainService.DeleteTenantMemberAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新租户成员
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Update)]
    public async Task<TenantMemberDetailDto> UpdateTenantMemberAsync(TenantMemberUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantDomainService.UpdateTenantMemberAsync(ToMemberUpdateCommand(input), cancellationToken);
        return TenantMemberApplicationMapper.ToDetailDto(result.Member, result.Now);
    }

    /// <summary>
    /// 更新租户成员邀请状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.InviteStatus)]
    public async Task<TenantMemberDetailDto> UpdateTenantMemberInviteStatusAsync(TenantMemberInviteStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantDomainService.UpdateTenantMemberInviteStatusAsync(
            new TenantMemberInviteStatusChangeCommand(input.BasicId, input.InviteStatus, input.InviteRemark),
            cancellationToken);
        return TenantMemberApplicationMapper.ToDetailDto(result.Member, result.Now);
    }

    /// <summary>
    /// 更新租户成员状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Status)]
    public async Task<TenantMemberDetailDto> UpdateTenantMemberStatusAsync(TenantMemberStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantDomainService.UpdateTenantMemberStatusAsync(
            new TenantMemberStatusChangeCommand(input.BasicId, input.Status, input.Remark),
            cancellationToken);
        return TenantMemberApplicationMapper.ToDetailDto(result.Member, result.Now);
    }

    private static TenantCreateCommand ToCreateCommand(TenantCreateDto input)
    {
        return new TenantCreateCommand(
            input.TenantCode,
            input.TenantName,
            input.TenantShortName,
            input.Logo,
            input.Domain,
            input.EditionId,
            input.IsolationMode,
            input.ExpireTime,
            input.UserLimit,
            input.StorageLimit,
            input.Sort,
            input.Remark);
    }

    private static TenantMemberUpdateCommand ToMemberUpdateCommand(TenantMemberUpdateDto input)
    {
        return new TenantMemberUpdateCommand(
            input.BasicId,
            input.MemberType,
            input.EffectiveTime,
            input.ExpirationTime,
            input.DisplayName,
            input.InviteRemark,
            input.Remark);
    }

    private static TenantUpdateCommand ToUpdateCommand(TenantUpdateDto input)
    {
        return new TenantUpdateCommand(
            input.BasicId,
            input.TenantName,
            input.TenantShortName,
            input.Logo,
            input.Domain,
            input.EditionId,
            input.IsolationMode,
            input.ExpireTime,
            input.UserLimit,
            input.StorageLimit,
            input.Sort,
            input.Remark);
    }
}
