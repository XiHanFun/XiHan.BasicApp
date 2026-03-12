#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRole.Aggregate
// Guid:a3a72482-eea2-4ffc-af9f-ddd171227e40
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 07:04:40
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 角色聚合领域行为
/// </summary>
public partial class SysRole
{
    /// <summary>
    /// 启用角色
    /// </summary>
    public void Enable()
    {
        Status = YesOrNo.Yes;
    }

    /// <summary>
    /// 禁用角色
    /// </summary>
    public void Disable()
    {
        Status = YesOrNo.No;
    }

    /// <summary>
    /// 记录权限变更
    /// </summary>
    public void MarkPermissionsChanged(IReadOnlyCollection<long> permissionIds)
    {
        AddLocalEvent(new RolePermissionsChangedDomainEvent(BasicId, permissionIds, TenantId));
    }

    /// <summary>
    /// 记录数据范围变更
    /// </summary>
    public void MarkDataScopeChanged(IReadOnlyCollection<long> departmentIds)
    {
        AddLocalEvent(new RoleDataScopeChangedDomainEvent(BasicId, DataScope, departmentIds, TenantId));
    }
}
