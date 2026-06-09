#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionSelectQueryDto
// Guid:af97cd0b-1eca-4c0b-9c6c-eb9a2d6f3718
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限选择器查询 DTO
/// </summary>
public sealed class PermissionSelectQueryDto
{
    /// <summary>
    /// 关键字（权限编码、名称、描述）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 模块编码
    /// </summary>
    public string? ModuleCode { get; set; }

    /// <summary>
    /// 权限类型
    /// </summary>
    public PermissionType? PermissionType { get; set; }

    /// <summary>
    /// 返回数量上限
    /// </summary>
    public int Limit { get; set; } = 100;
}
