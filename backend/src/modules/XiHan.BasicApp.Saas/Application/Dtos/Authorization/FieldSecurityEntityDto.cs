// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 可配置字段安全的实体
/// </summary>
public sealed class FieldSecurityEntityDto
{
    /// <summary>
    /// 实体名（类名）
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名（表说明）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 可配置字段
    /// </summary>
    public List<FieldSecurityFieldDto> Fields { get; set; } = [];
}

/// <summary>
/// 可配置字段安全的字段
/// </summary>
public sealed class FieldSecurityFieldDto
{
    /// <summary>
    /// 字段名（实体属性名）
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名（列说明）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 是否文本字段：非文本字段只能明文只读或隐藏
    /// </summary>
    public bool IsText { get; set; }
}
