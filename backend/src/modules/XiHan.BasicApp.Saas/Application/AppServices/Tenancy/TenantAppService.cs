// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Caching;
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
    private readonly ISaasCacheInvalidator _cacheInvalidator;
    private readonly ITenantProvisionDomainService _tenantProvisionDomainService;
    private readonly ITenantDatabaseInitializer _tenantDatabaseInitializer;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantAppService(
        ITenantDomainService tenantDomainService,
        ITenantProvisionDomainService tenantProvisionDomainService,
        ITenantDatabaseInitializer tenantDatabaseInitializer,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        ISaasCacheInvalidator cacheInvalidator)
    {
        _tenantDomainService = tenantDomainService;
        _tenantProvisionDomainService = tenantProvisionDomainService;
        _tenantDatabaseInitializer = tenantDatabaseInitializer;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _cacheInvalidator = cacheInvalidator;
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
        // 租户可能更换版本：失效版本门控缓存（事务提交后生效）
        await _cacheInvalidator.InvalidateEditionGateAsync(cancellationToken);
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
    /// 初始化租户数据库（仅库隔离租户：建库 → 建表 → 基线种子，幂等）
    /// </summary>
    /// <remarks>
    /// 非事务：DDL（建库）不可在事务内执行；建库/建表/种子在租户独立库上进行，配置状态写回平台库。
    /// 动态 API 约定：POST 不自动把 id 拼进路由，须显式 [FromRoute] 才生成 InitializeDatabase/{id}。
    /// </remarks>
    [UnitOfWork(false)]
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.Tenant.InitDb)]
    public async Task<TenantDetailDto> InitializeDatabaseAsync([FromRoute] long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tenant = await _tenantDatabaseInitializer.InitializeAsync(id, cancellationToken);
        return TenantApplicationMapper.ToDetailDto(tenant, DateTimeOffset.UtcNow);
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
