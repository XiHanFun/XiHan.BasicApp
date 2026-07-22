// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 资源选择项 DTO
/// </summary>
public sealed class ResourceSelectItemDto : BasicAppDto
{
    /// <summary>
    /// 资源编码
    /// </summary>
    public string ResourceCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType ResourceType { get; set; }

    /// <summary>
    /// 资源路径
    /// </summary>
    public string? ResourcePath { get; set; }
}
