// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
