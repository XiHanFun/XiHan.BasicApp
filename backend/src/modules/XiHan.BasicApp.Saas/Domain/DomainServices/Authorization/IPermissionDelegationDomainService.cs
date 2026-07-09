#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionDelegationDomainService
// Guid:9ce2f8de-9507-40f8-8aba-14d4a3f92c84
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
