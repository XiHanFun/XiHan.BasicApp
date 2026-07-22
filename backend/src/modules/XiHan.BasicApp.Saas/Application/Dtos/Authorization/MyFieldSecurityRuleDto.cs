// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 当前用户的字段脱敏规则（按资源下发，前端据此渲染脱敏）
/// </summary>
public sealed class MyFieldSecurityRuleDto
{
    /// <summary>
    /// 字段名（与前端列 key 对应）
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可读（false 表示整体隐藏）
    /// </summary>
    public bool IsReadable { get; set; } = true;

    /// <summary>
    /// 是否可编辑（false 表示表单只读 / 后端拒绝修改）
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// 脱敏策略（对应后端 FieldMaskStrategy：0 不脱敏 / 2 全掩码 / 3 部分掩码 / 99 自定义）
    /// </summary>
    public int MaskStrategy { get; set; }

    /// <summary>
    /// 脱敏规则描述（部分/自定义策略可用）
    /// </summary>
    public string? MaskPattern { get; set; }
}
