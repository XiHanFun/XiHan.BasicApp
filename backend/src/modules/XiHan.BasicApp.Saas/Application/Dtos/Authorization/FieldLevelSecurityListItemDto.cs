#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityListItemDto
// Guid:62148dd5-e46a-4aa7-822a-f5a5d21070cb
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
/// 字段级安全列表项 DTO
/// </summary>
public sealed class FieldLevelSecurityListItemDto : BasicAppDto
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public FieldSecurityTargetType TargetType { get; set; }

    /// <summary>
    /// 目标主键
    /// </summary>
    public long TargetId { get; set; }

    /// <summary>
    /// 目标编码
    /// </summary>
    public string? TargetCode { get; set; }

    /// <summary>
    /// 目标名称
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// 资源主键
    /// </summary>
    public long ResourceId { get; set; }

    /// <summary>
    /// 资源编码
    /// </summary>
    public string? ResourceCode { get; set; }

    /// <summary>
    /// 资源名称
    /// </summary>
    public string? ResourceName { get; set; }

    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType? ResourceType { get; set; }

    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可读
    /// </summary>
    public bool IsReadable { get; set; }

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool IsEditable { get; set; }

    /// <summary>
    /// 脱敏策略
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 策略描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
