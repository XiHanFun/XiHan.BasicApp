#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DynamicRuntimeDtos
// Guid:c0de9e00-0410-4a00-9000-000000000410
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/21 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

namespace XiHan.BasicApp.CodeGeneration.Application.Dtos;

/// <summary>
/// 零代码运行时字段 schema DTO（单列元数据）
/// </summary>
public sealed class DynamicRuntimeColumnDto
{
    /// <summary>数据库列名</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>前端属性名（camelCase，由 CSharpProperty/ColumnName 推导）</summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>显示标签（=ColumnComment）</summary>
    public string? Label { get; set; }

    /// <summary>TypeScript 类型</summary>
    public string? TsType { get; set; }

    /// <summary>表单组件类型（HtmlType 枚举名）</summary>
    public string HtmlType { get; set; } = string.Empty;

    /// <summary>查询方式（QueryType 枚举名）</summary>
    public string QueryType { get; set; } = string.Empty;

    /// <summary>是否列表显示</summary>
    public bool IsList { get; set; }

    /// <summary>是否查询字段</summary>
    public bool IsQuery { get; set; }

    /// <summary>是否必填</summary>
    public bool IsRequired { get; set; }
}

/// <summary>
/// 零代码运行时表 schema DTO
/// </summary>
public sealed class DynamicRuntimeSchemaDto
{
    /// <summary>表配置主键</summary>
    public long TableId { get; set; }

    /// <summary>数据库表名</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>实体类名称</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>表描述</summary>
    public string? TableComment { get; set; }

    /// <summary>字段元数据集合</summary>
    public IReadOnlyList<DynamicRuntimeColumnDto> Columns { get; set; } = [];
}

/// <summary>
/// 零代码运行时分页查询 DTO
/// </summary>
public sealed class DynamicRuntimePageQueryDto
{
    /// <summary>表配置主键</summary>
    public long TableId { get; set; }

    /// <summary>页码（从 1 开始）</summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>每页条数</summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 零代码运行时分页结果 DTO
/// </summary>
public sealed class DynamicRuntimePageResultDto
{
    /// <summary>当前页数据行（按列名为键的动态字典）</summary>
    public IReadOnlyList<Dictionary<string, object>> Rows { get; set; } = [];

    /// <summary>总记录数</summary>
    public long TotalCount { get; set; }

    /// <summary>页码（从 1 开始）</summary>
    public int PageIndex { get; set; }

    /// <summary>每页条数</summary>
    public int PageSize { get; set; }
}
