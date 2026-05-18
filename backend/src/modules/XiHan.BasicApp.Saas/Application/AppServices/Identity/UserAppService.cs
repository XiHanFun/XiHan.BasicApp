#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserAppService
// Guid:ce9953f4-f7d4-4ebe-96cc-e282947f11ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户")]
public sealed class UserAppService
    : SaasApplicationService, IUserAppService
{
    private readonly ICurrentUser _currentUser;

    private readonly ILocalEventBus _localEventBus;

    private readonly IUserDomainService _userDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserAppService(
        IUserDomainService userDomainService,
        ICurrentUser currentUser,
        ILocalEventBus localEventBus)
    {
        _userDomainService = userDomainService;
        _currentUser = currentUser;
        _localEventBus = localEventBus;
    }

    #region 用户核心

    /// <summary>
    /// 创建用户
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Create)]
    public async Task<UserDetailDto> CreateUserAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.CreateUserAsync(
            UserApplicationMapper.ToCreateCommand(input, _currentUser.UserId),
            cancellationToken);
        return UserApplicationMapper.ToDetailDto(result.User);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Delete)]
    public async Task DeleteUserAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userDomainService.DeleteUserAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Update)]
    public async Task<UserDetailDto> UpdateUserAsync(UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserAsync(UserApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return UserApplicationMapper.ToDetailDto(result.User);
    }

    /// <summary>
    /// 更新用户状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Status)]
    public async Task<UserDetailDto> UpdateUserStatusAsync(UserStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserStatusAsync(UserApplicationMapper.ToStatusCommand(input), cancellationToken);
        return UserApplicationMapper.ToDetailDto(result.User);
    }

    #endregion

    #region 用户安全

    /// <summary>
    /// 重置用户密码
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.ResetPassword)]
    public async Task<UserSecurityDetailDto> ResetUserPasswordAsync(UserPasswordResetDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.ResetUserPasswordAsync(UserSecurityApplicationMapper.ToPasswordResetCommand(input), cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    /// <summary>
    /// 更新用户锁定状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.Lock)]
    public async Task<UserSecurityDetailDto> UpdateUserLockAsync(UserLockUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserLockAsync(UserSecurityApplicationMapper.ToLockCommand(input), cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    /// <summary>
    /// 更新用户登录策略
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.LoginPolicy)]
    public async Task<UserSecurityDetailDto> UpdateUserLoginPolicyAsync(UserLoginPolicyUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserLoginPolicyAsync(UserSecurityApplicationMapper.ToLoginPolicyCommand(input), cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    #endregion

    #region 用户角色

    /// <summary>
    /// 授予用户角色
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Grant)]
    public async Task<UserRoleDetailDto> CreateUserRoleAsync(UserRoleGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.CreateUserRoleAsync(UserRoleApplicationMapper.ToGrantCommand(input), cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(result.UserRole, result.Role, result.TenantMember, result.Now);
    }

    /// <summary>
    /// 撤销用户角色
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Revoke)]
    public async Task DeleteUserRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userDomainService.DeleteUserRoleAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新用户角色
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Update)]
    public async Task<UserRoleDetailDto> UpdateUserRoleAsync(UserRoleUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserRoleAsync(UserRoleApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(result.UserRole, result.Role, result.TenantMember, result.Now);
    }

    /// <summary>
    /// 更新用户角色状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Status)]
    public async Task<UserRoleDetailDto> UpdateUserRoleStatusAsync(UserRoleStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserRoleStatusAsync(UserRoleApplicationMapper.ToStatusCommand(input), cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(result.UserRole, result.Role, result.TenantMember, result.Now);
    }

    #endregion

    #region 用户直授权限

    /// <summary>
    /// 授予用户直授权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Grant)]
    public async Task<UserPermissionDetailDto> CreateUserPermissionAsync(UserPermissionGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.CreateUserPermissionAsync(UserPermissionApplicationMapper.ToGrantCommand(input), cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(result.UserPermission, result.Permission, result.TenantMember, result.Now);
    }

    /// <summary>
    /// 撤销用户直授权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Revoke)]
    public async Task DeleteUserPermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userDomainService.DeleteUserPermissionAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新用户直授权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Update)]
    public async Task<UserPermissionDetailDto> UpdateUserPermissionAsync(UserPermissionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserPermissionAsync(UserPermissionApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(result.UserPermission, result.Permission, result.TenantMember, result.Now);
    }

    /// <summary>
    /// 更新用户直授权限状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Status)]
    public async Task<UserPermissionDetailDto> UpdateUserPermissionStatusAsync(UserPermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserPermissionStatusAsync(UserPermissionApplicationMapper.ToStatusCommand(input), cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(result.UserPermission, result.Permission, result.TenantMember, result.Now);
    }

    #endregion

    #region 用户数据范围

    /// <summary>
    /// 授予用户数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Grant)]
    public async Task<UserDataScopeDetailDto> CreateUserDataScopeAsync(UserDataScopeGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.CreateUserDataScopeAsync(UserDataScopeApplicationMapper.ToGrantCommand(input), cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department, result.TenantMember);
    }

    /// <summary>
    /// 撤销用户数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Revoke)]
    public async Task DeleteUserDataScopeAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userDomainService.DeleteUserDataScopeAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新用户数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Update)]
    public async Task<UserDataScopeDetailDto> UpdateUserDataScopeAsync(UserDataScopeUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserDataScopeAsync(UserDataScopeApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department, result.TenantMember);
    }

    /// <summary>
    /// 更新用户数据范围状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Status)]
    public async Task<UserDataScopeDetailDto> UpdateUserDataScopeStatusAsync(UserDataScopeStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserDataScopeStatusAsync(UserDataScopeApplicationMapper.ToStatusCommand(input), cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department, result.TenantMember);
    }

    #endregion

    #region 用户部门

    /// <summary>
    /// 分配用户部门归属
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Grant)]
    public async Task<UserDepartmentDetailDto> CreateUserDepartmentAsync(UserDepartmentAssignDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.CreateUserDepartmentAsync(UserDepartmentApplicationMapper.ToAssignCommand(input), cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(result.UserDepartment, result.Department);
    }

    /// <summary>
    /// 撤销用户部门归属
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Revoke)]
    public async Task DeleteUserDepartmentAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userDomainService.DeleteUserDepartmentAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新用户部门归属
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Update)]
    public async Task<UserDepartmentDetailDto> UpdateUserDepartmentAsync(UserDepartmentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserDepartmentAsync(UserDepartmentApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(result.UserDepartment, result.Department);
    }

    /// <summary>
    /// 更新用户部门归属状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Status)]
    public async Task<UserDepartmentDetailDto> UpdateUserDepartmentStatusAsync(UserDepartmentStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserDepartmentStatusAsync(UserDepartmentApplicationMapper.ToStatusCommand(input), cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(result.UserDepartment, result.Department);
    }

    #endregion

    #region 用户会话

    /// <summary>
    /// 撤销用户会话
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Revoke)]
    public async Task<UserSessionDetailDto> RevokeUserSessionAsync(UserSessionRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.RevokeUserSessionAsync(
            UserSessionApplicationMapper.ToRevokeCommand(input, _currentUser.UserId),
            cancellationToken);
        await PublishDomainEventAsync(result.DomainEvent, cancellationToken);
        return UserSessionApplicationMapper.ToDetailDto(result.Session, result.User, result.Now);
    }

    /// <summary>
    /// 撤销用户全部会话
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Revoke)]
    public async Task<int> RevokeUserSessionsAsync(UserSessionsRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.RevokeUserSessionsAsync(
            UserSessionApplicationMapper.ToRevokeAllCommand(input, _currentUser.UserId),
            cancellationToken);
        await PublishDomainEventAsync(result.DomainEvent, cancellationToken);
        return result.Count;
    }

    #endregion

    private async Task PublishDomainEventAsync(UserSessionRevokedDomainEvent? domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent is null)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _localEventBus.PublishAsync(domainEvent);
    }

}
