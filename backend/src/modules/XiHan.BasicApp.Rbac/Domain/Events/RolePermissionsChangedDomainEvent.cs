#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RolePermissionsChangedDomainEvent
// Guid:1cc5f276-8e1e-4622-b48a-73bc7c278d52
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:37:30
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 角色权限变更事件
/// </summary>
public sealed class RolePermissionsChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <param name="permissionIds">新权限集合</param>
    public RolePermissionsChangedDomainEvent(long roleId, IReadOnlyCollection<long> permissionIds)
    {
        RoleId = roleId;
        PermissionIds = permissionIds;
    }

    /// <summary>
    /// 角色 ID
    /// </summary>
    public long RoleId { get; }

    /// <summary>
    /// 新权限集合
    /// </summary>
    public IReadOnlyCollection<long> PermissionIds { get; }
}
