#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleDataScopeChangedDomainEvent
// Guid:4de8b7ed-f56a-45ea-b024-083e7df7cd2f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 19:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 角色数据范围变更事件
/// </summary>
public sealed class RoleDataScopeChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <param name="dataScope">数据范围类型</param>
    /// <param name="departmentIds">自定义部门集合</param>
    /// <param name="tenantId">租户 ID</param>
    public RoleDataScopeChangedDomainEvent(
        long roleId,
        DataPermissionScope dataScope,
        IReadOnlyCollection<long> departmentIds,
        long? tenantId = null)
    {
        RoleId = roleId;
        DataScope = dataScope;
        DepartmentIds = departmentIds;
        TenantId = tenantId;
    }

    /// <summary>
    /// 角色 ID
    /// </summary>
    public long RoleId { get; }

    /// <summary>
    /// 数据范围类型
    /// </summary>
    public DataPermissionScope DataScope { get; }

    /// <summary>
    /// 自定义部门集合
    /// </summary>
    public IReadOnlyCollection<long> DepartmentIds { get; }

    /// <summary>
    /// 租户 ID
    /// </summary>
    public long? TenantId { get; }
}
