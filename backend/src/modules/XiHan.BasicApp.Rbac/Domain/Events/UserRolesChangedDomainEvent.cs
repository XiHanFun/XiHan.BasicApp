#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRolesChangedDomainEvent
// Guid:b8ea5fda-6a10-4749-a5b7-0bc79e462bce
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:37:24
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 用户角色变更事件
/// </summary>
public sealed class UserRolesChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="roleIds">新角色集合</param>
    public UserRolesChangedDomainEvent(long userId, IReadOnlyCollection<long> roleIds)
    {
        UserId = userId;
        RoleIds = roleIds;
    }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// 新角色集合
    /// </summary>
    public IReadOnlyCollection<long> RoleIds { get; }
}
