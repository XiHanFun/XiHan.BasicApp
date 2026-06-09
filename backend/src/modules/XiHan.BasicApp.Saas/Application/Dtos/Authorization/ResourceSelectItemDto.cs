#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceSelectItemDto
// Guid:59c20ec8-6b8c-432b-b157-5741903e433e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
