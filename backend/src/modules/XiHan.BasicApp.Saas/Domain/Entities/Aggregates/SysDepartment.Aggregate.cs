#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartment.Aggregate.cs
// Guid:a3c7e1f4-8b2d-4a6e-9f01-d5c8b7a2e4f6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 部门聚合领域行为
/// </summary>
public partial class SysDepartment
{
    /// <summary>
    /// 启用部门
    /// </summary>
    public void Enable()
    {
        Status = EnableStatus.Enabled;
        AddLocalEvent(new DepartmentChangedDomainEvent(BasicId));
    }

    /// <summary>
    /// 禁用部门
    /// </summary>
    public void Disable()
    {
        Status = EnableStatus.Disabled;
        AddLocalEvent(new DepartmentChangedDomainEvent(BasicId));
    }
}
