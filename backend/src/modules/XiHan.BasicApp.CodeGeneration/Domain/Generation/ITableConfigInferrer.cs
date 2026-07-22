// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 表配置推断引擎（DB 结构 + 已注册实体 + 当前用户 → 一份可直接生成的配置建议）
/// </summary>
/// <remarks>
/// "输入最小化"的落地载体：能推断的一律不问。对项目内的表（在 <see cref="IEntityMetadataCatalog"/>
/// 中命中）可读出实体类型，做到近乎零配置；外部库的表走名称/语义约定，约八成可推断。
/// 纯函数、无副作用，便于单测。
/// </remarks>
public interface ITableConfigInferrer
{
    /// <summary>
    /// 推断表与列配置
    /// </summary>
    /// <param name="schema">DbFirst 扫描结果</param>
    /// <param name="context">推断上下文（当前用户、数据库类型、全局表前缀等）</param>
    TableConfigSuggestion Infer(TableSchema schema, InferenceContext context);
}

/// <summary>
/// 推断上下文
/// </summary>
/// <param name="CurrentUserName">当前登录用户名（作为生成代码作者）</param>
/// <param name="DatabaseType">数据库类型（来自数据源）</param>
/// <remarks>
/// 表前缀是全局配置，由引擎从选项读取，不进上下文——调用方（应用层）无需感知基础设施配置。
/// </remarks>
public sealed record InferenceContext(
    string? CurrentUserName,
    DatabaseType DatabaseType);

/// <summary>
/// 表配置建议
/// </summary>
public sealed class TableConfigSuggestion
{
    /// <summary>实体类名</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>命名空间</summary>
    public string? Namespace { get; set; }

    /// <summary>模块名称</summary>
    public string? ModuleName { get; set; }

    /// <summary>业务名称</summary>
    public string? BusinessName { get; set; }

    /// <summary>功能名称</summary>
    public string? FunctionName { get; set; }

    /// <summary>作者</summary>
    public string? Author { get; set; }

    /// <summary>模板类型（自关联检测：命中则树表，否则单表）</summary>
    public TemplateType TemplateType { get; set; } = TemplateType.Single;

    /// <summary>主键列名</summary>
    public string? PrimaryKeyColumn { get; set; }

    /// <summary>树表父级列名（仅 TemplateType.Tree 时有值）</summary>
    public string? TreeParentColumn { get; set; }

    /// <summary>树表显示名列名（仅 TemplateType.Tree 时有值）</summary>
    public string? TreeNameColumn { get; set; }

    /// <summary>是否命中项目内实体（true = 高置信推断，false = 外部表走名称约定）</summary>
    public bool FromRegisteredEntity { get; set; }

    /// <summary>列配置建议</summary>
    public IReadOnlyList<ColumnConfigSuggestion> Columns { get; set; } = [];
}

/// <summary>
/// 列配置建议
/// </summary>
public sealed class ColumnConfigSuggestion
{
    /// <summary>数据库列名</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>列注释</summary>
    public string? ColumnComment { get; set; }

    /// <summary>数据库列类型</summary>
    public string? ColumnType { get; set; }

    /// <summary>C# 类型</summary>
    public string CSharpType { get; set; } = "string";

    /// <summary>C# 属性名</summary>
    public string CSharpProperty { get; set; } = string.Empty;

    /// <summary>TypeScript 类型</summary>
    public string TsType { get; set; } = "string";

    /// <summary>是否主键</summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>是否自增</summary>
    public bool IsIdentity { get; set; }

    /// <summary>是否可空</summary>
    public bool IsNullable { get; set; }

    /// <summary>是否必填</summary>
    public bool IsRequired { get; set; }

    /// <summary>长度</summary>
    public int? Length { get; set; }

    /// <summary>小数位</summary>
    public int? DecimalDigits { get; set; }

    /// <summary>是否通用列（基类托管：主键/租户/审计/软删）</summary>
    public bool IsCommon { get; set; }

    /// <summary>列表显示</summary>
    public bool IsList { get; set; } = true;

    /// <summary>新增</summary>
    public bool IsInsert { get; set; } = true;

    /// <summary>编辑</summary>
    public bool IsEdit { get; set; } = true;

    /// <summary>查询</summary>
    public bool IsQuery { get; set; }

    /// <summary>表单控件</summary>
    public HtmlType HtmlType { get; set; } = HtmlType.Input;

    /// <summary>查询方式</summary>
    public QueryType QueryType { get; set; } = QueryType.Equal;

    /// <summary>字典选择器类型（枚举列自动 EnumSelector）</summary>
    public DictSelectorType? DictSelectorType { get; set; }

    /// <summary>枚举类型全名（EnumSelector 时填充）</summary>
    public string? EnumTypeName { get; set; }

    /// <summary>排序（列序）</summary>
    public int Sort { get; set; }
}
