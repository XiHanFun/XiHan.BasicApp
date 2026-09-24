// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 自定义数据范围里的一个部门
/// </summary>
public sealed class DataScopeDepartmentDto
{
    /// <summary>
    /// 部门主键
    /// </summary>
    public long DepartmentId { get; set; }

    /// <summary>
    /// 是否含下级部门
    /// </summary>
    public bool IncludeChildren { get; set; }
}
