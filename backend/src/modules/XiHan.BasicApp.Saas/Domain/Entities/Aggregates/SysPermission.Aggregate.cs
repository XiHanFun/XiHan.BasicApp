#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermission.Aggregate
// Guid:a1b2c3d4-5e6f-7890-abcd-ef12345678d1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities.Enums;
using XiHan.BasicApp.Saas.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限聚合领域行为
/// </summary>
public partial class SysPermission
{
    /// <summary>
    /// 启用权限
    /// </summary>
    public void Enable()
    {
        Status = EnableStatus.Enabled;
        AddLocalEvent(new PermissionChangedDomainEvent(BasicId));
    }

    /// <summary>
    /// 停用权限
    /// </summary>
    public void Disable()
    {
        Status = EnableStatus.Disabled;
        AddLocalEvent(new PermissionChangedDomainEvent(BasicId));
    }
}
