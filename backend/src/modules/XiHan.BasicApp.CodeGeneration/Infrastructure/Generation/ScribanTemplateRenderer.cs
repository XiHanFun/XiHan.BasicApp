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
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.Framework.Templating.Services;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// Scriban 模板渲染器（接通框架 ITemplateService）
/// </summary>
/// <remarks>
/// 这是本轮唯一落地的渲染通道。运行时依赖 Templating 模块注册的 <see cref="ITemplateService"/>。
/// </remarks>
public sealed class ScribanTemplateRenderer(ITemplateService templateService) : ITemplateRenderer
{
    private readonly ITemplateService _templateService = templateService;

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

        // 走框架已验证可用的「字典变量」重载（生产中 MessageTemplateRenderer 同款路径）。
        // 将上下文展开为嵌套字典：键即 Scriban 变量名/成员名，精确匹配、不经成员重命名，
        // 故模板用确定的 PascalCase 访问（如 {{ ClassName }}、{{ for col in Columns }}{{ col.CSharpProperty }}）。
        var variables = BuildVariables(context);
        return await _templateService.RenderAsync(templateSource, variables);
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
            ["Options"] = context.Options
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
    /// PascalCase → camelCase（首字母小写，其余不变）
    /// </summary>
    private static string Camelize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// PascalCase → kebab-case（与前端 toKebabCase 一致：SysProduct → sys-product）
    /// </summary>
    private static string Kebabize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = Regex.Replace(value, "([A-Z]+)([A-Z][a-z])", "$1-$2");
        result = Regex.Replace(result, "([a-z0-9])([A-Z])", "$1-$2");
        return result.Replace('_', '-').ToLowerInvariant();
    }

    /// <inheritdoc />
    public TemplateRenderValidation Validate(string templateSource)
    {
        if (string.IsNullOrWhiteSpace(templateSource))
        {
            return TemplateRenderValidation.Invalid("模板内容为空");
        }

        var validation = _templateService.ValidateTemplate(templateSource);
        return validation.IsValid
            ? TemplateRenderValidation.Valid()
            : TemplateRenderValidation.Invalid(validation.ErrorMessage ?? "模板语法错误");
    }
}
