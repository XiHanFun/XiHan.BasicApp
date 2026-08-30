// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Core.Exceptions;
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
    /// <summary>
    /// 租户管理员用户名最小长度
    /// </summary>
    private const int MinAdminUserNameLength = 3;

    /// <summary>
    /// 租户管理员用户名最大长度
    /// </summary>
    private const int MaxAdminUserNameLength = 50;

    private readonly ICurrentUser _currentUser;
    private readonly ITenantDomainService _tenantDomainService;
    private readonly ISaasCacheInvalidator _cacheInvalidator;
    private readonly ITenantProvisionDomainService _tenantProvisionDomainService;
    private readonly ITenantDatabaseInitializer _tenantDatabaseInitializer;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthenticationService _authenticationService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantAppService(
        ITenantDomainService tenantDomainService,
        ITenantProvisionDomainService tenantProvisionDomainService,
        ITenantDatabaseInitializer tenantDatabaseInitializer,
        IPasswordHasher passwordHasher,
        IAuthenticationService authenticationService,
        ICurrentUser currentUser,
        ISaasCacheInvalidator cacheInvalidator)
    {
        _tenantDomainService = tenantDomainService;
        _tenantProvisionDomainService = tenantProvisionDomainService;
        _tenantDatabaseInitializer = tenantDatabaseInitializer;
        _passwordHasher = passwordHasher;
        _authenticationService = authenticationService;
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

        // 管理员账号先于建租户校验：没有管理员的租户没有任何账号能登录，因此是创建租户的必要组成
        var adminUserName = input.AdminUserName?.Trim() ?? string.Empty;
        var adminEmail = input.AdminEmail?.Trim() ?? string.Empty;
        var adminPassword = input.AdminPassword?.Trim() ?? string.Empty;
        await ValidateTenantAdminAsync(input, adminUserName, adminEmail, adminPassword);

        var result = await _tenantDomainService.CreateTenantAsync(TenantApplicationMapper.ToCreateCommand(input), cancellationToken);

        // 一站式开通：管理员 + Owner 角色 + 按版本白名单授权
        var passwordHash = _passwordHasher.HashPassword(adminPassword);
        _ = await _tenantProvisionDomainService.ProvisionTenantAdminAsync(
            result.Tenant,
            adminUserName,
            adminEmail,
            passwordHash,
            cancellationToken);

        return TenantApplicationMapper.ToDetailDto(result.Tenant, result.Now);
    }

    /// <summary>
    /// 校验租户管理员账号：用户名长度、邮箱格式、密码策略
    /// </summary>
    /// <remarks>
    /// 用户名与邮箱的唯一性在 <see cref="ITenantProvisionDomainService.InitializeTenantAdminAsync"/> 内校验（平台态查账号注册表）。
    /// </remarks>
    private async Task ValidateTenantAdminAsync(TenantCreateDto input, string adminUserName, string adminEmail, string adminPassword)
    {
        if (string.IsNullOrWhiteSpace(adminUserName))
        {
            throw new UserFriendlyException("租户管理员用户名不能为空。");
        }

        if (adminUserName.Length is < MinAdminUserNameLength or > MaxAdminUserNameLength)
        {
            throw new UserFriendlyException($"租户管理员用户名长度须在 {MinAdminUserNameLength}~{MaxAdminUserNameLength} 个字符之间。");
        }

        if (string.IsNullOrWhiteSpace(adminEmail) || !MailAddress.TryCreate(adminEmail, out _))
        {
            throw new UserFriendlyException("租户管理员邮箱不能为空且格式必须有效。");
        }

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new UserFriendlyException("租户管理员初始密码不能为空。");
        }

        // 密码黑名单：禁止用账号自身信息做密码
        var blacklist = new List<string> { adminUserName, adminEmail, input.TenantCode, input.TenantName }
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToList();

        var validation = await _authenticationService.ValidatePasswordStrengthAsync(adminPassword, blacklist);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Count > 0 ? string.Join("；", validation.Errors) : validation.Message;
            throw new UserFriendlyException($"租户管理员密码不符合安全要求：{errors}");
        }
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
    /// </remarks>
    [UnitOfWork(false)]
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.Tenant.InitDb)]
    public async Task<TenantDetailDto> InitializeDatabaseAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tenant = await _tenantDatabaseInitializer.InitializeAsync(id, cancellationToken);
        return TenantApplicationMapper.ToDetailDto(tenant, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 删除租户（软删，要求租户已停用或暂停）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Tenant.Delete)]
    public async Task DeleteTenantAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _tenantDomainService.DeleteTenantAsync(id, cancellationToken);
        // 删除的租户不应再出现在版本门控缓存里
        await _cacheInvalidator.InvalidateEditionGateAsync(cancellationToken);
    }

    /// <summary>
    /// 添加租户成员（把已有用户直接加入租户，立即生效）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Create)]
    public async Task<TenantMemberDetailDto> AddTenantMemberAsync(TenantMemberAddDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantDomainService.AddTenantMemberAsync(
            TenantMemberApplicationMapper.ToAddCommand(input, _currentUser.UserId),
            cancellationToken);
        return TenantMemberApplicationMapper.ToDetailDto(result.Member, result.Now);
    }

    /// <summary>
    /// 邀请租户成员（落待接受邀请，被邀请人接受后生效）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Invite)]
    public async Task<TenantMemberDetailDto> InviteTenantMemberAsync(TenantMemberInviteDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tenantDomainService.AddTenantMemberAsync(
            TenantMemberApplicationMapper.ToInviteCommand(input, _currentUser.UserId),
            cancellationToken);
        return TenantMemberApplicationMapper.ToDetailDto(result.Member, result.Now);
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
