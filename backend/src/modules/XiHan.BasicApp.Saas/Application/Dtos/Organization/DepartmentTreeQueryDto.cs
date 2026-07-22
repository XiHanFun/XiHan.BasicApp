// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 部门树查询 DTO
/// </summary>
public sealed class DepartmentTreeQueryDto
{
    /// <summary>
    /// 关键字（部门编码、名称、电话、邮箱、地址、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 是否仅返回启用部门
    /// </summary>
    public bool OnlyEnabled { get; set; } = true;

    /// <summary>
    /// 返回数量上限
    /// </summary>
    public int Limit { get; set; } = 2000;
}
