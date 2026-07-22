// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限 ABAC 条件领域服务
/// </summary>
public interface IPermissionConditionDomainService
{
    /// <summary>
    /// 创建权限 ABAC 条件
    /// </summary>
    Task<PermissionConditionCommandResult> CreatePermissionConditionAsync(PermissionConditionCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除权限 ABAC 条件
    /// </summary>
    Task DeletePermissionConditionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限 ABAC 条件
    /// </summary>
    Task<PermissionConditionCommandResult> UpdatePermissionConditionAsync(PermissionConditionUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限 ABAC 条件状态
    /// </summary>
    Task<PermissionConditionCommandResult> UpdatePermissionConditionStatusAsync(PermissionConditionStatusCommand command, CancellationToken cancellationToken = default);
}
