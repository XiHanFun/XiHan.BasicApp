// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 字段级安全列表项 DTO
/// </summary>
public class FieldLevelSecurityListItemDto : BasicAppDto
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public FieldSecurityTargetType TargetType { get; set; }

    /// <summary>
    /// 目标ID
    /// </summary>
    public long TargetId { get; set; }

    /// <summary>
    /// 目标编码（角色/部门编码）
    /// </summary>
    public string? TargetCode { get; set; }

    /// <summary>
    /// 目标名称
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// 实体名
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// 实体显示名（实体已不再支持字段安全时为空）
    /// </summary>
    public string? EntityDisplayName { get; set; }

    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 字段显示名（字段已不存在时为空）
    /// </summary>
    public string? FieldDisplayName { get; set; }

    /// <summary>
    /// 读取方式
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; set; }

    /// <summary>
    /// 部分脱敏保留前几位
    /// </summary>
    public int? MaskKeepHead { get; set; }

    /// <summary>
    /// 部分脱敏保留后几位
    /// </summary>
    public int? MaskKeepTail { get; set; }

    /// <summary>
    /// 固定文本
    /// </summary>
    public string? MaskReplacement { get; set; }

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool IsEditable { get; set; }

    /// <summary>
    /// 是否平台规则（对所有租户生效，只能在平台维护）
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
