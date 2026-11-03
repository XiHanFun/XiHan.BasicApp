// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.CodeDom.Compiler;
using System.Reflection;
using System.Text.RegularExpressions;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.Abstractions.Schema;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 由表配置与列配置建出生成的上下文。
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>BuildContextAsync</c>：树表、主从与关联的结构字段在这里解析成列模型，解析不出来即报错、不静默降级（否则模板渲染出引用了不存在属性的代码，要到编译时才暴露）。
/// 与 4.x 不同（6.0 的写法）：主键是 Guid，树表的父级列与关联列要 <c>Guid</c>（4.x 要 long）；状态切换要一列启停枚举（有 <c>Active</c> 与 <c>Disabled</c>，4.x 是全局的 <c>EnableStatus</c>），新建实体时属性名为 <c>Status</c>、没选枚举的整数列由生成器在实体里出 <c>&lt;类&gt;Status</c>；
/// 包名、资源名、权限码与页面码由包的命名空间与类名推出；父菜单去掉（页面登记在包的 <c>Pages</c> 里）。
/// </remarks>
/// <param name="tableService">表配置服务。</param>
/// <param name="tables">表配置仓储。</param>
/// <param name="entities">框架登记的实体。</param>
/// <param name="enums">枚举目录。</param>
public sealed partial class CodeGenContextBuilder(CodeGenTableService tableService, IRepository<SysCodeGenTable, Guid> tables, IEntityCatalog entities, CodeGenEnumCatalog enums)
{
    /// <summary>
    /// 生成器给它产出的实体打的工具名：据此区分生成的与手写的。
    /// </summary>
    public const string GeneratedCodeTool = "XiHan.CodeGen";

    private const string AppPrefix = "XiHan.BasicApp.";

    /// <summary>
    /// 建出上下文。
    /// </summary>
    /// <param name="tableId">表配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<Result<CodeGenContext>> BuildAsync(Guid tableId, CancellationToken cancellationToken)
    {
        if (!(await tableService.FindAsync(tableId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var table))
        {
            return CodeGenTableService.NotFound(tableId);
        }

        var mappings = await entities.ListAsync(cancellationToken).ConfigureAwait(false);
        var columns = (await tableService.ColumnsOfAsync(table.Id, cancellationToken).ConfigureAwait(false)).Select(Model).ToList();

        // 表由手写实体建出：沿用它，类名要一致（照 4.x）
        var existing = HandWrittenEntity(table, mappings);
        if (existing is not null && !string.Equals(existing.Name, table.ClassName, StringComparison.Ordinal))
        {
            return Invalid(CodeGenerationMessages.GenerateClassMismatch, ("table", table.TableName), ("entity", existing.FullName), ("expected", existing.Name));
        }

        if (!NamesOf(table).TryGetValue(out var names))
        {
            return NamesOf(table).Error!;
        }

        var actions = CodeGenActions.Normalize(table.EnabledActions);
        if (!CodeGenActions.IsValid([.. actions]))
        {
            return Error.Validation(CodeGenerationErrors.ActionsConflict, CodeGenerationMessages.ActionsConflict);
        }

        var context = new CodeGenContext
        {
            Table = table,
            Namespace = names.Namespace,
            ModuleName = names.Module,
            ModuleCode = names.Module.ToLowerInvariant(),
            Resource = ResourceOf(table.ClassName),
            ResourcePascal = CodeGenNaming.Pascalize(ResourceOf(table.ClassName)),
            DisplayName = DisplayNameOf(table),
            EntityNamespace = existing?.Namespace ?? names.Namespace + ".Domain.Entities",
            ExistingEntity = existing,
            Actions = actions,
            Columns = columns,
            PrimaryKey = columns.FirstOrDefault(static c => c.IsPrimaryKey) ?? columns.FirstOrDefault(c => string.Equals(c.ColumnName, table.PrimaryKeyColumn, StringComparison.OrdinalIgnoreCase)),
        };

        var checks = ValidateSelectors(columns)
            ?? await ResolveRelationsAsync(table, columns, mappings, cancellationToken).ConfigureAwait(false)
            ?? ValidateUnique(columns)
            ?? (context.Can(CodeGenActions.Status) ? ResolveStatus(table, context) : null)
            ?? (table.TemplateType == CodeGenTemplateType.Tree ? ResolveTree(table, context) : null);
        if (checks is not null)
        {
            return checks;
        }

        if (table.TemplateType == CodeGenTemplateType.MasterDetail)
        {
            var master = await ResolveMasterAsync(table, columns, cancellationToken).ConfigureAwait(false);
            if (!master.TryGetValue(out var masterTable))
            {
                return master.Error!;
            }

            context.MasterTable = masterTable;
        }

        // 以本表为主表的明细表：与本表自己的生成模式无关；外键没配好的跳过（主表照常生成，照 4.x）
        context.DetailTables = await DetailTablesAsync(table, cancellationToken).ConfigureAwait(false);
        context.ReferencedPackages = columns
            .Where(static c => c.IsBusiness)
            .SelectMany(static c => new[] { c.Enum?.Assembly, c.Relation?.EntityAssembly })
            .Where(a => a is not null && a.StartsWith(AppPrefix, StringComparison.Ordinal) && a != names.Namespace && a is not (AppPrefix + "Contracts" or AppPrefix + "Runtime"))
            .Select(static a => a!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        return context;
    }

    /// <summary>
    /// 类名的资源名：kebab 写法、末段取复数（如 <c>SalesOrder</c> → <c>sales-orders</c>）。
    /// </summary>
    /// <param name="className">类名。</param>
    public static string ResourceOf(string className)
    {
        var kebab = CodeGenNaming.Kebabize(className);
        var dash = kebab.LastIndexOf('-');
        var (head, last) = dash < 0 ? (string.Empty, kebab) : (kebab[..(dash + 1)], kebab[(dash + 1)..]);
        return head + Plural(last);
    }

    /// <summary>
    /// 界面上的业务对象名。
    /// </summary>
    /// <remarks>
    /// 业务名，没有即表注释，再没有即类名（照 4.x）。
    /// </remarks>
    /// <param name="table">表配置。</param>
    public static string DisplayNameOf(SysCodeGenTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        return CodeGenDataSourceService.Trimmed(table.BusinessName) ?? CodeGenDataSourceService.Trimmed(table.TableComment) ?? table.ClassName;
    }

    /// <summary>
    /// 包的命名空间与短名。
    /// </summary>
    /// <remarks>
    /// 命名空间没填按 <c>XiHan.BasicApp.&lt;包名&gt;</c>，包名没填取命名空间末段。
    /// </remarks>
    /// <param name="table">表配置。</param>
    public static Result<(string Namespace, string Module)> NamesOf(SysCodeGenTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var ns = CodeGenDataSourceService.Trimmed(table.Namespace);
        var module = CodeGenDataSourceService.Trimmed(table.ModuleName);
        module ??= ns?[(ns.LastIndexOf('.') + 1)..];
        if (module is null || !ModulePattern().IsMatch(module))
        {
            return Invalid(CodeGenerationMessages.GenerateModuleInvalid, ("module", module ?? string.Empty));
        }

        ns ??= AppPrefix + module;
        return NamespacePattern().IsMatch(ns) ? (ns, module) : Invalid(CodeGenerationMessages.GenerateNamespaceInvalid, ("namespace", ns));
    }

    private static Error Invalid(string message, params (string Name, object? Value)[] arguments)
        => Error.Validation(CodeGenerationErrors.GenerationInvalid, message, MessageArguments.Of(arguments));

    private static string Plural(string word)
    {
        if (word.Length == 0)
        {
            return word;
        }

        if (word.EndsWith('y') && word.Length > 1 && !"aeiou".Contains(word[^2], StringComparison.Ordinal))
        {
            return word[..^1] + "ies";
        }

        return word.EndsWith('s') || word.EndsWith('x') || word.EndsWith('z') || word.EndsWith("ch", StringComparison.Ordinal) || word.EndsWith("sh", StringComparison.Ordinal)
            ? word + "es"
            : word + "s";
    }

    // 主库的表登记过实体、且不是生成器自己产出的（带 GeneratedCode 的照常重新生成，照 4.x）
    private static Type? HandWrittenEntity(SysCodeGenTable table, IReadOnlyList<EntityMapping> mappings)
    {
        if (table.DataSourceId is not null)
        {
            return null;
        }

        var entity = mappings.FirstOrDefault(m => string.Equals(m.Table, table.TableName, StringComparison.OrdinalIgnoreCase))?.EntityType;
        return entity is null || entity.GetCustomAttributes<GeneratedCodeAttribute>(inherit: false).Any(static a => a.Tool == GeneratedCodeTool) ? null : entity;
    }

    private CodeGenColumnModel Model(SysCodeGenTableColumn column)
    {
        var model = new CodeGenColumnModel
        {
            ColumnName = column.ColumnName,
            ColumnComment = column.ColumnComment,
            DbType = column.ColumnType,
            CSharpType = CodeGenDataSourceService.Trimmed(column.CSharpType) ?? "string",
            CSharpProperty = CodeGenDataSourceService.Trimmed(column.CSharpProperty) ?? CodeGenNaming.Pascalize(column.ColumnName),
            TsType = CodeGenDataSourceService.Trimmed(column.TsType) ?? "string",
            IsPrimaryKey = column.IsPrimaryKey,
            IsIdentity = column.IsIdentity,
            IsNullable = column.IsNullable,
            IsRequired = column.IsRequired,
            IsUnique = column.IsUnique,
            IsList = column.IsList,
            IsInsert = column.IsInsert,
            IsEdit = column.IsEdit,
            IsQuery = column.IsQuery,
            Length = column.ColumnLength,
            DecimalDigits = column.DecimalDigits,
            HtmlType = column.HtmlType,
            QueryType = column.QueryType,
            DictSelectorType = column.DictSelectorType,
            DictCode = column.DictCode,
            EnumTypeName = column.EnumTypeName,
            ConstValues = column.ConstValues,
            RelationTableId = column.RelationTableId,
            RelationLabelColumn = column.RelationLabelColumn,
            IsBaseColumn = CodeGenInference.IsBaseColumn(column.ColumnName),
        };

        // 照 4.x：C# 类型等于解析出的枚举短名才接上（旧配置还是 int 的不被动改，重新同步之后才变）
        if (model.DictSelectorType == CodeGenDictSelectorType.EnumSelector
            && enums.TryResolve(model.EnumTypeName, out var facts)
            && string.Equals(model.CSharpType.TrimEnd('?'), facts.ShortName, StringComparison.Ordinal))
        {
            model.Enum = facts;
        }

        return model;
    }

    // 系统字典按字典项编码存储，列要是文本、要有字典码（照 4.x）
    private static Error? ValidateSelectors(IReadOnlyList<CodeGenColumnModel> columns)
    {
        foreach (var column in columns.Where(static c => c.IsBusiness && c.DictSelectorType == CodeGenDictSelectorType.DictSelector))
        {
            if (string.IsNullOrWhiteSpace(column.DictCode))
            {
                return Invalid(CodeGenerationMessages.GenerateDictCodeMissing, ("column", column.ColumnName));
            }

            if (column.CSharpType.TrimEnd('?') != "string")
            {
                return Invalid(CodeGenerationMessages.GenerateDictNotString, ("column", column.ColumnName), ("type", column.CSharpType));
            }
        }

        return null;
    }

    private async Task<Error?> ResolveRelationsAsync(SysCodeGenTable table, IReadOnlyList<CodeGenColumnModel> columns, IReadOnlyList<EntityMapping> mappings, CancellationToken cancellationToken)
    {
        foreach (var column in columns.Where(static c => c.IsBusiness && c.DictSelectorType is CodeGenDictSelectorType.TableSelector or CodeGenDictSelectorType.TreeSelector))
        {
            if (column.CSharpType.TrimEnd('?') != nameof(Guid))
            {
                return Invalid(CodeGenerationMessages.GenerateRelationNotGuid, ("column", column.ColumnName), ("type", column.CSharpType));
            }

            if (column.RelationTableId is not { } targetId)
            {
                return Invalid(CodeGenerationMessages.GenerateRelationTargetInvalid, ("column", column.ColumnName));
            }

            var target = targetId == table.Id ? table : (await tables.FindAsync(targetId, cancellationToken).ConfigureAwait(false)).ValueOr(null!);
            if (target is null)
            {
                return Invalid(CodeGenerationMessages.GenerateRelationTargetInvalid, ("column", column.ColumnName));
            }

            var targetColumns = target.Id == table.Id ? columns : [.. (await tableService.ColumnsOfAsync(target.Id, cancellationToken).ConfigureAwait(false)).Select(Model)];
            var isTree = column.DictSelectorType == CodeGenDictSelectorType.TreeSelector;
            string? parentProperty = null;
            if (isTree)
            {
                var parent = Find(targetColumns, target.TreeParentColumn);
                if (target.TemplateType != CodeGenTemplateType.Tree || parent is null || parent.CSharpType.TrimEnd('?') != nameof(Guid))
                {
                    return Invalid(CodeGenerationMessages.GenerateRelationTreeInvalid, ("column", column.ColumnName), ("target", target.TableName));
                }

                parentProperty = parent.CSharpProperty;
            }

            // 关联树的显示列缺省取目标树表的名称列（照 4.x）
            var labelName = CodeGenDataSourceService.Trimmed(column.RelationLabelColumn) ?? (isTree ? target.TreeNameColumn : null);
            var label = Find(targetColumns, labelName);
            if (label is null || label.CSharpType.TrimEnd('?') != "string")
            {
                return Invalid(CodeGenerationMessages.GenerateRelationLabelInvalid, ("column", column.ColumnName), ("label", labelName ?? string.Empty));
            }

            // 目标表已有实体时用它的真实类型，否则按目标表配置推出它生成后的位置
            var targetEntity = HandWrittenEntity(target, mappings);
            var targetNames = NamesOf(target);
            if (targetEntity is null && targetNames.IsFailure)
            {
                return targetNames.Error!;
            }

            column.Relation = new CodeGenRelation
            {
                TableId = target.Id,
                TableName = target.TableName,
                Label = CodeGenDataSourceService.Trimmed(target.TableComment) ?? target.ClassName,
                ClassName = target.ClassName,
                EntityTypeQualified = targetEntity?.FullName ?? $"{targetNames.ValueOrThrow().Namespace}.Domain.Entities.{target.ClassName}",
                EntityAssembly = targetEntity?.Assembly.GetName().Name ?? targetNames.ValueOrThrow().Namespace,
                LabelProperty = label.CSharpProperty,
                ParentProperty = parentProperty,
                IsTree = isTree,
            };
        }

        return null;
    }

    // 二进制与布尔列做不了唯一校验（照 4.x）
    private static Error? ValidateUnique(IReadOnlyList<CodeGenColumnModel> columns)
    {
        var column = columns.FirstOrDefault(static c => c.IsBusiness && c.IsUnique && (c.CSharpType.TrimEnd('?') is "bool" or "byte[]"));
        return column is null ? null : Invalid(CodeGenerationMessages.GenerateUniqueUnsupported, ("column", column.ColumnName), ("type", column.CSharpType));
    }

    // 状态列：启停枚举的业务列，有属性名 Status 的取它，否则要恰好一列；行内启停按列表里的值切换，要进列表。
    // 一列也没有时，属性名为 Status、没选枚举的整数列由生成器在实体里出 <类>Status（Active 0、Disabled 1，同各包的状态枚举）；沿用已有实体时实体是手写的，不出
    private static Error? ResolveStatus(SysCodeGenTable table, CodeGenContext context)
    {
        var candidates = context.Columns.Where(static c => c.IsBusiness && c.Enum is { IsActiveDisabled: true }).ToList();
        if (candidates.Count == 0
            && context.ExistingEntity is null
            && context.Columns.FirstOrDefault(static c => c.IsBusiness && c.CSharpProperty == "Status" && string.IsNullOrWhiteSpace(c.EnumTypeName) && c.CSharpType.TrimEnd('?') is "int" or "long" or "short" or "byte") is { } integer)
        {
            integer.Enum = new CodeGenEnumFacts(table.ClassName + "Status", context.EntityNamespace, context.Namespace, ["Active", "Disabled"]) { IsGenerated = true };
            integer.CSharpType = integer.Enum.ShortName + (integer.CSharpType.EndsWith('?') ? "?" : string.Empty);
            candidates.Add(integer);
        }

        var status = candidates.FirstOrDefault(static c => c.CSharpProperty == "Status") ?? (candidates.Count == 1 ? candidates[0] : null);
        if (status is null)
        {
            return candidates.Count == 0
                ? Invalid(CodeGenerationMessages.GenerateStatusMissing, ("table", table.TableName))
                : Invalid(CodeGenerationMessages.GenerateStatusAmbiguous, ("columns", string.Join(", ", candidates.Select(static c => c.ColumnName))));
        }

        if (!status.IsList)
        {
            return Invalid(CodeGenerationMessages.GenerateStatusNotListed, ("column", status.ColumnName));
        }

        context.StatusColumn = status;
        return null;
    }

    // 树表：父级列要 Guid；名称列承载展开箭头、要进列表（照 4.x）
    private static Error? ResolveTree(SysCodeGenTable table, CodeGenContext context)
    {
        var parent = Find(context.Columns, table.TreeParentColumn);
        if (parent is null || parent.CSharpType.TrimEnd('?') != nameof(Guid))
        {
            return Invalid(CodeGenerationMessages.GenerateTreeParentInvalid, ("table", table.TableName), ("column", table.TreeParentColumn ?? string.Empty));
        }

        var name = Find(context.Columns, table.TreeNameColumn);
        if (name is null || !name.IsList)
        {
            return Invalid(CodeGenerationMessages.GenerateTreeNameInvalid, ("table", table.TableName), ("column", table.TreeNameColumn ?? string.Empty));
        }

        context.TreeParentColumn = parent;
        context.TreeNameColumn = name;
        return null;
    }

    private async Task<Result<CodeGenRelatedTable>> ResolveMasterAsync(SysCodeGenTable table, IReadOnlyList<CodeGenColumnModel> columns, CancellationToken cancellationToken)
    {
        var foreignKey = Find(columns, table.MasterForeignKey);
        var master = table.MasterTableId is { } masterId ? (await tables.FindAsync(masterId, cancellationToken).ConfigureAwait(false)).ValueOr(null!) : null;
        if (master is null || foreignKey is null)
        {
            return Invalid(CodeGenerationMessages.GenerateMasterInvalid, ("table", table.TableName));
        }

        var masterColumns = (await tableService.ColumnsOfAsync(master.Id, cancellationToken).ConfigureAwait(false)).Select(Model).ToList();
        return Related(master, masterColumns, foreignKey);
    }

    private async Task<IReadOnlyList<CodeGenRelatedTable>> DetailTablesAsync(SysCodeGenTable table, CancellationToken cancellationToken)
    {
        var tableId = table.Id;
        var details = await tables.ListAsync(EntityQuery.All<SysCodeGenTable>().Where(new Specification<SysCodeGenTable>(t => t.MasterTableId == tableId)), cancellationToken).ConfigureAwait(false);
        var result = new List<CodeGenRelatedTable>(details.Count);
        foreach (var detail in details.OrderBy(static d => d.TableName, StringComparer.Ordinal))
        {
            var detailColumns = (await tableService.ColumnsOfAsync(detail.Id, cancellationToken).ConfigureAwait(false)).Select(Model).ToList();
            var foreignKey = Find(detailColumns, detail.MasterForeignKey);
            if (foreignKey is not null && NamesOf(detail).IsSuccess)
            {
                result.Add(Related(detail, detailColumns, foreignKey));
            }
        }

        return result;
    }

    private static CodeGenRelatedTable Related(SysCodeGenTable table, IReadOnlyList<CodeGenColumnModel> columns, CodeGenColumnModel foreignKey) => new()
    {
        TableId = table.Id,
        TableName = table.TableName,
        DisplayName = DisplayNameOf(table),
        ClassName = table.ClassName,
        Resource = ResourceOf(table.ClassName),
        ModuleCode = NamesOf(table).TryGetValue(out var names) ? names.Module.ToLowerInvariant() : string.Empty,
        ForeignKeyColumn = foreignKey.ColumnName,
        ForeignKeyProperty = foreignKey.CSharpProperty,
        Columns = columns,
    };

    private static CodeGenColumnModel? Find(IEnumerable<CodeGenColumnModel> columns, string? name)
        => string.IsNullOrWhiteSpace(name) ? null : columns.FirstOrDefault(c => string.Equals(c.ColumnName, name, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*$")]
    private static partial Regex ModulePattern();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$")]
    private static partial Regex NamespacePattern();
}
