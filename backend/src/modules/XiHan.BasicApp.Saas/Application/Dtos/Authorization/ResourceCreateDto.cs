#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceCreateDto
// Guid:49afe985-0352-4607-bdf6-39a0a32ef6f7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 资源定义创建 DTO
/// </summary>
public sealed class ResourceCreateDto
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
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
