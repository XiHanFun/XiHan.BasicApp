// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.Options;
using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Inference;

/// <summary>
/// 表配置推断引擎实现
/// </summary>
public sealed class TableConfigInferenceEngine(
    IEntityMetadataCatalog catalog,
    ITypeMappingProvider typeMappingProvider,
    IOptions<CodeGenerationOptions> options) : ITableConfigInferrer
{
    /// <summary>
    /// 树表父级列候选名（大小写不敏感）
    /// </summary>
    private static readonly string[] TreeParentCandidates = ["ParentId", "Pid", "ParentCode"];

    /// <summary>
    /// 树表显示名列候选名（大小写不敏感）
    /// </summary>
    private static readonly string[] TreeNameCandidates = ["Name", "Title", "Label", "DisplayName"];

    /// <summary>
    /// 默认参与查询的列名关键字（全列默认查询会让搜索区爆炸，只放常用维度）
    /// </summary>
    private static readonly string[] DefaultQueryKeywords = ["name", "title", "code", "status", "state", "type", "time", "date"];

    private readonly IEntityMetadataCatalog _catalog = catalog;
    private readonly ITypeMappingProvider _typeMappingProvider = typeMappingProvider;
    private readonly CodeGenerationOptions _options = options.Value;

    /// <inheritdoc />
    public TableConfigSuggestion Infer(TableSchema schema, InferenceContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        var fromEntity = _catalog.TryGetEntityType(schema.TableName, out var entityType);

        // 项目内表：建"列名 → 属性"映射，供类名/枚举检测；外部表为空映射
        var propertyMap = fromEntity ? BuildPropertyMap(entityType) : new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        var className = fromEntity
            ? entityType.Name
            : NamingConventions.Pascalize(StripPrefix(schema.TableName, _options.ResolvedTablePrefixes));

        var (rootNamespace, moduleName) = fromEntity
            ? DeriveNamespaceAndModule(entityType)
            : (null, null);

        var businessName = DeriveBusinessName(schema.TableComment, className);

        var suggestion = new TableConfigSuggestion
        {
            ClassName = className,
            Namespace = rootNamespace,
            ModuleName = moduleName,
            BusinessName = businessName,
            FunctionName = businessName,
            Author = string.IsNullOrWhiteSpace(context.CurrentUserName) ? null : context.CurrentUserName,
            PrimaryKeyColumn = schema.PrimaryKeyColumn,
            FromRegisteredEntity = fromEntity,
            TemplateType = TemplateType.Single,
            Columns = [.. schema.Columns.Select((column, index) => InferColumn(column, index, context, propertyMap))]
        };

        ApplyTreeDetection(suggestion, schema);
        return suggestion;
    }

    /// <summary>
    /// 单列推断
    /// </summary>
    private ColumnConfigSuggestion InferColumn(
        ColumnSchema column,
        int index,
        InferenceContext context,
        IReadOnlyDictionary<string, PropertyInfo> propertyMap)
    {
        var mapping = _typeMappingProvider.Map(context.DatabaseType, column.DbType, column.IsNullable);
        var isCommon = GeneratedColumnNames.IsBaseColumn(column.ColumnName);
        var isEditable = !isCommon && !column.IsPrimaryKey;

        var suggestion = new ColumnConfigSuggestion
        {
            ColumnName = column.ColumnName,
            ColumnComment = column.ColumnComment,
            ColumnType = column.DbType,
            CSharpType = mapping.CSharpType,
            CSharpProperty = ResolveProperty(column.ColumnName, propertyMap),
            TsType = mapping.TsType,
            IsPrimaryKey = column.IsPrimaryKey,
            IsIdentity = column.IsIdentity,
            IsNullable = column.IsNullable,
            IsRequired = !column.IsNullable && !column.IsIdentity && !column.IsPrimaryKey,
            Length = column.Length,
            DecimalDigits = column.DecimalDigits,
            IsCommon = isCommon,
            IsList = isEditable,
            IsInsert = isEditable,
            IsEdit = isEditable,
            IsQuery = isEditable && MatchesAny(column.ColumnName, DefaultQueryKeywords),
            HtmlType = mapping.DefaultHtmlType,
            QueryType = mapping.DefaultQueryType,
            Sort = index
        };

        // 文本类列按列名语义把控件推断得更贴合（数值/布尔/时间由类型映射决定，语义无需干预）
        if (mapping.TsType == "string" && ColumnSemanticRules.Infer(column.ColumnName, column.Length) is { } semanticHtml)
        {
            suggestion.HtmlType = semanticHtml;
        }

        // 项目内表：属性是枚举 → 自动 EnumSelector + 枚举全名 + 下拉控件
        if (propertyMap.TryGetValue(column.ColumnName, out var property))
        {
            var enumType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (enumType.IsEnum)
            {
                suggestion.DictSelectorType = DictSelectorType.EnumSelector;
                suggestion.EnumTypeName = enumType.FullName;
                suggestion.HtmlType = HtmlType.Select;
                suggestion.QueryType = QueryType.Equal;
            }
        }

        return suggestion;
    }

    /// <summary>
    /// 自关联检测：命中父级列则判为树表，并解析显示名列
    /// </summary>
    private static void ApplyTreeDetection(TableConfigSuggestion suggestion, TableSchema schema)
    {
        var parent = schema.Columns.FirstOrDefault(column =>
            !column.IsPrimaryKey && MatchesTreeParent(column.ColumnName, suggestion.ClassName));
        if (parent is null)
        {
            return;
        }

        var nameColumn = schema.Columns.FirstOrDefault(column =>
            MatchesTreeName(column.ColumnName, suggestion.ClassName));
        if (nameColumn is null)
        {
            // 有父级列但找不到显示名列：不硬判为树表，避免生成出无法渲染展开列的树页面
            return;
        }

        suggestion.TemplateType = TemplateType.Tree;
        suggestion.TreeParentColumn = parent.ColumnName;
        suggestion.TreeNameColumn = nameColumn.ColumnName;
    }

    /// <summary>
    /// 建"列名（小写）→ 属性"映射（含 SugarColumn 自定义列名）
    /// </summary>
    private static Dictionary<string, PropertyInfo> BuildPropertyMap(Type entityType)
    {
        var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var columnAttribute = property.GetCustomAttribute<SugarColumn>();
            if (columnAttribute is not null && columnAttribute.IsIgnore)
            {
                continue;
            }

            var columnName = columnAttribute is not null && !string.IsNullOrWhiteSpace(columnAttribute.ColumnName)
                ? columnAttribute.ColumnName
                : property.Name;
            map[columnName] = property;
        }

        return map;
    }

    /// <summary>
    /// 列名 → C# 属性名（项目内取属性名，外部表列名转 Pascal）
    /// </summary>
    private static string ResolveProperty(string columnName, IReadOnlyDictionary<string, PropertyInfo> propertyMap)
    {
        return propertyMap.TryGetValue(columnName, out var property)
            ? property.Name
            : NamingConventions.Pascalize(columnName);
    }

    /// <summary>
    /// 从实体命名空间推导生成目标根命名空间与模块名
    /// </summary>
    /// <remarks>
    /// 实体命名空间形如 <c>XiHan.BasicApp.CodeGeneration.Domain.Entities</c>，
    /// 生成目标（模板用 <c>{{ Namespace }}.Domain.Entities</c> 拼接）应为 <c>XiHan.BasicApp.CodeGeneration</c>，
    /// 模块名取其最后一段。
    /// </remarks>
    private static (string? RootNamespace, string? ModuleName) DeriveNamespaceAndModule(Type entityType)
    {
        var ns = entityType.Namespace;
        if (string.IsNullOrWhiteSpace(ns))
        {
            return (null, null);
        }

        var domainIndex = ns.IndexOf(".Domain", StringComparison.Ordinal);
        var root = domainIndex > 0 ? ns[..domainIndex] : ns;
        var lastDot = root.LastIndexOf('.');
        var module = lastDot >= 0 && lastDot < root.Length - 1 ? root[(lastDot + 1)..] : root;
        return (root, module);
    }

    /// <summary>
    /// 表注释 → 业务名（去掉尾部"表"字；空则回退类名）
    /// </summary>
    private static string DeriveBusinessName(string? tableComment, string className)
    {
        if (string.IsNullOrWhiteSpace(tableComment))
        {
            return className;
        }

        var trimmed = tableComment.Trim();
        if (trimmed.EndsWith('表'))
        {
            trimmed = trimmed[..^1];
        }

        return string.IsNullOrWhiteSpace(trimmed) ? className : trimmed;
    }

    /// <summary>
    /// 剥离表前缀（大小写不敏感；命中最长匹配）
    /// </summary>
    private static string StripPrefix(string tableName, IReadOnlyList<string> prefixes)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return tableName;
        }

        var best = prefixes
            .Where(prefix => !string.IsNullOrWhiteSpace(prefix)
                && tableName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && tableName.Length > prefix.Length)
            .OrderByDescending(prefix => prefix.Length)
            .FirstOrDefault();

        return best is null ? tableName : tableName[best.Length..];
    }

    /// <summary>
    /// 列名是否命中树表父级候选（精确匹配候选，或匹配 {类名}ParentId）
    /// </summary>
    private static bool MatchesTreeParent(string columnName, string className)
    {
        var normalized = columnName.Replace("_", string.Empty);
        return TreeParentCandidates.Any(candidate => string.Equals(normalized, candidate, StringComparison.OrdinalIgnoreCase))
            || string.Equals(normalized, $"{className}ParentId", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 列名是否命中树表显示名候选（精确匹配候选，或匹配 {类名}Name）
    /// </summary>
    private static bool MatchesTreeName(string columnName, string className)
    {
        var normalized = columnName.Replace("_", string.Empty);
        return TreeNameCandidates.Any(candidate => string.Equals(normalized, candidate, StringComparison.OrdinalIgnoreCase))
            || string.Equals(normalized, $"{className}Name", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 列名是否包含任一关键字（大小写不敏感）
    /// </summary>
    private static bool MatchesAny(string columnName, string[] keywords)
    {
        var lower = columnName.ToLowerInvariant();
        return keywords.Any(keyword => lower.Contains(keyword, StringComparison.Ordinal));
    }
}
