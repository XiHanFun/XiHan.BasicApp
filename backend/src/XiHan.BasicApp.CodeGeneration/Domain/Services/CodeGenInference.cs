// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions.Schema;

namespace XiHan.BasicApp.CodeGeneration.Domain.Services;

/// <summary>
/// 按库表结构推出的表配置
/// </summary>
public sealed class CodeGenTableSuggestion
{
    /// <summary>
    /// 实体类名
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// 包的根命名空间（沿用已有实体时才有）
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// 包名（沿用已有实体时才有）
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// 业务名
    /// </summary>
    public string? BusinessName { get; set; }

    /// <summary>
    /// 功能名
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// 主键列名
    /// </summary>
    public string? PrimaryKeyColumn { get; set; }

    /// <summary>
    /// 生成模式
    /// </summary>
    public CodeGenTemplateType TemplateType { get; set; } = CodeGenTemplateType.Single;

    /// <summary>
    /// 树表的父级列名
    /// </summary>
    public string? TreeParentColumn { get; set; }

    /// <summary>
    /// 树表的名称列名
    /// </summary>
    public string? TreeNameColumn { get; set; }

    /// <summary>
    /// 是否由已登记的实体建出
    /// </summary>
    public bool FromRegisteredEntity { get; set; }

    /// <summary>
    /// 列
    /// </summary>
    public IReadOnlyList<CodeGenColumnSuggestion> Columns { get; set; } = [];
}

/// <summary>
/// 按库列结构推出的列配置
/// </summary>
public sealed class CodeGenColumnSuggestion
{
    /// <summary>
    /// 列名
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 列注释
    /// </summary>
    public string? ColumnComment { get; set; }

    /// <summary>
    /// 库里的类型名
    /// </summary>
    public string? ColumnType { get; set; }

    /// <summary>
    /// C# 类型
    /// </summary>
    public string CSharpType { get; set; } = "string";

    /// <summary>
    /// C# 属性名
    /// </summary>
    public string CSharpProperty { get; set; } = string.Empty;

    /// <summary>
    /// TypeScript 类型
    /// </summary>
    public string TsType { get; set; } = "string";

    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否自增
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// 是否可空
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 长度（只有字符与二进制类型有）
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? DecimalDigits { get; set; }

    /// <summary>
    /// 是否实体基类的列
    /// </summary>
    public bool IsBase { get; set; }

    /// <summary>
    /// 是否在列表里显示
    /// </summary>
    public bool IsList { get; set; } = true;

    /// <summary>
    /// 是否进新建表单
    /// </summary>
    public bool IsInsert { get; set; } = true;

    /// <summary>
    /// 是否进编辑表单
    /// </summary>
    public bool IsEdit { get; set; } = true;

    /// <summary>
    /// 是否可查询
    /// </summary>
    public bool IsQuery { get; set; }

    /// <summary>
    /// 表单控件
    /// </summary>
    public CodeGenHtmlType HtmlType { get; set; } = CodeGenHtmlType.Input;

    /// <summary>
    /// 查询方式
    /// </summary>
    public CodeGenQueryType QueryType { get; set; } = CodeGenQueryType.Equal;

    /// <summary>
    /// 选项来源
    /// </summary>
    public CodeGenDictSelectorType? DictSelectorType { get; set; }

    /// <summary>
    /// 枚举类型全名
    /// </summary>
    public string? EnumTypeName { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// 按库表结构推断表与列配置
/// </summary>
/// <remarks>
/// 类名去掉表名前缀后转 PascalCase，业务名取表注释（去掉末尾的「表」），
/// 列的类型按库映射（<see cref="CodeGenTypeMapping"/>），字符类的列按列名语义推控件，含名称、编码、状态、类型、时间、日期的列缺省可查询，
/// 有父级列与名称列即推为树表。基类的列（主键、租户、审计、软删与并发戳，取自实体基类的属性）不进表单与列表
/// 「沿用已有实体」按登记的实体识别（框架的 <see cref="IEntityCatalog"/>）：只认主库的表，
/// 类名、命名空间、包名与属性名取实体，枚举属性推为枚举选项
/// </remarks>
/// <param name="catalog">框架的实体目录</param>
/// <param name="options">代码生成选项</param>
public sealed class CodeGenInference(IEntityCatalog catalog, CodeGenerationOptions options)
{
    private static readonly string[] _treeParentCandidates = ["ParentId", "Pid", "ParentCode"];
    private static readonly string[] _treeNameCandidates = ["Name", "Title", "Label", "DisplayName"];
    private static readonly string[] _queryKeywords = ["name", "title", "code", "status", "state", "type", "time", "date"];

    private static readonly (string[] Keywords, CodeGenHtmlType Html)[] _semantics =
    [
        (["icon", "img", "image", "avatar", "logo", "photo", "cover", "banner"], CodeGenHtmlType.ImageUpload),
        (["pic", "picture"], CodeGenHtmlType.ImageUpload),
        (["file", "attachment", "annex"], CodeGenHtmlType.FileUpload),
        (["color", "colour"], CodeGenHtmlType.Input),
        (["content", "description", "remark", "detail", "intro", "summary", "note"], CodeGenHtmlType.Textarea),
    ];

    /// <summary>
    /// 实体基类的列（去掉下划线、不分大小写）
    /// </summary>
    public static IReadOnlySet<string> BaseColumns { get; } = typeof(TenantScopedAuditedEntity)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(static p => p.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 是否实体基类的列
    /// </summary>
    /// <param name="columnName">列名</param>
    public static bool IsBaseColumn(string columnName)
        => !string.IsNullOrWhiteSpace(columnName) && BaseColumns.Contains(columnName.Replace("_", string.Empty, StringComparison.Ordinal));

    /// <summary>
    /// 推断一张表
    /// </summary>
    /// <param name="schema">读库结构得到的表</param>
    /// <param name="database">数据库类型</param>
    /// <param name="isPrimaryDatabase">是否主库的表（只有主库的表才沿用已有实体）</param>
    /// <param name="author">作者（调用方的名字）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<CodeGenTableSuggestion> InferAsync(TableSchema schema, CodeGenDatabaseType database, bool isPrimaryDatabase, string? author, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var entity = isPrimaryDatabase
            ? (await catalog.ListAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(m => string.Equals(m.Table, schema.Table.Name, StringComparison.OrdinalIgnoreCase))
            : null;
        var properties = entity is null
            ? new Dictionary<string, EntityColumnMapping>(StringComparer.OrdinalIgnoreCase)
            : entity.Columns.ToDictionary(static c => c.Column, StringComparer.OrdinalIgnoreCase);
        var className = entity?.EntityType.Name ?? CodeGenNaming.Pascalize(CodeGenNaming.StripPrefix(schema.Table.Name, options.TablePrefixes));
        var (root, module) = entity is null ? (null, null) : NamespaceOf(entity.EntityType);
        var business = BusinessNameOf(schema.Table.Comment, className);
        var suggestion = new CodeGenTableSuggestion
        {
            ClassName = className,
            Namespace = root,
            ModuleName = module,
            BusinessName = business,
            FunctionName = business,
            Author = string.IsNullOrWhiteSpace(author) ? null : author,
            PrimaryKeyColumn = schema.Columns.FirstOrDefault(static c => c.IsPrimaryKey)?.Name,
            FromRegisteredEntity = entity is not null,
            Columns = [.. schema.Columns.Select((column, index) => InferColumn(column, index, database, properties))],
        };
        ApplyTree(suggestion, schema);
        return suggestion;
    }

    private static CodeGenColumnSuggestion InferColumn(ColumnInfo column, int index, CodeGenDatabaseType database, Dictionary<string, EntityColumnMapping> properties)
    {
        var mapped = CodeGenTypeMapping.Map(database, column);
        var isBase = IsBaseColumn(column.Name);
        var editable = !isBase && !column.IsPrimaryKey;
        var suggestion = new CodeGenColumnSuggestion
        {
            ColumnName = column.Name,
            ColumnComment = column.Comment,
            ColumnType = column.DataType,
            CSharpType = mapped.CSharpType,
            CSharpProperty = properties.TryGetValue(column.Name, out var property) ? property.Property : CodeGenNaming.Pascalize(column.Name),
            TsType = mapped.TsType,
            IsPrimaryKey = column.IsPrimaryKey,
            IsIdentity = column.IsIdentity,
            IsNullable = column.IsNullable,
            IsRequired = !column.IsNullable && !column.IsIdentity && !column.IsPrimaryKey,
            Length = column.Length,
            DecimalDigits = column.Scale,
            IsBase = isBase,
            IsList = editable,
            IsInsert = editable,
            IsEdit = editable,
            IsQuery = editable && _queryKeywords.Any(keyword => column.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)),
            HtmlType = mapped.HtmlType,
            QueryType = mapped.QueryType,
            Order = index,
        };

        // 字符类的列按列名语义推控件（数值、布尔、时间由类型决定）；判据是 C# 类型，long 的 TypeScript 也是字符串
        if (string.Equals(mapped.CSharpType.TrimEnd('?'), "string", StringComparison.Ordinal) && SemanticHtmlOf(column.Name, column.Length) is { } html)
        {
            suggestion.HtmlType = html;
        }

        // 沿用已有实体：属性是枚举即枚举选项、下拉控件，C# 类型取枚举名（线上按成员名传）
        if (property is not null && (Nullable.GetUnderlyingType(property.Type) ?? property.Type) is { IsEnum: true } enumType)
        {
            suggestion.DictSelectorType = CodeGenDictSelectorType.EnumSelector;
            suggestion.EnumTypeName = enumType.FullName;
            suggestion.HtmlType = CodeGenHtmlType.Select;
            suggestion.QueryType = CodeGenQueryType.Equal;
            suggestion.CSharpType = column.IsNullable ? enumType.Name + "?" : enumType.Name;
            suggestion.TsType = "string";
        }

        return suggestion;
    }

    private static CodeGenHtmlType? SemanticHtmlOf(string columnName, int? length)
    {
        foreach (var (keywords, html) in _semantics)
        {
            if (keywords.Any(keyword => columnName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return html;
            }
        }

        // 超长的字符列即使名字没有语义，也更适合多行输入
        return length is > 500 ? CodeGenHtmlType.Textarea : null;
    }

    private static void ApplyTree(CodeGenTableSuggestion suggestion, TableSchema schema)
    {
        var parent = schema.Columns.FirstOrDefault(c => !c.IsPrimaryKey && Matches(c.Name, _treeParentCandidates, suggestion.ClassName + "ParentId"));
        var name = schema.Columns.FirstOrDefault(c => Matches(c.Name, _treeNameCandidates, suggestion.ClassName + "Name"));
        // 有父级列却找不到名称列不判为树表，免得生成出展开不了的树页面
        if (parent is null || name is null)
        {
            return;
        }

        suggestion.TemplateType = CodeGenTemplateType.Tree;
        suggestion.TreeParentColumn = parent.Name;
        suggestion.TreeNameColumn = name.Name;
    }

    private static bool Matches(string columnName, string[] candidates, string classSpecific)
    {
        var normalized = columnName.Replace("_", string.Empty, StringComparison.Ordinal);
        return candidates.Any(c => string.Equals(normalized, c, StringComparison.OrdinalIgnoreCase)) || string.Equals(normalized, classSpecific, StringComparison.OrdinalIgnoreCase);
    }

    private static (string? Root, string? Module) NamespaceOf(Type entityType)
    {
        var ns = entityType.Namespace;
        if (string.IsNullOrWhiteSpace(ns))
        {
            return (null, null);
        }

        var domain = ns.IndexOf(".Domain", StringComparison.Ordinal);
        var root = domain > 0 ? ns[..domain] : ns;
        var dot = root.LastIndexOf('.');
        return (root, dot >= 0 && dot < root.Length - 1 ? root[(dot + 1)..] : root);
    }

    private static string BusinessNameOf(string? comment, string className)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return className;
        }

        var trimmed = comment.Trim();
        if (trimmed.EndsWith('表'))
        {
            trimmed = trimmed[..^1];
        }

        return string.IsNullOrWhiteSpace(trimmed) ? className : trimmed;
    }
}
