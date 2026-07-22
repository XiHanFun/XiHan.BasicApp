// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 字段级安全更新 DTO
/// </summary>
public sealed class FieldLevelSecurityUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public FieldSecurityTargetType TargetType { get; set; } = FieldSecurityTargetType.Role;

    /// <summary>
    /// 目标主键
    /// </summary>
    public long TargetId { get; set; }

    /// <summary>
    /// 资源主键
    /// </summary>
    public long ResourceId { get; set; }

    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可读
    /// </summary>
    public bool IsReadable { get; set; } = true;

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// 脱敏策略
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; set; } = FieldMaskStrategy.None;

    /// <summary>
    /// 脱敏模式
    /// </summary>
    public string? MaskPattern { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 策略描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
