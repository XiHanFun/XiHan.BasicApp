// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 资源选择器查询 DTO
/// </summary>
public sealed class ResourceSelectQueryDto
{
    /// <summary>
    /// 关键字（资源编码、名称、路径、描述）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType? ResourceType { get; set; }

    /// <summary>
    /// 返回数量上限
    /// </summary>
    public int Limit { get; set; } = 100;
}
