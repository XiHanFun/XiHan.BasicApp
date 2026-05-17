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

    private readonly IUserDomainService _userDomainService;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalEventBus _localEventBus;

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

        var result = await _userDomainService.CreateUserAsync(ToCreateCommand(input), cancellationToken);
        return UserApplicationMapper.ToDetailDto(result.User);
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

        var result = await _userDomainService.UpdateUserAsync(ToUpdateCommand(input), cancellationToken);
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

        var result = await _userDomainService.UpdateUserStatusAsync(ToStatusCommand(input), cancellationToken);
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

        var result = await _userDomainService.ResetUserPasswordAsync(ToPasswordResetCommand(input), cancellationToken);
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

        var result = await _userDomainService.UpdateUserLockAsync(ToLockCommand(input), cancellationToken);
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

        var result = await _userDomainService.UpdateUserLoginPolicyAsync(ToLoginPolicyCommand(input), cancellationToken);
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

        var result = await _userDomainService.CreateUserRoleAsync(ToRoleGrantCommand(input), cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(result.UserRole, result.Role, result.TenantMember, result.Now);
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

        var result = await _userDomainService.UpdateUserRoleAsync(ToRoleUpdateCommand(input), cancellationToken);
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

        var result = await _userDomainService.UpdateUserRoleStatusAsync(ToRoleStatusCommand(input), cancellationToken);
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

        var result = await _userDomainService.CreateUserPermissionAsync(ToPermissionGrantCommand(input), cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(result.UserPermission, result.Permission, result.TenantMember, result.Now);
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

        var result = await _userDomainService.UpdateUserPermissionAsync(ToPermissionUpdateCommand(input), cancellationToken);
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

        var result = await _userDomainService.UpdateUserPermissionStatusAsync(ToPermissionStatusCommand(input), cancellationToken);
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

        var result = await _userDomainService.CreateUserDataScopeAsync(ToDataScopeGrantCommand(input), cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department, result.TenantMember);
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

        var result = await _userDomainService.UpdateUserDataScopeAsync(ToDataScopeUpdateCommand(input), cancellationToken);
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

        var result = await _userDomainService.UpdateUserDataScopeStatusAsync(ToDataScopeStatusCommand(input), cancellationToken);
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

        var result = await _userDomainService.CreateUserDepartmentAsync(ToDepartmentAssignCommand(input), cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(result.UserDepartment, result.Department);
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

        var result = await _userDomainService.UpdateUserDepartmentAsync(ToDepartmentUpdateCommand(input), cancellationToken);
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

        var result = await _userDomainService.UpdateUserDepartmentStatusAsync(ToDepartmentStatusCommand(input), cancellationToken);
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

        var result = await _userDomainService.RevokeUserSessionAsync(ToSessionRevokeCommand(input), cancellationToken);
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

        var result = await _userDomainService.RevokeUserSessionsAsync(ToSessionsRevokeCommand(input), cancellationToken);
        await PublishDomainEventAsync(result.DomainEvent, cancellationToken);
        return result.Count;
    }

    #endregion

    private UserCreateCommand ToCreateCommand(UserCreateDto input)
    {
        return new UserCreateCommand(
            input.UserName,
            input.InitialPassword,
            input.RealName,
            input.NickName,
            input.Avatar,
            input.Email,
            input.Phone,
            input.Gender,
            input.Birthday,
            input.Status,
            input.TimeZone,
            input.Language,
            input.Country,
            input.MemberType,
            input.EffectiveTime,
            input.ExpirationTime,
            input.DisplayName,
            input.InviteRemark,
            input.Remark,
            _currentUser.UserId);
    }

    private static UserUpdateCommand ToUpdateCommand(UserUpdateDto input)
    {
        return new UserUpdateCommand(
            input.BasicId,
            input.RealName,
            input.NickName,
            input.Avatar,
            input.Email,
            input.Phone,
            input.Gender,
            input.Birthday,
            input.TimeZone,
            input.Language,
            input.Country,
            input.Remark);
    }

    private static UserStatusChangeCommand ToStatusCommand(UserStatusUpdateDto input)
    {
        return new UserStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    private static UserPasswordResetCommand ToPasswordResetCommand(UserPasswordResetDto input)
    {
        return new UserPasswordResetCommand(input.UserId, input.NewPassword, input.PasswordExpiryTime, input.Remark);
    }

    private static UserLockChangeCommand ToLockCommand(UserLockUpdateDto input)
    {
        return new UserLockChangeCommand(input.UserId, input.IsLocked, input.LockoutEndTime, input.Remark);
    }

    private static UserLoginPolicyUpdateCommand ToLoginPolicyCommand(UserLoginPolicyUpdateDto input)
    {
        return new UserLoginPolicyUpdateCommand(input.UserId, input.AllowMultiLogin, input.MaxLoginDevices, input.Remark);
    }

    private static UserRoleGrantCommand ToRoleGrantCommand(UserRoleGrantDto input)
    {
        return new UserRoleGrantCommand(
            input.UserId,
            input.RoleId,
            input.EffectiveTime,
            input.ExpirationTime,
            input.GrantReason,
            input.Remark);
    }

    private static UserRoleUpdateCommand ToRoleUpdateCommand(UserRoleUpdateDto input)
    {
        return new UserRoleUpdateCommand(
            input.BasicId,
            input.EffectiveTime,
            input.ExpirationTime,
            input.GrantReason,
            input.Remark);
    }

    private static UserRoleStatusChangeCommand ToRoleStatusCommand(UserRoleStatusUpdateDto input)
    {
        return new UserRoleStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    private static UserPermissionGrantCommand ToPermissionGrantCommand(UserPermissionGrantDto input)
    {
        return new UserPermissionGrantCommand(
            input.UserId,
            input.PermissionId,
            input.PermissionAction,
            input.EffectiveTime,
            input.ExpirationTime,
            input.GrantReason,
            input.Remark);
    }

    private static UserPermissionUpdateCommand ToPermissionUpdateCommand(UserPermissionUpdateDto input)
    {
        return new UserPermissionUpdateCommand(
            input.BasicId,
            input.PermissionAction,
            input.EffectiveTime,
            input.ExpirationTime,
            input.GrantReason,
            input.Remark);
    }

    private static UserPermissionStatusChangeCommand ToPermissionStatusCommand(UserPermissionStatusUpdateDto input)
    {
        return new UserPermissionStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    private static UserDataScopeGrantCommand ToDataScopeGrantCommand(UserDataScopeGrantDto input)
    {
        return new UserDataScopeGrantCommand(
            input.UserId,
            input.DataScope,
            input.DepartmentId,
            input.IncludeChildren,
            input.Remark);
    }

    private static UserDataScopeUpdateCommand ToDataScopeUpdateCommand(UserDataScopeUpdateDto input)
    {
        return new UserDataScopeUpdateCommand(
            input.BasicId,
            input.DataScope,
            input.DepartmentId,
            input.IncludeChildren,
            input.Remark);
    }

    private static UserDataScopeStatusChangeCommand ToDataScopeStatusCommand(UserDataScopeStatusUpdateDto input)
    {
        return new UserDataScopeStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    private static UserDepartmentAssignCommand ToDepartmentAssignCommand(UserDepartmentAssignDto input)
    {
        return new UserDepartmentAssignCommand(input.UserId, input.DepartmentId, input.IsMain, input.Remark);
    }

    private static UserDepartmentUpdateCommand ToDepartmentUpdateCommand(UserDepartmentUpdateDto input)
    {
        return new UserDepartmentUpdateCommand(input.BasicId, input.IsMain, input.Remark);
    }

    private static UserDepartmentStatusChangeCommand ToDepartmentStatusCommand(UserDepartmentStatusUpdateDto input)
    {
        return new UserDepartmentStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    private UserSessionRevokeCommand ToSessionRevokeCommand(UserSessionRevokeDto input)
    {
        return new UserSessionRevokeCommand(input.BasicId, input.Reason, _currentUser.UserId);
    }

    private UserSessionsRevokeCommand ToSessionsRevokeCommand(UserSessionsRevokeDto input)
    {
        return new UserSessionsRevokeCommand(input.UserId, input.Reason, _currentUser.UserId);
    }

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
