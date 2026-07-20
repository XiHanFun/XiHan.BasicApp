#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenerationModels
// Guid:c0de9e00-0001-4a00-9000-000000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 列类型映射结果（DB 列类型 → 多目标类型 + 默认表单/查询语义）
/// </summary>
/// <param name="CSharpType">C# 类型（如 long、string、DateTimeOffset?）</param>
/// <param name="TsType">TypeScript 类型（如 number、string、boolean）</param>
/// <param name="DefaultHtmlType">默认表单控件</param>
/// <param name="DefaultQueryType">默认查询方式</param>
public sealed record ColumnTypeMapping(
    string CSharpType,
    string TsType,
    HtmlType DefaultHtmlType,
    QueryType DefaultQueryType);

/// <summary>
/// 列结构（引擎内部模型，独立于实体/DTO）
/// </summary>
public sealed class ColumnSchema
{
    /// <summary>数据库列名</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>列注释</summary>
    public string? ColumnComment { get; set; }

    /// <summary>数据库列类型</summary>
    public string? DbType { get; set; }

    /// <summary>C# 类型</summary>
    public string CSharpType { get; set; } = "string";

    /// <summary>C# 属性名（PascalCase）</summary>
    public string CSharpProperty { get; set; } = string.Empty;

    /// <summary>TypeScript 类型</summary>
    public string TsType { get; set; } = "string";

    /// <summary>是否主键</summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>是否自增</summary>
    public bool IsIdentity { get; set; }

    /// <summary>是否可空</summary>
    public bool IsNullable { get; set; }

    /// <summary>是否必填（表单）</summary>
    public bool IsRequired { get; set; }

    /// <summary>长度</summary>
    public int? Length { get; set; }

    /// <summary>小数位</summary>
    public int? DecimalDigits { get; set; }

    /// <summary>表单控件类型</summary>
    public HtmlType HtmlType { get; set; } = HtmlType.Input;

    /// <summary>查询方式</summary>
    public QueryType QueryType { get; set; } = QueryType.Equal;

    /// <summary>字典选择器类型（字典/枚举/常量；空表示非选项列）</summary>
    public DictSelectorType? DictSelectorType { get; set; }

    /// <summary>字典码（DictSelector 时生效）</summary>
    public string? DictCode { get; set; }

    /// <summary>枚举类型全名（EnumSelector 时生效）</summary>
    public string? EnumTypeName { get; set; }

    /// <summary>常量项 JSON（ConstSelector 时生效）</summary>
    public string? ConstValues { get; set; }
}

/// <summary>
/// 表结构（DbFirst 扫描结果 / 引擎建模输入）
/// </summary>
public sealed class TableSchema
{
    /// <summary>数据库表名</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>表注释</summary>
    public string? TableComment { get; set; }

    /// <summary>主键列名</summary>
    public string? PrimaryKeyColumn { get; set; }

    /// <summary>列集合</summary>
    public IReadOnlyList<ColumnSchema> Columns { get; set; } = [];
}

/// <summary>
/// 代码生成上下文（模板可消费的强类型模型）
/// </summary>
/// <remarks>
/// 由表配置 + 列配置 + 类型映射 + 生成选项合并而成，是模板渲染的唯一数据源。
/// </remarks>
public sealed class CodeGenerationContext
{
    /// <summary>数据库表名</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>表注释</summary>
    public string? TableComment { get; set; }

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

    /// <summary>模板类型（单表/树表/主子表）</summary>
    public TemplateType TemplateType { get; set; } = TemplateType.Single;

    /// <summary>主键列</summary>
    public ColumnSchema? PrimaryKey { get; set; }

    /// <summary>列集合</summary>
    public IReadOnlyList<ColumnSchema> Columns { get; set; } = [];

    /// <summary>扩展选项（模板可读取的自定义键值）</summary>
    public IDictionary<string, object?> Options { get; set; } = new Dictionary<string, object?>();
}

/// <summary>
/// 生成产物（单个文件）
/// </summary>
/// <param name="RelativePath">相对路径（含文件名）</param>
/// <param name="FileName">文件名</param>
/// <param name="Content">文件内容</param>
/// <param name="TemplateCode">来源模板编码</param>
/// <param name="WriteMode">写入策略（机器文件总是覆盖；人类文件仅首次创建）</param>
public sealed record GeneratedArtifact(
    string RelativePath,
    string FileName,
    string Content,
    string? TemplateCode,
    ArtifactWriteMode WriteMode = ArtifactWriteMode.AlwaysOverwrite);

/// <summary>
/// 生成请求
/// </summary>
public sealed class GenerationRequest
{
    /// <summary>表配置主键</summary>
    public long TableId { get; set; }

    /// <summary>指定模板编码集合（为空表示按表的 TemplateGroup 取全部启用模板）</summary>
    public IReadOnlyList<string>? TemplateCodes { get; set; }

    /// <summary>生成方式（预览/Zip/落盘）</summary>
    public GenType GenType { get; set; } = GenType.Preview;
}

/// <summary>
/// 生成结果
/// </summary>
public sealed class GenerationResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>消息（失败原因/提示）</summary>
    public string? Message { get; set; }

    /// <summary>产物清单</summary>
    public IReadOnlyList<GeneratedArtifact> Artifacts { get; set; } = [];

    /// <summary>打包字节流（GenType.Zip 时填充）</summary>
    public byte[]? Package { get; set; }

    /// <summary>实际写入文件数（GenType.CustomPath 时填充）</summary>
    public int WrittenCount { get; set; }

    /// <summary>被跳过的人类文件相对路径（GenType.CustomPath 时填充；目标已存在，未覆盖）</summary>
    public IReadOnlyList<string> SkippedPaths { get; set; } = [];

    /// <summary>耗时（毫秒）</summary>
    public long DurationMilliseconds { get; set; }

    /// <summary>创建成功结果</summary>
    public static GenerationResult Ok(IReadOnlyList<GeneratedArtifact> artifacts, long duration, byte[]? package = null) => new()
    {
        Success = true,
        Artifacts = artifacts,
        Package = package,
        DurationMilliseconds = duration
    };

    /// <summary>创建失败结果</summary>
    public static GenerationResult Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}

/// <summary>
/// 模板语法校验结果
/// </summary>
/// <param name="IsValid">是否有效</param>
/// <param name="Errors">错误信息</param>
public sealed record TemplateRenderValidation(bool IsValid, IReadOnlyList<string> Errors)
{
    /// <summary>有效结果</summary>
    public static TemplateRenderValidation Valid() => new(true, []);

    /// <summary>无效结果</summary>
    public static TemplateRenderValidation Invalid(params string[] errors) => new(false, errors);
}
