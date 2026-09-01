// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
public sealed partial class ScribanTemplateRenderer : ITemplateRenderer
{
    /// <summary>
    /// 渲染器对应的模板引擎
    /// </summary>
    public TemplateEngine Engine => TemplateEngine.Scriban;

    /// <summary>
    /// 渲染模板
    /// </summary>
    /// <param name="templateSource">模板源码</param>
    /// <param name="context">代码生成上下文（模板模型）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>渲染结果文本</returns>
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

        RegisterEscapeFilters(scriptObject);

        // 键前缀不合规不会被前端门禁抓到（其孤儿扫描正则对连字符与非 ASCII 是静默漏检），
        // 只会在运行期渲染裸键，故在生成期 fail-closed
        var i18nPrefix = BuildI18nPrefix(context);
        if (!I18nPrefixRegex().IsMatch(i18nPrefix))
        {
            throw new InvalidOperationException($"i18n 键前缀不合规：{i18nPrefix}（模块名与类名须可归一化为 [a-z][a-z0-9_]*）");
        }

        var scribanContext = new TemplateContext { MemberRenamer = member => member.Name };
        scribanContext.PushGlobal(scriptObject);
        return await template.RenderAsync(scribanContext);
    }

    /// <summary>
    /// 注册转义过滤器
    /// </summary>
    /// <remarks>
    /// 表注释、列注释是自由文本，直插产物会破坏宿主语法。模板据插值点所在上下文选用：
    /// <c>cs_string</c>（C# 字符串字面量）、<c>xml_doc</c>（XML 文档注释）、
    /// <c>ts_string</c>（TS 单引号字面量）、<c>html_attr</c>（HTML/Vue 双引号属性）。
    /// </remarks>
    private static void RegisterEscapeFilters(ScriptObject scriptObject)
    {
        scriptObject.Import("cs_string", new Func<string?, string>(TemplateTextEscaper.CSharpString));
        scriptObject.Import("xml_doc", new Func<string?, string>(TemplateTextEscaper.XmlDoc));
        scriptObject.Import("ts_string", new Func<string?, string>(TemplateTextEscaper.TsString));
        scriptObject.Import("html_attr", new Func<string?, string>(TemplateTextEscaper.HtmlAttribute));
        scriptObject.Import("html_text", new Func<string?, string>(TemplateTextEscaper.HtmlText));
        scriptObject.Import("block_comment", new Func<string?, string>(TemplateTextEscaper.BlockComment));
        scriptObject.Import("html_comment", new Func<string?, string>(TemplateTextEscaper.HtmlComment));
        scriptObject.Import("i18n_message", new Func<string?, string>(TemplateTextEscaper.I18nMessage));
        scriptObject.Import("js_literal", new Func<string?, string>(TemplateTextEscaper.JsLiteral));
        scriptObject.Import("select_options", new Func<string?, string?, string?, string>(TemplateTextEscaper.SelectOptions));
    }

    /// <summary>
    /// 构建 i18n 键前缀（模块段 + 类名段）
    /// </summary>
    private static string BuildI18nPrefix(CodeGenerationContext context)
    {
        var moduleSegment = NamingConventions.I18nSegment(MenuPermissionArtifactShared.ModuleSegment(context));
        return $"{moduleSegment}.{NamingConventions.Snakeize(context.ClassName)}";
    }

    /// <summary>
    /// i18n 键前缀合规判据（与前端门禁的孤儿扫描正则同源）
    /// </summary>
    [GeneratedRegex(@"^[a-z]\w*(?:\.\w+)+$")]
    private static partial Regex I18nPrefixRegex();

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
            // 命名空间为空时回退到模块段：DbFirst 导入的表未配置命名空间，直插会渲染出 using .Domain.Entities;
            ["Namespace"] = MenuPermissionArtifactShared.ResolveNamespace(context),
            // 模块名为空时回退到类名：页面码、落盘路径、菜单组件路径都由它推导，
            // 裸值为 null 会产出 pageCode '.sys-product' 与 src/views//sys-product
            ["ModuleName"] = MenuPermissionArtifactShared.ModuleSegment(context),
            // i18n 键段：模块名是自由输入，直插会同时破坏 TS 对象字面量与前端门禁正则；
            // 类名段用 snake 与手写页对齐
            ["I18nNamespace"] = NamingConventions.I18nSegment(MenuPermissionArtifactShared.ModuleSegment(context)),
            ["ClassNameSnake"] = NamingConventions.Snakeize(context.ClassName),
            ["I18nPrefix"] = BuildI18nPrefix(context),
            // 页面码：与 PageRegistry 片段同一处推导，模板不再各自拼接
            ["PageCode"] = MenuPermissionArtifactShared.PageCode(context),
            // en-US 侧唯一素材：列注释只有中文，标识符才是英文
            ["ClassNameEn"] = NamingConventions.HumanizeIdentifier(context.ClassName),
            ["BusinessName"] = context.BusinessName,
            ["FunctionName"] = context.FunctionName,
            ["Author"] = context.Author,
            // 枚举以名称字符串透出，便于模板按名比较（如 {{ if TemplateType == "Tree" }}）
            ["TemplateType"] = context.TemplateType.ToString(),
            // 包含操作：透出列表（供 array.contains）+ 三个便捷布尔（模板首选，避免重复判定）
            ["EnabledActions"] = context.EnabledActions.ToList(),
            ["CanCreate"] = context.EnabledActions.Contains("create"),
            ["CanUpdate"] = context.EnabledActions.Contains("update"),
            ["CanDelete"] = context.EnabledActions.Contains("delete"),
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
            ["ClassNameSnake"] = NamingConventions.Snakeize(table.ClassName),
            ["ClassNameEn"] = NamingConventions.HumanizeIdentifier(table.ClassName),
            // 与主表同口径回退：DbFirst 导入的关联表 ModuleName 恒为 null，
            // 裸值会让前端产物渲出 '@/api/modules//sys-xxx' 这种解析不到的双斜杠路径
            ["ModuleName"] = MenuPermissionArtifactShared.SafeSegment(table.ModuleName) ?? table.ClassName,
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
        // 业务列 = 非基类托管、非主键；基类列与主键由基类承载，不进任何产物的属性列表
        var isBusinessColumn = !GeneratedColumnNames.IsBaseColumn(column.ColumnName) && !column.IsPrimaryKey;

        // 查询归类：二进制列不参与查询；日期区间走 conditions.filters，其余等值走 DTO 顶层字段。
        // 八个模板共用同一判据，避免各写一份长条件导致取数侧与展现侧漂移。
        var isDateColumn = column.HtmlType is HtmlType.DatePicker or HtmlType.DateTimePicker;
        var isQueryable = isBusinessColumn
            && column.IsQuery
            && !CSharpTypeFacts.IsBinary(column.CSharpType);
        // 日期列一律按区间下发：搜索区渲的是日期控件、给出的是时间戳，
        // 走等值那条路只会被前端的字符串归一化丢掉，等于搜索框恒不生效
        var isRangeQuery = isQueryable && isDateColumn;
        var isScalarQuery = isQueryable && !isRangeQuery
            && column.QueryType is QueryType.Equal or QueryType.Between;
        var isKeywordQuery = isQueryable && column.QueryType == QueryType.Like;

        // long 在报文里是字符串（全局 LongJsonConverter），前端一律按字符串承载。
        // 存量列配置可能还存着 ts_type='number'，这里统一归一化，免得模板各自判两把尺子。
        var isLongColumn = column.CSharpType.TrimEnd('?') == "long";
        var tsType = isLongColumn ? "string" : column.TsType;
        var controlKind = ResolveControlKind(column, tsType);

        return new Dictionary<string, object?>
        {
            ["IsDateColumn"] = isDateColumn,
            ["IsQueryable"] = isQueryable,
            ["IsScalarQuery"] = isScalarQuery,
            ["IsRangeQuery"] = isRangeQuery,
            ["IsKeywordQuery"] = isKeywordQuery,
            // 列开关：列配置里的列表/新增/编辑四个开关，折进业务列判定后供模板直接使用
            ["InList"] = isBusinessColumn && column.IsList,
            ["InCreate"] = isBusinessColumn && column.IsInsert,
            ["InUpdate"] = isBusinessColumn && column.IsEdit,
            ["InForm"] = isBusinessColumn && (column.IsInsert || column.IsEdit),
            // 详情与实体承载全部业务列：详情要能看到全部字段，实体要能映射全部列
            ["InDetail"] = isBusinessColumn,
            ["ColumnName"] = column.ColumnName,
            ["ColumnComment"] = column.ColumnComment,
            ["DbType"] = column.DbType,
            ["CSharpType"] = column.CSharpType,
            // 限定类型名：枚举类型不在生成目标命名空间内，直插短名编译不过。
            // 产物带 auto-generated 头，全限定名不触发命名简化分析器，也免掉 using 排序问题。
            ["CSharpTypeQualified"] = column.EnumNamespace is null
                ? column.CSharpType
                : $"{column.EnumNamespace}.{column.CSharpType}",
            // 类型语义：模板据此选可空判据（值类型解包取 .Value）与跳过二进制列。
            // 枚举短名不在类型名白名单里，但它是值类型，须显式并入
            ["IsValueType"] = CSharpTypeFacts.IsValueType(column.CSharpType) || column.EnumTypeShortName is not null,
            ["IsBinary"] = CSharpTypeFacts.IsBinary(column.CSharpType),
            ["CSharpProperty"] = column.CSharpProperty,
            // 前端属性名（camelCase，对应后端 camelCase JSON 序列化）
            ["TsProperty"] = Camelize(column.CSharpProperty),
            // 文案键段与推导英文标签（键段从属性名派生：中文列注释会塌缩成同一个键）
            ["I18nKey"] = NamingConventions.I18nSegment(column.CSharpProperty),
            ["EnLabel"] = NamingConventions.HumanizeIdentifier(column.CSharpProperty),
            ["TsType"] = tsType,
            // 表单控件的唯一判据。模板不要再按 HtmlType/TsType 各自级联——
            // 标志段、渲染段、回填、提交、表单模型、默认值分散在六份模板里，
            // 任何一处次序不同都会渲出「控件是下拉、模型是时间戳」这类自相矛盾的代码。
            ["ControlKind"] = controlKind,
            // 表单模型里该列的 TS 类型（开关恒 boolean、日期按时间戳承载，其余同 TsType）
            ["FormTsType"] = controlKind switch
            {
                "switch" => "boolean",
                "date" => "number",
                _ => tsType
            },
            ["IsLongColumn"] = isLongColumn,
            ["IsPrimaryKey"] = column.IsPrimaryKey,
            ["IsIdentity"] = column.IsIdentity,
            ["IsNullable"] = column.IsNullable,
            ["IsRequired"] = column.IsRequired,
            // 基类托管列（主键/审计/软删/租户）：模板生成业务属性时应跳过
            ["IsBaseColumn"] = GeneratedColumnNames.IsBaseColumn(column.ColumnName),
            ["Length"] = column.Length,
            ["DecimalDigits"] = column.DecimalDigits,
            ["HtmlType"] = column.HtmlType.ToString(),
            ["QueryType"] = column.QueryType.ToString(),
            // 字典三分（表单选项来源；关联不入生成代码，仅供模板渲染下拉控件）
            ["DictSelectorType"] = column.DictSelectorType?.ToString(),
            ["DictCode"] = column.DictCode,
            ["EnumTypeName"] = column.EnumTypeName,
            // 枚举事实：解析成功时非空，模板据此判定「选项来源是否接通」
            ["EnumTypeShortName"] = column.EnumTypeShortName,
            ["EnumNamespace"] = column.EnumNamespace,
            ["EnumDefaultMember"] = column.EnumDefaultMember,
            ["ConstValues"] = column.ConstValues
        };
    }

    /// <summary>
    /// 解析该列在表单里用哪种控件
    /// </summary>
    /// <remarks>
    /// 判定次序即「类型优先于配置」：当列类型与控件配置打架时，能与表单模型类型自洽的那一方胜出。
    /// 布尔只能是开关、日期只能是日期控件——给它们挂下拉会渲出绑不上的 v-model；
    /// 而数字与文本都能被下拉承载，故下拉排在这两者之前，用户的选择器配置得以保留。
    /// </remarks>
    /// <param name="column">列结构</param>
    /// <param name="tsType">归一化后的 TS 类型</param>
    /// <returns>控件种类：binary/switch/date/datetime/time/select/number/textarea/text</returns>
    private static string ResolveControlKind(ColumnSchema column, string tsType)
    {
        if (CSharpTypeFacts.IsBinary(column.CSharpType))
        {
            return "binary";
        }

        if (tsType == "boolean")
        {
            return "switch";
        }

        if (column.HtmlType == HtmlType.DatePicker)
        {
            return "date";
        }

        if (column.HtmlType == HtmlType.DateTimePicker)
        {
            return "datetime";
        }

        if (column.HtmlType == HtmlType.TimePicker)
        {
            return "time";
        }

        if (column.DictSelectorType is not null)
        {
            return "select";
        }

        if (tsType == "number")
        {
            return "number";
        }

        return column.HtmlType == HtmlType.Textarea ? "textarea" : "text";
    }

    /// <summary>
    /// PascalCase → camelCase（转换实现见 <see cref="NamingConventions"/>，与引擎共用）
    /// </summary>
    private static string Camelize(string value) => NamingConventions.Camelize(value);

    /// <summary>
    /// PascalCase → kebab-case（转换实现见 <see cref="NamingConventions"/>，与引擎共用）
    /// </summary>
    private static string Kebabize(string value) => NamingConventions.Kebabize(value);

    /// <summary>
    /// 校验模板语法
    /// </summary>
    /// <param name="templateSource">模板源码</param>
    /// <returns>校验结果</returns>
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
