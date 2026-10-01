// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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
            // 界面与提示文案里的业务对象名，与菜单名同一口径（业务名优先，其次表注释，最后类名）
            ["DisplayName"] = MenuPermissionArtifactShared.Display(context),
            ["ClassName"] = context.ClassName,
            // 前端文件名/标识用：类名的 camelCase 与 kebab-case
            ["ClassNameCamel"] = Camelize(context.ClassName),
            ["ClassNameKebab"] = Kebabize(context.ClassName),
            // 命名空间为空时回退到模块段：DbFirst 导入的表未配置命名空间，直插会渲染出 using .Domain.Entities;
            ["Namespace"] = MenuPermissionArtifactShared.ResolveNamespace(context),
            // 实体所在命名空间：沿用已有实体时取它的命名空间，否则是生成实体的位置
            ["EntityNamespace"] = context.ExistingEntityNamespace ?? $"{MenuPermissionArtifactShared.ResolveNamespace(context)}.Domain.Entities",
            ["HasExistingEntity"] = context.ExistingEntityNamespace is not null,
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
            // 包含操作：透出列表（供 array.contains）+ 逐项便捷布尔（模板首选，避免重复判定）。
            // 导入必带新增由引擎 fail-closed 保证，模板里 CanImport 为真时新增接口一定在
            ["EnabledActions"] = context.EnabledActions.ToList(),
            ["CanCreate"] = context.EnabledActions.Contains(CodeGenActions.Create),
            ["CanUpdate"] = context.EnabledActions.Contains(CodeGenActions.Update),
            ["CanDelete"] = context.EnabledActions.Contains(CodeGenActions.Delete),
            ["CanExport"] = context.EnabledActions.Contains(CodeGenActions.Export),
            ["CanImport"] = context.EnabledActions.Contains(CodeGenActions.Import),
            // 状态切换要有状态列（引擎 fail-closed 解析，勾了就一定有）；打印跟读取权限走
            ["CanStatus"] = context.EnabledActions.Contains(CodeGenActions.Status) && context.StatusColumn is not null,
            ["CanPrint"] = context.EnabledActions.Contains(CodeGenActions.Print),
            ["StatusColumn"] = context.StatusColumn is null ? null : BuildColumn(context.StatusColumn),
            // 打印数据源的样例数据（设计器预览与样例表单初值）
            ["PrintSampleJson"] = BuildPrintSampleJson(context),
            ["PrimaryKey"] = context.PrimaryKey is null ? null : BuildColumn(context.PrimaryKey),
            ["Columns"] = context.Columns.Select(BuildColumn).ToList(),
            // 树表结构列（TemplateType == "Tree" 时非空，由引擎 fail-closed 保证）
            ["TreeParentColumn"] = context.TreeParentColumn is null ? null : BuildColumn(context.TreeParentColumn),
            ["TreeNameColumn"] = context.TreeNameColumn is null ? null : BuildColumn(context.TreeNameColumn),
            // 主子表关联（本表为子表时 MasterTable 非空；本表为主表时 DetailTables 非空）
            ["MasterTable"] = context.MasterTable is null ? null : BuildRelatedTable(context.MasterTable),
            ["DetailTables"] = context.DetailTables.Select(BuildRelatedTable).ToList(),
            ["HasDetailTables"] = context.DetailTables.Count > 0,
            // 关联选择器列：后端出选项接口，前端出下拉/树形下拉；模板据此决定是否引入关联选项 DTO
            ["HasRelations"] = context.Columns.Any(column => column.Relation is not null && IsBusinessColumn(column)),
            ["RelationColumns"] = context.Columns.Where(column => column.Relation is not null && IsBusinessColumn(column)).Select(BuildColumn).ToList(),
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
        var isBusinessColumn = IsBusinessColumn(column);

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
        var form = ResolveFormFacts(column, controlKind, tsType, isLongColumn);

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
            // 界面文案（列标题、表单标签、校验提示）：DbFirst 导入的表常没有列注释，
            // 直接用注释会渲出没有名字的字段，缺注释时按属性名推导
            ["Label"] = string.IsNullOrWhiteSpace(column.ColumnComment)
                ? NamingConventions.HumanizeIdentifier(column.CSharpProperty)
                : column.ColumnComment.Trim(),
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
            // 表单模型里该列的 TS 类型（开关恒 boolean、日期与日期时间按时间戳承载，其余同 TsType）
            ["FormTsType"] = controlKind switch
            {
                "switch" => "boolean",
                "date" or "datetime" => "number",
                _ => tsType
            },
            // 表单取值的五件套，与 ControlKind 同理收在这里，两份页面模板与两份 schema 模板共用。
            // 表达式里的 $s / $v 是占位符，模板按所在位置替换成 src.xxx / form.value.xxx
            ["IsFormRequired"] = form.IsRequired,
            ["FormRequiredVerb"] = form.RequiredVerb,
            ["FormEmptyCheck"] = form.EmptyCheck,
            ["FormDefault"] = form.Default,
            ["FormFromSource"] = form.FromSource,
            ["FormToWire"] = form.ToWire,
            // 数字框的小数位（XNumberInput precision）：整数列 0 位，否则 1.5 会让整单 400；
            // decimal 列按列定义的小数位；其余不限
            ["NumberPrecision"] = ResolveNumberPrecision(column, controlKind),
            // 文本框的字数上限（XInput max-length）：DTO 不校验长度，超长要到落库才报错
            ["InputMaxLength"] = ResolveInputMaxLength(column, controlKind, tsType, isLongColumn),
            // 列表字段的 dataType：表格渲染、搜索控件、导入换算都按它走，三份 schema 字段段共用这一个判据
            ["FieldDataType"] = ResolveFieldDataType(column, controlKind, tsType),
            // 导入：CSV 逐行调新增接口，列集合与新增表单一致；二进制列没法在表格里填，不进导入
            ["IsImportable"] = isBusinessColumn && column.IsInsert && controlKind != "binary",
            // 导入记录 → 新增入参的取值表达式（$r 为记录里的同名值，$t 为新增入参里该字段的类型）
            ["ImportFromRecord"] = form.ImportFromRecord,
            ["IsLongColumn"] = isLongColumn,
            ["IsPrimaryKey"] = column.IsPrimaryKey,
            ["IsIdentity"] = column.IsIdentity,
            ["IsNullable"] = column.IsNullable,
            ["IsRequired"] = column.IsRequired,
            // 唯一：实体出租户内唯一索引，新增与更新时查重（二进制与布尔列由引擎拦下）
            ["IsUnique"] = isBusinessColumn && column.IsUnique,
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
            ["ConstValues"] = column.ConstValues,
            // 关联目标（关联表 / 关联树选择器列非空）：选项接口按本列属性名命名，
            // 后端 Get{类名}{属性}OptionsAsync、前端 api.{属性 camel}Options()
            ["Relation"] = column.Relation is null || !isBusinessColumn ? null : new Dictionary<string, object?>
            {
                ["ClassName"] = column.Relation.ClassName,
                ["EntityTypeQualified"] = column.Relation.EntityTypeQualified,
                ["TableName"] = column.Relation.TableName,
                ["TableComment"] = column.Relation.TableComment,
                ["Label"] = string.IsNullOrWhiteSpace(column.Relation.TableComment) ? column.Relation.ClassName : column.Relation.TableComment.Trim(),
                ["LabelProperty"] = column.Relation.LabelProperty,
                ["ParentProperty"] = column.Relation.ParentProperty,
                ["IsTree"] = column.Relation.IsTree,
                ["OptionsMethod"] = $"{column.CSharpProperty}Options",
                ["OptionsMethodCamel"] = $"{Camelize(column.CSharpProperty)}Options"
            }
        };
    }

    /// <summary>
    /// 打印样例数据的写出选项（中文原样输出，样例在设计器里可读）
    /// </summary>
    /// <remarks>
    /// 用 <see cref="Utf8JsonWriter"/> 逐项写出而不走 <see cref="JsonSerializer"/>：
    /// 宿主关掉反射序列化（裁剪/AOT、文件式程序）时反射序列化直接抛错，生成器不该依赖宿主的这项开关。
    /// </remarks>
    private static readonly JsonWriterOptions PrintSampleJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 打印数据源的样例数据
    /// </summary>
    /// <remarks>
    /// 打印时选项列、日期、布尔已换成显示文本，样例按显示文本给：选项列给「示例xx」、布尔给「是」、
    /// 日期给本地格式；图片给站内图标，数字给 1。键与详情 DTO 的前端属性名一致。
    /// </remarks>
    private static string BuildPrintSampleJson(CodeGenerationContext context)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, PrintSampleJsonOptions))
        {
            writer.WriteStartObject();
            foreach (var column in context.Columns.Where(column => IsBusinessColumn(column) && !CSharpTypeFacts.IsBinary(column.CSharpType)))
            {
                var facts = BuildColumn(column);
                var key = (string)facts["TsProperty"]!;
                switch ((string)facts["FieldDataType"]!)
                {
                    case "number":
                        writer.WriteNumber(key, 1);
                        break;
                    case "boolean":
                        writer.WriteString(key, "是");
                        break;
                    case "date":
                        writer.WriteString(key, "2026-01-01");
                        break;
                    case "datetime":
                        writer.WriteString(key, "2026-01-01 08:00:00");
                        break;
                    case "image":
                        writer.WriteString(key, "/favicon.png");
                        break;
                    default:
                        writer.WriteString(key, "示例" + (string)facts["Label"]!);
                        break;
                }
            }

            // 创建时间是基类列，不在业务列里，单独补上（与数据源字段表末尾那项对应）
            writer.WriteString("createdTime", "2026-01-01 08:00:00");
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// 业务列 = 非基类托管、非主键（基类列与主键由基类承载，不进任何产物的属性列表）
    /// </summary>
    private static bool IsBusinessColumn(ColumnSchema column)
        => !GeneratedColumnNames.IsBaseColumn(column.ColumnName) && !column.IsPrimaryKey;

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
    /// <returns>控件种类：binary/switch/date/datetime/time/image/file/treeselect/select/number/textarea/text</returns>
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

        // 上传控件存文件中心的文件主键，列得装得下字符串（文本列或 long 标识）；
        // 二进制列存的是文件内容本身，已在最前面判成 binary
        if (tsType == "string" && column.HtmlType is HtmlType.ImageUpload or HtmlType.FileUpload)
        {
            return column.HtmlType == HtmlType.ImageUpload ? "image" : "file";
        }

        // 关联树：外键指向树表，按树形下拉选节点（列已由引擎校验为 long）
        if (column.DictSelectorType == DictSelectorType.TreeSelector)
        {
            return "treeselect";
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
    /// 解析该列在表单里的取值口径
    /// </summary>
    /// <remarks>
    /// 报文的可空性跟 C# DTO 走（即列本身可不可空），「必填」只管表单校验，二者不能混用：
    /// 非空列若只因没勾必填就在报文里发 null，值类型会整单 400、非空字符串会在落库时撞 NOT NULL。
    /// 所以非空列留空时报文要有一个能落库的值——文本给空串、数字给 0、开关恒有值；
    /// 下拉、日期、时间、二进制与 long 标识没有说得通的缺省值，非空时一律按必填校验。
    /// 可空的文本类控件清空后是空串，按 null 发：long 与时间的空串后端解析不了。
    /// 日期与日期时间都用日期选择器、按时间戳承载，提交时分别换成本地日期与本地日期时间文本。
    /// </remarks>
    private static FormFacts ResolveFormFacts(ColumnSchema column, string controlKind, string tsType, bool isLongColumn)
    {
        // 上传控件没传文件时是空串，与文本同理：非空文本列留空发空串，long 标识列没有说得通的缺省值
        var hasFallback = controlKind is "switch" or "number"
            || (controlKind is "text" or "textarea" or "image" or "file" && !isLongColumn);
        var isRequired = controlKind != "switch" && (column.IsRequired || (!column.IsNullable && !hasFallback));

        var enumDefault = column.EnumDefaultMember is null ? null : $"'{column.EnumDefaultMember}'";
        var zero = controlKind switch
        {
            "switch" => "false",
            "number" => "0",
            _ when enumDefault is not null => enumDefault,
            _ when tsType == "number" => "0",
            _ => "''"
        };

        var defaultValue = controlKind switch
        {
            // 日期与日期时间不预填：非空时交给必填校验强制选一次
            "date" or "datetime" => "null",
            "switch" => "false",
            _ when column.IsNullable => "null",
            _ when enumDefault is not null => enumDefault,
            // 下拉没有说得通的缺省项，留空让必填校验逼用户选
            "select" or "treeselect" => "null",
            _ => zero
        };

        var fromSource = controlKind switch
        {
            // 后端按 yyyy-MM-dd HH:mm:ss 下发，空格分隔在部分浏览器里解析不出，换成 T 按本地时间解析
            "date" or "datetime" => "$s ? new Date(String($s).replace(' ', 'T')).getTime() : null",
            "switch" => "$s ?? false",
            _ => "$s ?? null"
        };

        var toWire = controlKind switch
        {
            "date" => column.IsNullable ? "$v == null ? null : toDateOnly($v)" : "toDateOnly($v)",
            "datetime" => column.IsNullable ? "$v == null ? null : toDateTime($v)" : "toDateTime($v)",
            "switch" => "$v",
            _ when column.IsNullable => controlKind is "number" or "select" or "treeselect" ? "$v ?? null" : "$v || null",
            _ => $"$v ?? {zero}"
        };

        string? emptyCheck = !isRequired ? null : controlKind switch
        {
            "date" or "datetime" => "$v == null || Number.isNaN($v)",
            "number" => "$v == null",
            "select" or "treeselect" => tsType == "number" ? "$v == null" : "!$v",
            _ => "!$v?.trim()"
        };

        var verb = controlKind switch
        {
            "date" or "datetime" or "select" or "treeselect" => "请选择",
            "image" or "file" => "请上传",
            _ => "请输入"
        };
        return new FormFacts(isRequired, verb, emptyCheck, defaultValue, fromSource, toWire, ResolveImportFromRecord(column, controlKind, isRequired, zero));
    }

    /// <summary>
    /// 解析导入记录到新增入参的取值表达式
    /// </summary>
    /// <remarks>
    /// 导入按字段类型把单元格换算好后交给页面（数字、布尔、下拉的值已是报文形态），只有两件事要在这里补：
    /// 一是没填的列——必填列没填过不了导入校验，其余列与新增表单留空同口径（可空发 null、非空发缺省值）；
    /// 二是日期——单元格是原文，表格软件另存的 CSV 常写成 2026/9/30，要归一成后端认的本地时间文本。
    /// </remarks>
    private static string ResolveImportFromRecord(ColumnSchema column, string controlKind, bool isRequired, string zero)
    {
        if (controlKind is "date" or "datetime")
        {
            var convert = $"toImportDateText($r, {(controlKind == "datetime" ? "true" : "false")})";
            // 日期列没有缺省值，非空时一定按必填校验，不必填就一定可空
            return isRequired ? convert : $"$r == null ? null : {convert}";
        }

        if (isRequired)
        {
            return "$r as $t";
        }

        return column.IsNullable ? "($r as $t | undefined) ?? null" : $"($r as $t | undefined) ?? {zero}";
    }

    /// <summary>
    /// 数字框小数位的上限（与 XNumberInput 的 precision 校验同口径，超出时组件直接报错）
    /// </summary>
    private const int MaxNumberPrecision = 20;

    /// <summary>
    /// 解析数字框的小数位数
    /// </summary>
    /// <returns>整数列 0；decimal 列取列定义的小数位（超过组件上限时不限）；其余返回 null 表示不限</returns>
    private static int? ResolveNumberPrecision(ColumnSchema column, string controlKind)
    {
        if (controlKind != "number")
        {
            return null;
        }

        if (CSharpTypeFacts.IsInteger(column.CSharpType))
        {
            return 0;
        }

        return CSharpTypeFacts.IsDecimal(column.CSharpType) && column.DecimalDigits is > 0 and <= MaxNumberPrecision
            ? column.DecimalDigits
            : null;
    }

    /// <summary>
    /// 文本框字数上限的有效范围：text / clob 等不限长类型由库元数据报成 2^31-1 一类的哨兵值，不当真
    /// </summary>
    private const int MaxInputLengthHint = 65535;

    /// <summary>
    /// 解析文本框的字数上限
    /// </summary>
    /// <returns>字符串列的定义长度；long 标识、不限长或未定义长度时返回 null</returns>
    private static int? ResolveInputMaxLength(ColumnSchema column, string controlKind, string tsType, bool isLongColumn)
    {
        if (controlKind is not ("text" or "textarea") || tsType != "string" || isLongColumn)
        {
            return null;
        }

        return column.Length is > 0 and <= MaxInputLengthHint ? column.Length : null;
    }

    /// <summary>
    /// 解析列表字段的 dataType
    /// </summary>
    /// <remarks>
    /// 已接通选项来源的下拉（枚举元数据、系统字典、常量候选项）按 enum：表格显示选项文本、搜索渲下拉、导入按文本反查值；
    /// 未解析出的枚举仍按原类型承载。上传列按 image / file：表格出缩略图或打开入口。
    /// </remarks>
    private static string ResolveFieldDataType(ColumnSchema column, string controlKind, string tsType)
    {
        return controlKind switch
        {
            "datetime" => "datetime",
            "date" => "date",
            "image" => "image",
            "file" => "file",
            "select" when column.EnumTypeShortName is not null
                || column.DictSelectorType is DictSelectorType.ConstSelector or DictSelectorType.DictSelector or DictSelectorType.TableSelector => "enum",
            "treeselect" => "enum",
            _ when tsType == "number" => "number",
            _ when tsType == "boolean" => "boolean",
            _ => "string"
        };
    }

    /// <summary>
    /// 列在表单里的取值口径（表达式中的 $s / $v / $r / $t 为占位符）
    /// </summary>
    /// <param name="IsRequired">表单是否按必填校验</param>
    /// <param name="RequiredVerb">必填提示的动词（请输入 / 请选择）</param>
    /// <param name="EmptyCheck">判空表达式（$v 为表单值）；不必填时为 null</param>
    /// <param name="Default">新增时的默认值字面量</param>
    /// <param name="FromSource">编辑回填表达式（$s 为详情值）</param>
    /// <param name="ToWire">提交取值表达式（$v 为表单值）</param>
    /// <param name="ImportFromRecord">导入取值表达式（$r 为导入记录值，$t 为新增入参字段类型）</param>
    private sealed record FormFacts(bool IsRequired, string RequiredVerb, string? EmptyCheck, string Default, string FromSource, string ToWire, string ImportFromRecord);

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
