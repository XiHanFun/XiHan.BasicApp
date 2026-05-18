#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionConditionDomainService
// Guid:809ce435-d62f-4c36-b2d6-ae77350016b5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
