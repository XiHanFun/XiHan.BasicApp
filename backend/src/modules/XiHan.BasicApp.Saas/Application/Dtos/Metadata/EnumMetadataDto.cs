#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnumMetadataDto
// Guid:d8e3c7a1-2f5b-4e90-bc16-9a73f2d4a8c1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos.Metadata;

/// <summary>
/// 枚举元数据 DTO
/// 用于向前端暴露枚举类型的完整定义，消除前端硬编码枚举选项
/// </summary>
public sealed class EnumMetadataDto
{
    /// <summary>
    /// 枚举类型名称
    /// </summary>
    /// <example>UserGender</example>
    public required string EnumTypeName { get; init; }

    /// <summary>
    /// 枚举类型显示名称
    /// </summary>
    /// <example>用户性别</example>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 枚举项列表
    /// </summary>
    public required List<EnumItemDto> Items { get; init; }
}

/// <summary>
/// 枚举项 DTO
/// 表示枚举中单个成员的值、名称与显示文本
/// </summary>
public sealed class EnumItemDto
{
    /// <summary>
    /// 枚举成员名称
    /// </summary>
    /// <example>Male</example>
    public required string Name { get; init; }

    /// <summary>
    /// 枚举数值
    /// </summary>
    /// <example>1</example>
    public int Value { get; init; }

    /// <summary>
    /// 枚举成员显示名称
    /// 来源于枚举字段上的 [Description] 特性
    /// </summary>
    /// <example>男</example>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 枚举成员描述
    /// 来源于枚举字段上的 XML 文档注释，用于 Tooltip 等辅助说明
    /// </summary>
    /// <example>男性用户</example>
    public string? Description { get; init; }
}
