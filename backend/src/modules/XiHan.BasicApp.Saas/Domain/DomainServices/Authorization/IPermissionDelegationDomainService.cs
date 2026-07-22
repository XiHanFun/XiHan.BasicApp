// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限委托领域服务
/// </summary>
public interface IPermissionDelegationDomainService
{
    /// <summary>
    /// 创建权限委托
    /// </summary>
    Task<PermissionDelegationCommandResult> CreatePermissionDelegationAsync(PermissionDelegationCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销权限委托
    /// </summary>
    /// <returns>被撤销委托的被委托人/权限/角色（供审计发事件）</returns>
    Task<PermissionDelegationCommandResult> RevokePermissionDelegationAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限委托
    /// </summary>
    Task<PermissionDelegationCommandResult> UpdatePermissionDelegationAsync(PermissionDelegationUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限委托状态
    /// </summary>
    Task<PermissionDelegationCommandResult> UpdatePermissionDelegationStatusAsync(PermissionDelegationStatusCommand command, CancellationToken cancellationToken = default);
}
