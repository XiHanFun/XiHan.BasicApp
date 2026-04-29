#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourcePageQueryDto
// Guid:a898ecbd-872d-4595-ae96-ff818a0c5bf1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 资源分页查询 DTO
/// </summary>
public sealed class ResourcePageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（资源编码、名称、路径、描述、元数据）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType? ResourceType { get; set; }

    /// <summary>
    /// 访问级别
    /// </summary>
    public ResourceAccessLevel? AccessLevel { get; set; }

    /// <summary>
    /// 是否全局资源
    /// </summary>
    public bool? IsGlobal { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
