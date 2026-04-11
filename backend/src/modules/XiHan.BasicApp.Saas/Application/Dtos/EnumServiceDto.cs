#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnumServiceDto
// Guid:d3466f12-8397-4e95-8b1e-8e5948b2583f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 枚举项 DTO
/// </summary>
public sealed class EnumOptionDto
{
    /// <summary>
    /// 枚举字段名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 枚举值
    /// </summary>
    public object Value { get; set; } = default!;

    /// <summary>
    /// 枚举值字符串
    /// </summary>
    public string ValueText { get; set; } = string.Empty;

    /// <summary>
    /// 显示文本
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 原始描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 主题
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// 数据来源（enum/dict）
    /// </summary>
    public string Source { get; set; } = "enum";

    /// <summary>
    /// 扩展字段
    /// </summary>
    public Dictionary<string, object>? Extra { get; set; }
}

/// <summary>
/// 枚举定义 DTO
/// </summary>
public sealed class EnumDefinitionDto
{
    /// <summary>
    /// 枚举短名称
    /// </summary>
    public string EnumName { get; set; } = string.Empty;

    /// <summary>
    /// 枚举完整名称
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// 枚举描述
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 文化
    /// </summary>
    public string CultureName { get; set; } = string.Empty;

    /// <summary>
    /// 是否 Flags 枚举
    /// </summary>
    public bool IsFlags { get; set; }

    /// <summary>
    /// 底层类型名
    /// </summary>
    public string UnderlyingTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 可选项列表
    /// </summary>
    public IReadOnlyList<EnumOptionDto> Items { get; set; } = [];
}

/// <summary>
/// 枚举批量查询 DTO
/// </summary>
public sealed class EnumBatchQueryDto
{
    /// <summary>
    /// 枚举类型名列表（短名或完整名）
    /// </summary>
    public IReadOnlyList<string> EnumNames { get; set; } = [];

    /// <summary>
    /// 语言（如 zh-CN / en-US）
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// 是否包含隐藏项
    /// </summary>
    public bool IncludeHidden { get; set; }

    /// <summary>
    /// 是否启用字典覆盖
    /// </summary>
    public bool IncludeDict { get; set; }

    /// <summary>
    /// 字典编码列表（为空时默认使用枚举名称）
    /// </summary>
    public IReadOnlyList<string>? DictCodes { get; set; }

    /// <summary>
    /// 租户 ID
    /// </summary>
    public long? TenantId { get; set; }
}
