#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ScribanTemplateRenderer
// Guid:c0de9e00-0301-4a00-9000-000000000301
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.RegularExpressions;
using Scriban;
using Scriban.Runtime;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// Scriban 模板渲染器（直接用原生 Scriban 渲染）
/// </summary>
/// <remarks>
/// 不走框架 ITemplateService：其 string 默认引擎是简单替换引擎、不解析 Scriban 语法（{{ }}/for/if），
/// 会把模板原样输出。这里以原生 Scriban 解析 + ScriptObject 注入变量渲染。
/// </remarks>
public sealed class ScribanTemplateRenderer : ITemplateRenderer
{
    /// <summary>
    /// 基类（BasicAppFullAuditedEntity）托管的列名集合：主键/租户/审计/软删，生成实体属性时应跳过。
    /// 模板可据 col.IsBaseColumn 过滤，只生成业务列。
    /// </summary>
    private static readonly HashSet<string> BaseColumnNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "BasicId", "Id", "TenantId", "IsDeleted",
        "CreatedTime", "CreatedId", "CreatedBy",
        "ModifiedTime", "ModifiedId", "ModifiedBy",
        "DeletedTime", "DeletedId", "DeletedBy"
    };

    /// <inheritdoc />
    public TemplateEngine Engine => TemplateEngine.Scriban;

    /// <inheritdoc />
    public async Task<string> RenderAsync(string templateSource, CodeGenerationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(templateSource))
        {
            return string.Empty;
        }

        var template = Template.Parse(templateSource);
        if (template.HasErrors)
        {
            var message = string.Join("; ", template.Messages.Select(item => item.Message));
            throw new InvalidOperationException($"Scriban 模板解析失败：{message}");
        }

        // 变量以 PascalCase 键直接注入 ScriptObject；关闭成员重命名（Scriban 默认转 snake_case），
        // 模板以确定的 PascalCase 访问（如 {{ ClassName }}、{{ for col in Columns }}{{ col.CSharpProperty }}）。
        var scriptObject = new ScriptObject();
        foreach (var (key, value) in BuildVariables(context))
        {
            scriptObject.SetValue(key, value, true);
        }

        var scribanContext = new TemplateContext { MemberRenamer = member => member.Name };
        scribanContext.PushGlobal(scriptObject);
        return await template.RenderAsync(scribanContext);
    }

    /// <summary>
    /// 上下文 → Scriban 字典模型（PascalCase 键；Columns 为字典列表）
    /// </summary>
    private static IDictionary<string, object?> BuildVariables(CodeGenerationContext context)
    {
        return new Dictionary<string, object?>
        {
            ["TableName"] = context.TableName,
            ["TableComment"] = context.TableComment,
            ["ClassName"] = context.ClassName,
            // 前端文件名/标识用：类名的 camelCase 与 kebab-case
            ["ClassNameCamel"] = Camelize(context.ClassName),
            ["ClassNameKebab"] = Kebabize(context.ClassName),
            ["Namespace"] = context.Namespace,
            ["ModuleName"] = context.ModuleName,
            ["BusinessName"] = context.BusinessName,
            ["FunctionName"] = context.FunctionName,
            ["Author"] = context.Author,
            // 枚举以名称字符串透出，便于模板按名比较（如 {{ if TemplateType == "Tree" }}）
            ["TemplateType"] = context.TemplateType.ToString(),
            ["PrimaryKey"] = context.PrimaryKey is null ? null : BuildColumn(context.PrimaryKey),
            ["Columns"] = context.Columns.Select(BuildColumn).ToList(),
            // 树表结构列（TemplateType == "Tree" 时非空，由引擎 fail-closed 保证）
            ["TreeParentColumn"] = context.TreeParentColumn is null ? null : BuildColumn(context.TreeParentColumn),
            ["TreeNameColumn"] = context.TreeNameColumn is null ? null : BuildColumn(context.TreeNameColumn),
            // 主子表关联（本表为子表时 MasterTable 非空；本表为主表时 DetailTables 非空）
            ["MasterTable"] = context.MasterTable is null ? null : BuildRelatedTable(context.MasterTable),
            ["DetailTables"] = context.DetailTables.Select(BuildRelatedTable).ToList(),
            ["HasDetailTables"] = context.DetailTables.Count > 0,
            ["Options"] = context.Options
        };
    }

    /// <summary>
    /// 关联表引用 → Scriban 字典
    /// </summary>
    private static IDictionary<string, object?> BuildRelatedTable(RelatedTableRef table)
    {
        return new Dictionary<string, object?>
        {
            ["TableId"] = table.TableId.ToString(),
            ["TableName"] = table.TableName,
            ["TableComment"] = table.TableComment,
            ["ClassName"] = table.ClassName,
            ["ClassNameCamel"] = table.ClassNameCamel,
            ["ClassNameKebab"] = table.ClassNameKebab,
            ["ModuleName"] = table.ModuleName,
            ["Namespace"] = table.Namespace,
            ["ForeignKeyColumn"] = table.ForeignKeyColumn,
            ["ForeignKeyProperty"] = table.ForeignKeyProperty,
            ["Columns"] = table.Columns.Select(BuildColumn).ToList()
        };
    }

    /// <summary>
    /// 列 → Scriban 字典（标量值；枚举以名称字符串透出）
    /// </summary>
    private static IDictionary<string, object?> BuildColumn(ColumnSchema column)
    {
        return new Dictionary<string, object?>
        {
            ["ColumnName"] = column.ColumnName,
            ["ColumnComment"] = column.ColumnComment,
            ["DbType"] = column.DbType,
            ["CSharpType"] = column.CSharpType,
            ["CSharpProperty"] = column.CSharpProperty,
            // 前端属性名（camelCase，对应后端 camelCase JSON 序列化）
            ["TsProperty"] = Camelize(column.CSharpProperty),
            ["TsType"] = column.TsType,
            ["IsPrimaryKey"] = column.IsPrimaryKey,
            ["IsIdentity"] = column.IsIdentity,
            ["IsNullable"] = column.IsNullable,
            ["IsRequired"] = column.IsRequired,
            // 基类托管列（主键/审计/软删/租户）：模板生成业务属性时应跳过
            ["IsBaseColumn"] = BaseColumnNames.Contains(column.ColumnName),
            ["Length"] = column.Length,
            ["DecimalDigits"] = column.DecimalDigits,
            ["HtmlType"] = column.HtmlType.ToString(),
            ["QueryType"] = column.QueryType.ToString(),
            // 字典三分（表单选项来源；关联不入生成代码，仅供模板渲染下拉控件）
            ["DictSelectorType"] = column.DictSelectorType?.ToString(),
            ["DictCode"] = column.DictCode,
            ["EnumTypeName"] = column.EnumTypeName,
            ["ConstValues"] = column.ConstValues
        };
    }

    /// <summary>
    /// PascalCase → camelCase（转换实现见 <see cref="NamingConventions"/>，与引擎共用）
    /// </summary>
    private static string Camelize(string value) => NamingConventions.Camelize(value);

    /// <summary>
    /// PascalCase → kebab-case（转换实现见 <see cref="NamingConventions"/>，与引擎共用）
    /// </summary>
    private static string Kebabize(string value) => NamingConventions.Kebabize(value);

    /// <inheritdoc />
    public TemplateRenderValidation Validate(string templateSource)
    {
        if (string.IsNullOrWhiteSpace(templateSource))
        {
            return TemplateRenderValidation.Invalid("模板内容为空");
        }

        var template = Template.Parse(templateSource);
        if (!template.HasErrors)
        {
            return TemplateRenderValidation.Valid();
        }

        var errors = template.Messages
            .Where(item => item.Type == Scriban.Parsing.ParserMessageType.Error)
            .Select(item => item.Message)
            .ToArray();
        return TemplateRenderValidation.Invalid(errors.Length > 0 ? errors : ["模板语法错误"]);
    }
}
