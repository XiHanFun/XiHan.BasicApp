// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 资源定义更新 DTO
/// </summary>
public sealed class ResourceUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 资源名称
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType ResourceType { get; set; } = ResourceType.Api;

    /// <summary>
    /// 资源路径
    /// </summary>
    public string? ResourcePath { get; set; }

    /// <summary>
    /// 资源描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 资源元数据
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 访问级别
    /// </summary>
    public ResourceAccessLevel AccessLevel { get; set; } = ResourceAccessLevel.Authorized;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
