#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperation.pl
// Guid:89012345-6789-0123-4567-678901234567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:42:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统操作实体扩展
/// </summary>
public partial class SysOperation
{
    /// <summary>
    /// 操作权限列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysPermission.OperationId))]
    public virtual List<SysPermission>? Permissions { get; set; }
}
