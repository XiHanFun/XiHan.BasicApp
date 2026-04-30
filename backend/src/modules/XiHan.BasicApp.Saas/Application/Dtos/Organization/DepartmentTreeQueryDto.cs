#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentTreeQueryDto
// Guid:da96fc8d-5115-4587-ae1f-1b8d7c42ed3f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
