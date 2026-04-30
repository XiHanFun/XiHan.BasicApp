#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDataScopeGrantDto
// Guid:5d6f7d73-585e-4f34-99f7-0a399b45c7e2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户数据范围授权 DTO
/// </summary>
public sealed class UserDataScopeGrantDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 数据权限范围
    /// </summary>
    public DataPermissionScope DataScope { get; set; } = DataPermissionScope.Custom;

    /// <summary>
    /// 部门主键（仅自定义范围需要）
    /// </summary>
    public long? DepartmentId { get; set; }

    /// <summary>
    /// 是否包含子部门（仅自定义范围生效）
    /// </summary>
    public bool IncludeChildren { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
