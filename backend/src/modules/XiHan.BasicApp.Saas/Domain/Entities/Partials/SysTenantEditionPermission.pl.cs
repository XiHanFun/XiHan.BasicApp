#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantEditionPermission.pl
// Guid:b8c9d0e1-f2a3-4567-1234-678901234508
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 租户版本可用权限映射实体扩展
/// </summary>
public partial class SysTenantEditionPermission
{
    /// <summary>
    /// 版本信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(EditionId))]
    public virtual SysTenantEdition? Edition { get; set; }

    /// <summary>
    /// 权限信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(PermissionId))]
    public virtual SysPermission? Permission { get; set; }
}
