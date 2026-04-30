#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDataScopeUpdateDto
// Guid:6f48ed14-89a2-4c5a-b154-a4574d67dcd4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户数据范围更新 DTO
/// </summary>
public sealed class UserDataScopeUpdateDto : BasicAppUDto
{
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
