#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPermissionsChangedDomainEvent
// Guid:2db6b12f-80d8-4f8d-91ec-68f7349ef550
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 19:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 用户直授权限变更事件
/// </summary>
public sealed class UserPermissionsChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="permissionIds">新权限集合</param>
    /// <param name="tenantId">租户 ID</param>
    public UserPermissionsChangedDomainEvent(long userId, IReadOnlyCollection<long> permissionIds, long? tenantId = null)
    {
        UserId = userId;
        PermissionIds = permissionIds;
        TenantId = tenantId;
    }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// 新权限集合
    /// </summary>
    public IReadOnlyCollection<long> PermissionIds { get; }

    /// <summary>
    /// 租户 ID
    /// </summary>
    public long? TenantId { get; }
}
