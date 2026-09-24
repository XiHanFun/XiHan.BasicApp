// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色数据范围设置 DTO：档位与自定义部门一次提交
/// </summary>
public sealed class RoleDataScopeSetDto
{
    /// <summary>
    /// 角色主键
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 数据范围档位（全局角色不能用自定义）
    /// </summary>
    public DataPermissionScope DataScope { get; set; }

    /// <summary>
    /// 自定义部门（仅档位为自定义时提交，且至少一个）
    /// </summary>
    public List<DataScopeDepartmentDto> Departments { get; set; } = [];
}
