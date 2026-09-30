// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 字段级安全创建 DTO
/// </summary>
public sealed class FieldLevelSecurityCreateDto
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public FieldSecurityTargetType TargetType { get; set; } = FieldSecurityTargetType.Role;

    /// <summary>
    /// 目标ID（角色/用户/部门）
    /// </summary>
    public long TargetId { get; set; }

    /// <summary>
    /// 实体名（取自字段安全实体目录）
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// 字段名（取自该实体的字段列表）
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 读取方式
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; set; } = FieldMaskStrategy.None;

    /// <summary>
    /// 部分脱敏保留前几位（仅部分脱敏）
    /// </summary>
    public int? MaskKeepHead { get; set; }

    /// <summary>
    /// 部分脱敏保留后几位（仅部分脱敏）
    /// </summary>
    public int? MaskKeepTail { get; set; }

    /// <summary>
    /// 固定文本（仅固定文本方式）
    /// </summary>
    public string? MaskReplacement { get; set; }

    /// <summary>
    /// 是否可编辑（仅明文可设为可编辑）
    /// </summary>
    public bool IsEditable { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
