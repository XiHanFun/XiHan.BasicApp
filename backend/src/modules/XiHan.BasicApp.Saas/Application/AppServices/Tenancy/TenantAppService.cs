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
using XiHan.Framework.Security.Password;
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
    private readonly ITenantProvisionDomainService _tenantProvisionDomainService;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantAppService(
        ITenantDomainService tenantDomainService,
        ITenantProvisionDomainService tenantProvisionDomainService,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser)
    {
        _tenantDomainService = tenantDomainService;
        _tenantProvisionDomainService = tenantProvisionDomainService;
        _passwordHasher = passwordHasher;
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

        var result = await _tenantDomainService.CreateTenantAsync(TenantApplicationMapper.ToCreateCommand(input), cancellationToken);

        // 同时提供管理员账号信息时，一站式开通：管理员 + Owner 角色 + 按版本白名单授权
        if (!string.IsNullOrWhiteSpace(input.AdminUserName) && !string.IsNullOrWhiteSpace(input.AdminPassword))
        {
            // 邮箱是全平台唯一的登录身份标识，开通管理员时必填
            if (string.IsNullOrWhiteSpace(input.AdminEmail) || !input.AdminEmail.Contains('@'))
            {
                throw new ArgumentException("租户管理员邮箱不能为空且格式必须有效。");
            }

            var passwordHash = _passwordHasher.HashPassword(input.AdminPassword.Trim());
            _ = await _tenantProvisionDomainService.ProvisionTenantAdminAsync(
                result.Tenant,
                input.AdminUserName.Trim(),
                input.AdminEmail.Trim(),
                passwordHash,
                cancellationToken);
        }

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

        var result = await _tenantDomainService.UpdateTenantAsync(TenantApplicationMapper.ToUpdateCommand(input), cancellationToken);
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
            TenantApplicationMapper.ToStatusCommand(input, _currentUser.UserId),
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

        var result = await _tenantDomainService.UpdateTenantMemberAsync(TenantMemberApplicationMapper.ToUpdateCommand(input), cancellationToken);
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
            TenantMemberApplicationMapper.ToInviteStatusCommand(input),
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
            TenantMemberApplicationMapper.ToStatusCommand(input),
            cancellationToken);
        return TenantMemberApplicationMapper.ToDetailDto(result.Member, result.Now);
    }
}
