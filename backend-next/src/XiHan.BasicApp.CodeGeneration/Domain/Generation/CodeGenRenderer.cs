// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Scriban;
using Scriban.Runtime;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;
using XiHan.BasicApp.Runtime.Templates;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 模板的渲染（Scriban）
/// </summary>
/// <remarks>
/// 照 4.x：变量以 PascalCase 原名注入（关掉 Scriban 缺省的 snake_case 改名），模板写 <c>{{ ClassName }}</c>、<c>{{ for col in Columns }}{{ col.CSharpProperty }}{{ end }}</c>；
/// 注册 10 个转义过滤器（<c>cs_string</c>、<c>xml_doc</c>、<c>ts_string</c>、<c>html_attr</c>、<c>html_text</c>、<c>block_comment</c>、<c>html_comment</c>、<c>i18n_message</c>、<c>js_literal</c>、<c>select_options</c>）
/// 6.0 加的限制（同 Notification 的消息模板）：不给模板加载器（不能 <c>include</c> 别的文件），循环与递归设上限
/// </remarks>
public static class CodeGenRenderer
{
    private const int LoopLimit = 10_000;
    private const int RecursiveLimit = 50;
    private const int MaxStringLength = 65_535;
    private const int DetailPageSize = 100;

    /// <summary>
    /// 检查模板语法
    /// </summary>
    /// <param name="source">模板源文</param>
    /// <returns>错误；没错为空集合</returns>
    public static IReadOnlyList<string> Validate(string? source)
    {
        var template = Template.Parse(source ?? string.Empty);
        if (template.HasErrors)
        {
            return [.. template.Messages.Select(static m => m.ToString())];
        }

        return TemplateLimits.IsTooDeep(template) ? ["The template is nested too deeply."] : [];
    }

    /// <summary>
    /// 渲染一段模板源文
    /// </summary>
    /// <param name="source">模板源文</param>
    /// <param name="model">变量（<see cref="Model"/> 建出的）</param>
    /// <returns>渲染结果</returns>
    /// <exception cref="InvalidOperationException">语法错误</exception>
    /// <exception cref="Scriban.Syntax.ScriptRuntimeException">运行时错误</exception>
    /// <exception cref="CodeGenRenderException">过滤器发现配置问题</exception>
    public static string Render(string? source, ScriptObject model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var template = Template.Parse(source ?? string.Empty);
        if (template.HasErrors)
        {
            throw new InvalidOperationException(string.Join("; ", template.Messages.Select(static m => m.ToString())));
        }

        if (TemplateLimits.IsTooDeep(template))
        {
            throw new InvalidOperationException("The template is nested too deeply.");
        }

        // 模板里赋的变量落在最上面这一层，不带到下一份模板
        var context = new TemplateContext { MemberRenamer = static member => member.Name, LoopLimit = LoopLimit, RecursiveLimit = RecursiveLimit, StrictVariables = false };
        context.PushGlobal(model);
        context.PushGlobal(new ScriptObject());
        return template.Render(context);
    }

    /// <summary>
    /// 由上下文建出模板变量
    /// </summary>
    /// <param name="context">生成上下文</param>
    /// <param name="items">要登记的操作、权限码与错误码</param>
    public static ScriptObject Model(CodeGenContext context, CodeGenItems items)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(items);
        var table = context.Table;
        var columns = context.Columns.Select(c => Column(c, ReferenceEquals(c, context.TreeParentColumn))).ToList();
        var byName = context.Columns.Zip(columns).ToDictionary(static p => p.First, static p => p.Second);
        var model = Object(new Dictionary<string, object?>
        {
            ["TableName"] = table.TableName,
            // 表注释拼进句子：去掉句末标点
            ["TableComment"] = CodeGenItems.PhraseOf(table.TableComment),
            ["DisplayName"] = context.DisplayName,
            ["ClassName"] = table.ClassName,
            ["ClassNameCamel"] = CodeGenNaming.Camelize(table.ClassName),
            ["ClassNameKebab"] = CodeGenNaming.Kebabize(table.ClassName),
            ["ClassNameSnake"] = CodeGenNaming.Snakeize(table.ClassName),
            ["BusinessName"] = table.BusinessName,
            ["FunctionName"] = table.FunctionName,
            ["Author"] = table.Author,
            ["TemplateType"] = table.TemplateType.ToString(),
            ["Target"] = table.Target.ToString(),
            ["IsNewPackage"] = table.Target == CodeGenTarget.NewPackage,
            // 表名与类名按约定换算一致时实体不写 [SugarTable]
            ["NeedsTableAttribute"] = !string.Equals(CodeGenNaming.Snakeize(table.ClassName), table.TableName, StringComparison.Ordinal),
            ["Namespace"] = context.Namespace,
            ["ModuleName"] = context.ModuleName,
            ["ModuleCode"] = context.ModuleCode,
            ["ErrorPrefix"] = context.ModuleName.ToUpperInvariant(),
            ["MessageCatalog"] = "basicapp." + context.ModuleCode,
            ["EntityNamespace"] = context.EntityNamespace,
            ["HasExistingEntity"] = context.ExistingEntity is not null,
            ["Resource"] = context.Resource,
            ["ResourcePascal"] = context.ResourcePascal,
            ["ResourceSnake"] = context.Resource.Replace('-', '_'),
            ["PageCode"] = $"{context.ModuleCode}.{context.Resource}",
            ["PageRoute"] = $"/{context.ModuleCode}/{context.Resource}",
            ["PageTitleKey"] = $"{context.ModuleCode}.{context.Resource.Replace('-', '_')}.title",
            // 前端：契约客户端名、文案键前缀与权限码
            ["ResourceCamel"] = CodeGenNaming.Camelize(context.ResourcePascal),
            ["I18nPrefix"] = $"{context.ModuleCode}.{context.Resource.Replace('-', '_')}",
            ["PermRead"] = PermissionOf(items, "read"),
            ["PermCreate"] = PermissionOf(items, "create"),
            ["PermUpdate"] = PermissionOf(items, "update"),
            ["PermDelete"] = PermissionOf(items, "delete"),
            ["HasKeyword"] = context.Columns.Any(static c => IsQueryable(c) && c.QueryType is CodeGenQueryType.Like or CodeGenQueryType.LikeLeft or CodeGenQueryType.LikeRight && Base(c) == "string"),
            // 确认提示里写明操作对象的列：树表的名称列，否则第一列进列表的短文本
            ["TitleField"] = (context.TreeNameColumn ?? context.Columns.FirstOrDefault(static c => c.IsBusiness && c.IsList && Base(c) == "string" && c.Enum is null && c.Relation is null && c.DictSelectorType is null && c.Length is > 0 and <= 200)) is { } title ? CodeGenNaming.Camelize(title.CSharpProperty) : null,
            // 主表行展开时每张明细表取的条数
            ["DetailPageSize"] = DetailPageSize,
            ["EnabledActions"] = context.Actions.ToList(),
            ["CanCreate"] = context.Can(CodeGenActions.Create),
            ["CanUpdate"] = context.Can(CodeGenActions.Update),
            ["CanDelete"] = context.Can(CodeGenActions.Delete),
            ["CanExport"] = context.Can(CodeGenActions.Export),
            ["CanImport"] = context.Can(CodeGenActions.Import),
            ["CanStatus"] = context.StatusColumn is not null,
            ["CanPrint"] = context.Can(CodeGenActions.Print),
            ["IsTree"] = context.TreeParentColumn is not null,
            ["PrimaryKey"] = context.PrimaryKey is null ? null : byName[context.PrimaryKey],
            ["StatusColumn"] = context.StatusColumn is null ? null : byName[context.StatusColumn],
            // 生成器在实体里出的启停枚举名（整数状态列没选枚举时），没有为空
            ["StatusEnum"] = context.StatusColumn?.Enum is { IsGenerated: true } statusEnum ? statusEnum.ShortName : null,
            ["TreeParentColumn"] = context.TreeParentColumn is null ? null : byName[context.TreeParentColumn],
            ["TreeNameColumn"] = context.TreeNameColumn is null ? null : byName[context.TreeNameColumn],
            ["Columns"] = columns,
            ["BusinessColumns"] = context.Columns.Where(static c => c.IsBusiness).Select(c => byName[c]).ToList(),
            ["UniqueColumns"] = context.Columns.Where(static c => c.IsBusiness && c.IsUnique).Select(c => byName[c]).ToList(),
            ["CreateColumns"] = context.Columns.Where(static c => c.IsBusiness && c.IsInsert).Select(c => byName[c]).ToList(),
            ["UpdateColumns"] = context.Columns.Where(static c => c.IsBusiness && c.IsEdit).Select(c => byName[c]).ToList(),
            ["CreateUniqueColumns"] = context.Can(CodeGenActions.Create) ? context.Columns.Where(static c => c.IsBusiness && c.IsUnique && c.IsInsert).Select(c => byName[c]).ToList() : [],
            ["UpdateUniqueColumns"] = context.Can(CodeGenActions.Update) ? context.Columns.Where(static c => c.IsBusiness && c.IsUnique && c.IsEdit).Select(c => byName[c]).ToList() : [],
            ["QueryableColumns"] = context.Columns.Where(IsQueryable).Select(c => byName[c]).ToList(),
            ["SortableColumns"] = context.Columns.Where(static c => c.IsBusiness && Base(c) != "byte[]").Select(c => byName[c]).ToList(),
            // 明细表按外键筛选（主表页面取明细），外键列本身没开等值查询时另加一条
            ["MasterFilterColumn"] = context.MasterTable is { } master && context.Columns.FirstOrDefault(c => c.ColumnName == master.ForeignKeyColumn) is { } foreignKey && !(IsQueryable(foreignKey) && Operators(foreignKey).Contains("eq")) ? byName[foreignKey] : null,
            ["PrintFields"] = context.Columns.Where(static c => c.IsBusiness && Base(c) != "byte[]").Select(PrintField).ToList(),
            ["PrintSampleJson"] = PrintSample(context),
            ["RelationColumns"] = context.Columns.Where(static c => c.IsBusiness && c.Relation is not null).Select(c => byName[c]).ToList(),
            ["HasRelations"] = context.Columns.Any(static c => c.IsBusiness && c.Relation is not null),
            ["KeywordColumns"] = context.Columns.Where(static c => IsQueryable(c) && c.QueryType is CodeGenQueryType.Like or CodeGenQueryType.LikeLeft or CodeGenQueryType.LikeRight && Base(c) == "string").Select(c => byName[c]).ToList(),
            ["MasterTable"] = context.MasterTable is null ? null : Related(context.MasterTable),
            ["DetailTables"] = context.DetailTables.Select(Related).ToList(),
            ["HasDetailTables"] = context.DetailTables.Count > 0,
            ["ReferencedPackages"] = context.ReferencedPackages.ToList(),
            ["JsonTypes"] = items.JsonTypes.ToList(),
            ["PageIcon"] = CodeGenRegistration.PageIcon,
            ["PageOrder"] = CodeGenRegistration.PageOrder,
            ["Operations"] = items.Operations.Select(o => Object(new Dictionary<string, object?>
            {
                ["Id"] = o.Id,
                ["Verb"] = o.Verb,
                ["ClassName"] = o.ClassName,
                ["DescriptionConst"] = o.DescriptionConst,
                ["PermissionConst"] = o.PermissionConst,
                ["Key"] = o.Key,
                ["Column"] = o.Column is null ? null : byName[o.Column],
            })).ToList(),
            ["Permissions"] = items.Permissions.Select(p => Object(new Dictionary<string, object?>
            {
                ["Action"] = p.Action,
                ["ConstName"] = p.ConstName,
                ["Code"] = p.Code,
                ["Audited"] = p.Audited,
                ["Name"] = CodeGenPhrases.Text($"perm.{p.Action}.name", "en-US", ("name", context.DisplayName)),
                ["Description"] = CodeGenPhrases.Text($"perm.{p.Action}.description", "en-US", ("name", context.DisplayName)),
                // 文档注释用中文说明，句末不加「。」（说明文案本身是带句号的消息）
                ["Summary"] = CodeGenItems.PhraseOf(CodeGenPhrases.Text($"perm.{p.Action}.description", "zh-CN", ("name", context.DisplayName))),
            })).ToList(),
            ["Errors"] = items.Errors.Select(static e => Object(new Dictionary<string, object?>
            {
                ["ConstName"] = e.ConstName,
                ["Kind"] = e.Kind,
                ["Code"] = e.Code,
                ["Messages"] = e.Messages.Select(static m => Object(new Dictionary<string, object?> { ["ConstName"] = m.ConstName, ["Key"] = m.Key })).ToList(),
            })).ToList(),
        });
        RegisterFilters(model);
        return model;
    }

    private static ScriptObject Object(Dictionary<string, object?> values)
    {
        var result = new ScriptObject();
        foreach (var (key, value) in values)
        {
            result.SetValue(key, value, readOnly: true);
        }

        return result;
    }

    private static void RegisterFilters(ScriptObject model)
    {
        model.Import("cs_string", new Func<string?, string>(CodeGenTextEscaper.CSharpString));
        model.Import("xml_doc", new Func<string?, string>(CodeGenTextEscaper.XmlDoc));
        model.Import("ts_string", new Func<string?, string>(CodeGenTextEscaper.TsString));
        model.Import("html_attr", new Func<string?, string>(CodeGenTextEscaper.HtmlAttribute));
        model.Import("html_text", new Func<string?, string>(CodeGenTextEscaper.HtmlText));
        model.Import("block_comment", new Func<string?, string>(CodeGenTextEscaper.BlockComment));
        model.Import("html_comment", new Func<string?, string>(CodeGenTextEscaper.HtmlComment));
        model.Import("i18n_message", new Func<string?, string>(CodeGenTextEscaper.I18nMessage));
        model.Import("js_literal", new Func<string?, string>(CodeGenTextEscaper.JsLiteral));
        model.Import("select_options", new Func<string?, string?, string?, string>(CodeGenTextEscaper.SelectOptions));
        model.Import("kebab", new Func<string?, string>(static v => CodeGenNaming.Kebabize(v ?? string.Empty)));
        model.Import("snake", new Func<string?, string>(static v => CodeGenNaming.Snakeize(v ?? string.Empty)));
        model.Import("camel", new Func<string?, string>(static v => CodeGenNaming.Camelize(v ?? string.Empty)));
        // 按名字排序（不分大小写，同前端 lint 的导入排序）
        model.Import("sort_names", new Func<IEnumerable<object>?, List<string>>(static v => [.. (v ?? []).Select(static o => o?.ToString() ?? string.Empty).Order(StringComparer.OrdinalIgnoreCase)]));
    }

    private static string Base(CodeGenColumnModel column) => column.CSharpType.TrimEnd('?');

    private static string PermissionOf(CodeGenItems items, string action) => items.Permissions.FirstOrDefault(p => p.Action == action)?.Code ?? string.Empty;

    private static bool IsQueryable(CodeGenColumnModel column) => column.IsBusiness && column.IsQuery && Base(column) != "byte[]";

    private static bool IsDate(CodeGenColumnModel column)
        => column.HtmlType is CodeGenHtmlType.DatePicker or CodeGenHtmlType.DateTimePicker || Base(column) is "DateTimeOffset" or "DateTime" or "DateOnly";

    private static ScriptObject Related(CodeGenRelatedTable table) => Object(new Dictionary<string, object?>
    {
        ["I18nPrefix"] = $"{table.ModuleCode}.{table.Resource.Replace('-', '_')}",
        ["Columns"] = table.Columns.Select(c => Column(c)).ToList(),
        ["TableName"] = table.TableName,
        ["DisplayName"] = table.DisplayName,
        ["ClassName"] = table.ClassName,
        ["ClassNameCamel"] = CodeGenNaming.Camelize(table.ClassName),
        ["ClassNameKebab"] = CodeGenNaming.Kebabize(table.ClassName),
        ["Resource"] = table.Resource,
        ["ResourceCamel"] = CodeGenNaming.Camelize(CodeGenNaming.Pascalize(table.Resource)),
        ["ModuleCode"] = table.ModuleCode,
        ["ForeignKeyColumn"] = table.ForeignKeyColumn,
        ["ForeignKeyProperty"] = table.ForeignKeyProperty,
        ["ForeignKeyField"] = CodeGenNaming.Camelize(table.ForeignKeyProperty),
    });

    // 列 → 模板变量。判据收在这里，几份模板共用，不各写一套（照 4.x）
    private static ScriptObject Column(CodeGenColumnModel column, bool isTreeParent = false)
    {
        var type = Base(column);
        var isString = type == "string";
        var nullable = column.CSharpType.EndsWith('?');
        var enumFacts = column.Enum;
        var qualified = enumFacts is null ? type : $"{enumFacts.Namespace}.{enumFacts.ShortName}";
        var queryable = IsQueryable(column);
        var isDate = IsDate(column);
        var isLongText = isString && column.Length is not (> 0 and <= MaxStringLength);
        var values = new Dictionary<string, object?>
        {
            ["ColumnName"] = column.ColumnName,
            ["ColumnComment"] = column.ColumnComment,
            ["Label"] = CodeGenItems.LabelOf(column),
            ["DbType"] = column.DbType,
            ["CSharpType"] = column.CSharpType,
            ["CSharpTypeBase"] = type,
            // 枚举不在生成的命名空间里，写全名免得少 using
            ["CSharpTypeQualified"] = nullable ? qualified + "?" : qualified,
            ["CSharpTypeQualifiedBase"] = qualified,
            ["CSharpProperty"] = column.CSharpProperty,
            ["PropertySnake"] = CodeGenNaming.Snakeize(column.CSharpProperty),
            ["Field"] = CodeGenNaming.Camelize(column.CSharpProperty),
            ["TsType"] = column.TsType,
            ["IsString"] = isString,
            ["IsGuid"] = type == nameof(Guid),
            ["IsBool"] = type == "bool",
            ["IsBinary"] = type == "byte[]",
            ["IsDate"] = isDate,
            ["IsEnum"] = enumFacts is not null,
            ["IsNumeric"] = type is "int" or "long" or "short" or "byte" or "decimal" or "double" or "float",
            ["IsValueType"] = !isString && type != "byte[]",
            ["IsNullableType"] = nullable,
            ["IsLongText"] = isLongText,
            ["StringLength"] = isString && !isLongText ? column.Length : null,
            // 列名与属性名按约定换算一致时不写 [SugarColumn]
            ["NeedsColumnName"] = !string.Equals(CodeGenNaming.Snakeize(column.CSharpProperty), column.ColumnName, StringComparison.Ordinal),
            ["IsPrimaryKey"] = column.IsPrimaryKey,
            ["IsIdentity"] = column.IsIdentity,
            ["IsNullable"] = column.IsNullable,
            ["IsRequired"] = column.IsRequired,
            ["IsUnique"] = column.IsBusiness && column.IsUnique,
            ["IsBaseColumn"] = column.IsBaseColumn,
            ["IsBusiness"] = column.IsBusiness,
            ["InList"] = column.IsBusiness && column.IsList,
            ["InCreate"] = column.IsBusiness && column.IsInsert,
            ["InUpdate"] = column.IsBusiness && column.IsEdit,
            ["InForm"] = column.IsBusiness && (column.IsInsert || column.IsEdit),
            ["IsQueryable"] = queryable,
            // 后端筛选白名单的运算符：日期一律按区间（照 4.x），模糊查询是 contains，其余按查询方式
            ["FilterOperators"] = queryable ? Operators(column) : new List<string>(),
            // 筛选值的解析：生成 Parse<属性>(string text, out T value) 的方法体
            ["Parser"] = Parser(column, qualified),
            // 请求参数的校验特性（带尾随空格，没有为空）
            ["RequestAttributes"] = RequestAttributes(column, type, nullable, isLongText),
            // 请求值写进实体时的取值：文本去首尾空白，可空文本空白即空
            ["Assign"] = type == "string" ? (nullable ? $"string.IsNullOrWhiteSpace({column.CSharpProperty}) ? null : {column.CSharpProperty}.Trim()" : $"{column.CSharpProperty}.Trim()") : column.CSharpProperty,
            ["IsSortable"] = column.IsBusiness && type != "byte[]",
            ["Length"] = column.Length,
            ["DecimalDigits"] = column.DecimalDigits,
            ["HtmlType"] = column.HtmlType.ToString(),
            ["QueryType"] = column.QueryType.ToString(),
            ["DictSelectorType"] = column.DictSelectorType?.ToString(),
            ["DictCode"] = column.DictCode,
            ["EnumTypeName"] = column.EnumTypeName,
            ["EnumShortName"] = enumFacts?.ShortName,
            ["EnumNamespace"] = enumFacts?.Namespace,
            ["EnumDefaultMember"] = enumFacts?.DefaultMember,
            ["ConstValues"] = column.ConstValues,
            ["Relation"] = column.Relation is not { } relation || !column.IsBusiness ? null : Object(new Dictionary<string, object?>
            {
                ["ClassName"] = relation.ClassName,
                ["EntityTypeQualified"] = relation.EntityTypeQualified,
                ["TableName"] = relation.TableName,
                ["Label"] = relation.Label,
                ["LabelProperty"] = relation.LabelProperty,
                ["ParentProperty"] = relation.ParentProperty,
                ["IsTree"] = relation.IsTree,
                // 契约客户端上的选项方法（<资源>.<列>-options）
                ["OptionsMethod"] = CodeGenNaming.Camelize(column.CSharpProperty) + "Options",
            }),
        };

        // 前端模板用的判据（控件、表单取值、搜索方式）
        foreach (var (key, value) in CodeGenFrontendFacts.Of(column, queryable ? Operators(column) : [], isTreeParent))
        {
            values[key] = value;
        }

        return Object(values);
    }

    private static List<string> Operators(CodeGenColumnModel column)
    {
        var type = Base(column);
        var comparable = type is not ("string" or "bool" or nameof(Guid)) && column.Enum is null;
        if (IsDate(column) || (column.QueryType == CodeGenQueryType.Between && comparable))
        {
            return ["ge", "le"];
        }

        if (type == "string" && column.QueryType is CodeGenQueryType.Like or CodeGenQueryType.LikeLeft or CodeGenQueryType.LikeRight)
        {
            return ["contains"];
        }

        // 文本、布尔、Guid 与枚举没有大小比较，退回等值
        var single = column.QueryType switch
        {
            CodeGenQueryType.NotEqual => "ne",
            CodeGenQueryType.GreaterThan when comparable => "gt",
            CodeGenQueryType.GreaterThanOrEqual when comparable => "ge",
            CodeGenQueryType.LessThan when comparable => "lt",
            CodeGenQueryType.LessThanOrEqual when comparable => "le",
            CodeGenQueryType.In or CodeGenQueryType.NotIn when type != "bool" => "in",
            _ => "eq",
        };

        // 选项列另收多选（照 4.x 的搜索下拉）
        return single == "eq" && column.DictSelectorType is not null && type != "bool" ? ["eq", "in"] : [single];
    }

    private static string Parser(CodeGenColumnModel column, string qualified) => Base(column) switch
    {
        _ when column.Enum is not null => "Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value)",
        "string" => "(value = text) is not null",
        nameof(Guid) => "Guid.TryParse(text, out value)",
        "bool" => "bool.TryParse(text, out value)",
        "DateTimeOffset" => "DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value)",
        "DateTime" => "DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)",
        "DateOnly" => "DateOnly.TryParse(text, CultureInfo.InvariantCulture, out value)",
        "TimeSpan" => "TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out value)",
        _ => $"{qualified}.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)",
    };

    private static string RequestAttributes(CodeGenColumnModel column, string type, bool nullable, bool isLongText)
    {
        var attributes = new List<string>();

        // 文本只在勾了必填时要求非空（非空列留空发空串，照 4.x）；可空的值类型勾了必填即不能为空
        if (column.IsRequired && (type == "string" || nullable))
        {
            attributes.Add("Required");
        }

        if (type == "string" && !isLongText && column.Length is { } length)
        {
            attributes.Add($"StringLength({length})");
        }

        return attributes.Count == 0 ? string.Empty : $"[{string.Join(", ", attributes)}] ";
    }

    private static ScriptObject PrintField(CodeGenColumnModel column)
    {
        var type = Base(column);
        var input = column.HtmlType switch
        {
            CodeGenHtmlType.DatePicker => "date",
            CodeGenHtmlType.DateTimePicker => "datetime",
            CodeGenHtmlType.Textarea => "textarea",
            _ when type == "bool" => "boolean",
            _ when IsNumber(type) => "number",
            _ when IsDate(column) => "datetime",
            _ => "text",
        };
        return Object(new Dictionary<string, object?>
        {
            ["Key"] = CodeGenNaming.Camelize(column.CSharpProperty),
            ["Label"] = CodeGenItems.LabelOf(column),
            ["IsImage"] = column.HtmlType == CodeGenHtmlType.ImageUpload,
            ["InputType"] = input,
        });
    }

    private static bool IsNumber(string type) => type is "int" or "long" or "short" or "byte" or "decimal" or "double" or "float";

    // 打印数据源的样例（设计器预览与样例表单的初值）：选项、布尔与日期按显示文本给（打印时已换成文本），照 4.x
    private static string PrintSample(CodeGenContext context)
    {
        using var buffer = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(buffer, new System.Text.Json.JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            foreach (var column in context.Columns.Where(static c => c.IsBusiness && Base(c) != "byte[]"))
            {
                var key = CodeGenNaming.Camelize(column.CSharpProperty);
                var type = Base(column);
                if (IsNumber(type) && column.DictSelectorType is null)
                {
                    writer.WriteNumber(key, 1);
                    continue;
                }

                writer.WriteString(key, column.HtmlType switch
                {
                    CodeGenHtmlType.ImageUpload => "/favicon.png",
                    CodeGenHtmlType.DatePicker => "2026-01-01",
                    _ when IsDate(column) => "2026-01-01 08:00:00",
                    _ when type == "bool" => "是",
                    _ => "示例" + CodeGenItems.LabelOf(column),
                });
            }

            writer.WriteString("createdAt", "2026-01-01 08:00:00");
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }
}
