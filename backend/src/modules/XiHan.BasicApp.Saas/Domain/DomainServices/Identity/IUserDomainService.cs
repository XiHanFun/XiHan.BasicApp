#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserDomainService
// Guid:da76a9e4-5f6d-4e43-9ebb-e04b41246d22
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 用户领域服务
/// </summary>
public interface IUserDomainService
{
    /// <summary>
    /// 创建用户
    /// </summary>
    Task<UserCommandResult> CreateUserAsync(UserCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户资料
    /// </summary>
    Task<UserCommandResult> UpdateUserAsync(UserUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户状态
    /// </summary>
    Task<UserCommandResult> UpdateUserStatusAsync(UserStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task DeleteUserAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置用户密码
    /// </summary>
    Task<UserSecurityCommandResult> ResetUserPasswordAsync(UserPasswordResetCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户锁定状态
    /// </summary>
    Task<UserSecurityCommandResult> UpdateUserLockAsync(UserLockChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户登录策略
    /// </summary>
    Task<UserSecurityCommandResult> UpdateUserLoginPolicyAsync(UserLoginPolicyUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 授予用户角色
    /// </summary>
    Task<UserRoleCommandResult> CreateUserRoleAsync(UserRoleGrantCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户角色
    /// </summary>
    Task<UserRoleCommandResult> UpdateUserRoleAsync(UserRoleUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户角色状态
    /// </summary>
    Task<UserRoleCommandResult> UpdateUserRoleStatusAsync(UserRoleStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户角色
    /// </summary>
    Task DeleteUserRoleAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 授予用户直授权限
    /// </summary>
    Task<UserPermissionCommandResult> CreateUserPermissionAsync(UserPermissionGrantCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户直授权限
    /// </summary>
    Task<UserPermissionCommandResult> UpdateUserPermissionAsync(UserPermissionUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户直授权限状态
    /// </summary>
    Task<UserPermissionCommandResult> UpdateUserPermissionStatusAsync(UserPermissionStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户直授权限
    /// </summary>
    Task DeleteUserPermissionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 授予用户数据范围
    /// </summary>
    Task<UserDataScopeCommandResult> CreateUserDataScopeAsync(UserDataScopeGrantCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户数据范围
    /// </summary>
    Task<UserDataScopeCommandResult> UpdateUserDataScopeAsync(UserDataScopeUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户数据范围状态
    /// </summary>
    Task<UserDataScopeCommandResult> UpdateUserDataScopeStatusAsync(UserDataScopeStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户数据范围
    /// </summary>
    Task DeleteUserDataScopeAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分配用户部门归属
    /// </summary>
    Task<UserDepartmentCommandResult> CreateUserDepartmentAsync(UserDepartmentAssignCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户部门归属
    /// </summary>
    Task<UserDepartmentCommandResult> UpdateUserDepartmentAsync(UserDepartmentUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户部门归属状态
    /// </summary>
    Task<UserDepartmentCommandResult> UpdateUserDepartmentStatusAsync(UserDepartmentStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户部门归属
    /// </summary>
    Task DeleteUserDepartmentAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户会话
    /// </summary>
    Task<UserSessionCommandResult> RevokeUserSessionAsync(UserSessionRevokeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户全部会话
    /// </summary>
    Task<UserSessionsRevokeResult> RevokeUserSessionsAsync(UserSessionsRevokeCommand command, CancellationToken cancellationToken = default);
}
