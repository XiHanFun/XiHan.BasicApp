// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Scriban.Runtime;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 前端模板用的列判据
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>ScribanTemplateRenderer</c>：控件种类、表单取值五件套、数字框小数位、文本框字数上限、字段数据类型与导入取值收在这一处，几份前端模板共用，不各写一套
/// 与 4.x 不同（6.0 的报文）：long 是数字（4.x 按字符串承载）；日期是 <c>DateTimeOffset</c>，回填按 ISO 解析、提交 <c>toISOString()</c>（4.x 发本地时间文本）；
/// 枚举是成员名字面量；树表的上级为空即根节点（可空列发 null，非空列发空 Guid）
/// </remarks>
internal static class CodeGenFrontendFacts
{
    /// <summary>
    /// 空 Guid：非空的上级列表示根节点
    /// </summary>
    public const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    // 数字框小数位的上限（与 XNumberInput 的 precision 校验同口径，超出时组件直接报错）
    private const int MaxNumberPrecision = 20;

    // 文本框字数上限的有效范围：不限长类型由库元数据报成 2^31-1 一类的哨兵值，不当真
    private const int MaxInputLengthHint = 65_535;

    /// <summary>
    /// 列的前端判据
    /// </summary>
    /// <param name="column">列模型</param>
    /// <param name="operators">后端筛选白名单给这一列的运算符</param>
    /// <param name="isTreeParent">是否树表的上级列</param>
    public static Dictionary<string, object?> Of(CodeGenColumnModel column, IReadOnlyList<string> operators, bool isTreeParent)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(operators);
        var type = column.CSharpType.TrimEnd('?');
        var nullable = column.CSharpType.EndsWith('?');
        var tsType = TsTypeOf(column, type);
        var options = OptionsKindOf(column, type);
        var facts = new Dictionary<string, object?>
        {
            ["OptionsKind"] = options,
            ["EnumMembers"] = column.Enum?.Members.Select(static m => new ScriptObject { ["Name"] = m, ["Snake"] = CodeGenNaming.Snakeize(m) }).ToList() ?? [],
            ["SearchKind"] = SearchKindOf(column, operators, options),
            ["IsImportable"] = column.IsBusiness && column.IsInsert && type != "byte[]",
        };

        if (isTreeParent)
        {
            // 树表的上级：同表的树形候选，留空即根节点；非空列的根节点是空 Guid
            facts["ControlKind"] = "treeparent";
            facts["FormTsType"] = "string";
            facts["IsFormRequired"] = false;
            facts["FormRequiredKind"] = "select";
            facts["FormEmptyCheck"] = null;
            facts["FormDefault"] = "null";
            facts["FormFromSource"] = nullable ? "$s ?? null" : $"$s && $s !== '{EmptyGuid}' ? $s : null";
            facts["FormToWire"] = nullable ? "$v ?? null" : $"$v ?? '{EmptyGuid}'";
            facts["ImportFromRecord"] = nullable ? "($r as $t | undefined) || null" : $"($r as $t | undefined) || '{EmptyGuid}'";
            facts["NumberPrecision"] = null;
            facts["InputMaxLength"] = null;
            facts["FieldDataType"] = "string";
            facts["SearchKind"] = string.Empty;
            return facts;
        }

        var control = ControlKindOf(column, type, tsType);
        var form = FormOf(column, control, tsType, type, nullable);
        facts["ControlKind"] = control;
        // 表单模型里该列的 TS 类型（开关恒 boolean、日期与日期时间按时间戳承载、枚举取契约的字面量，其余同 TsType）
        facts["FormTsType"] = control switch
        {
            "switch" => "boolean",
            "date" or "datetime" => "number",
            _ when column.Enum is not null => "enum",
            _ => tsType,
        };
        facts["IsFormRequired"] = form.IsRequired;
        facts["FormRequiredKind"] = control switch
        {
            "date" or "datetime" or "select" or "treeselect" => "select",
            "image" or "file" => "upload",
            _ => "input",
        };
        facts["FormEmptyCheck"] = form.EmptyCheck;
        facts["FormDefault"] = form.Default;
        facts["FormFromSource"] = form.FromSource;
        facts["FormToWire"] = form.ToWire;
        facts["ImportFromRecord"] = form.ImportFromRecord;
        facts["NumberPrecision"] = NumberPrecisionOf(column, control, type);
        facts["InputMaxLength"] = control is "text" or "textarea" && type == "string" && column.Length is > 0 and <= MaxInputLengthHint ? column.Length : null;
        facts["FieldDataType"] = control switch
        {
            "datetime" => "datetime",
            "date" => "date",
            "image" => "image",
            "file" => "file",
            "select" or "treeselect" => "enum",
            _ when tsType == "number" => "number",
            _ when tsType == "boolean" => "boolean",
            _ => "string",
        };
        return facts;
    }

    // 契约里的 TS 类型：数字（含 long）是 number，布尔是 boolean，其余（文本、Guid、日期、时间、枚举）是 string
    private static string TsTypeOf(CodeGenColumnModel column, string type) => type switch
    {
        _ when column.Enum is not null => "string",
        "bool" => "boolean",
        "int" or "long" or "short" or "byte" or "decimal" or "double" or "float" => "number",
        _ => "string",
    };

    private static string OptionsKindOf(CodeGenColumnModel column, string type) => column switch
    {
        { Relation: { IsTree: true } } => "tree",
        { Relation: not null } => "relation",
        { Enum: not null } => "enum",
        { DictSelectorType: CodeGenDictSelectorType.DictSelector } => "dict",
        { DictSelectorType: CodeGenDictSelectorType.ConstSelector } => "const",
        _ when type == "bool" => "bool",
        _ => string.Empty,
    };

    // 搜索区的取值方式：日期区间、等值与单边比较各自一个搜索框，模糊查询并进关键字框，有选项的多选；数字区间与 ne / gt / lt 搜索区不出（后端白名单照样认）
    private static string SearchKindOf(CodeGenColumnModel column, IReadOnlyList<string> operators, string options)
    {
        if (operators.Count == 2)
        {
            return column.HtmlType is CodeGenHtmlType.DatePicker or CodeGenHtmlType.DateTimePicker || column.CSharpType.TrimEnd('?') == "DateTimeOffset" ? "range" : string.Empty;
        }

        return operators.Count == 1 ? operators[0] switch
        {
            "contains" => "keyword",
            "eq" or "ge" or "le" => operators[0],
            "in" when options.Length > 0 => "multiple",
            _ => string.Empty,
        } : string.Empty;
    }

    /// <summary>
    /// 列在表单里用哪种控件
    /// </summary>
    /// <remarks>
    /// 取值 binary / switch / date / datetime / time / image / file / treeselect / select / number / textarea / text
    /// 照 4.x，判定次序即「类型优先于配置」：布尔只能是开关、日期只能是日期控件；数字与文本都能被下拉承载，故下拉排在这两者之前
    /// </remarks>
    private static string ControlKindOf(CodeGenColumnModel column, string type, string tsType)
    {
        if (type == "byte[]")
        {
            return "binary";
        }

        if (tsType == "boolean")
        {
            return "switch";
        }

        switch (column.HtmlType)
        {
            case CodeGenHtmlType.DatePicker:
                return "date";
            case CodeGenHtmlType.DateTimePicker:
                return "datetime";
            case CodeGenHtmlType.TimePicker:
                return "time";
            case CodeGenHtmlType.ImageUpload or CodeGenHtmlType.FileUpload when tsType == "string":
                // 上传控件存文件库的文件 ID
                return column.HtmlType == CodeGenHtmlType.ImageUpload ? "image" : "file";
        }

        if (column.DictSelectorType == CodeGenDictSelectorType.TreeSelector)
        {
            return "treeselect";
        }

        // 生成器出的启停枚举没有选项来源，照样是下拉
        if (column.DictSelectorType is not null || column.Enum is not null)
        {
            return "select";
        }

        if (tsType == "number")
        {
            return "number";
        }

        return column.HtmlType == CodeGenHtmlType.Textarea ? "textarea" : "text";
    }

    /// <summary>
    /// 表单取值口径
    /// </summary>
    /// <remarks>
    /// 照 4.x：报文的可空性跟请求类型走（即 C# 类型可不可空），「必填」只管表单校验，二者不混用
    /// 非空列留空时报文要有一个能落库的值：文本给空串、数字给 0、开关恒有值；下拉、日期、时间、二进制与 Guid 没有说得通的缺省值，非空时一律按必填校验
    /// 可空的文本类控件清空后是空串，按 null 发
    /// </remarks>
    private static FormFacts FormOf(CodeGenColumnModel column, string control, string tsType, string type, bool nullable)
    {
        var hasFallback = control is "switch" or "number" || (control is "text" or "textarea" or "image" or "file" && type == "string");
        var isRequired = control != "switch" && (column.IsRequired || (!nullable && !hasFallback));
        var enumDefault = column.Enum is null ? null : $"'{column.Enum.DefaultMember}'";
        var zero = control switch
        {
            "switch" => "false",
            "number" => "0",
            _ when enumDefault is not null => enumDefault,
            _ when tsType == "number" => "0",
            _ => "''",
        };

        var defaultValue = control switch
        {
            // 日期不预填：非空时交给必填校验强制选一次
            "date" or "datetime" => "null",
            "switch" => "false",
            _ when nullable => "null",
            _ when enumDefault is not null => enumDefault,
            // 下拉没有说得通的缺省项，留空让必填校验逼着选
            "select" or "treeselect" => "null",
            _ => zero,
        };

        // 含调用的三元式拆三行（前端 lint 的换行规则）：这三段只用在 schema 里 4 格缩进的对象属性上，续行按 6 格缩进
        var fromSource = control switch
        {
            "date" or "datetime" => "$s\n      ? new Date($s).getTime()\n      : null",
            "switch" => "$s ?? false",
            _ => "$s ?? null",
        };

        var toWire = control switch
        {
            "date" or "datetime" => nullable ? "$v == null\n      ? null\n      : new Date($v).toISOString()" : "new Date($v as number).toISOString()",
            "switch" => "$v",
            _ when nullable => control is "number" or "select" or "treeselect" || column.Enum is not null ? "$v ?? null" : "$v || null",
            _ => $"$v ?? {zero}",
        };

        string? emptyCheck = !isRequired ? null : control switch
        {
            "date" or "datetime" or "number" => "$v == null",
            "select" or "treeselect" => tsType == "number" ? "$v == null" : "!$v",
            _ => "!$v?.trim()",
        };

        return new FormFacts(isRequired, emptyCheck, defaultValue, fromSource, toWire, ImportOf(control, isRequired, nullable, zero));
    }

    // 导入记录 → 新建请求：值已按字段类型换算好；没填的列与新建表单留空同口径（可空发 null、非空发缺省值），日期按本地时间换成时刻
    private static string ImportOf(string control, bool isRequired, bool nullable, string zero)
    {
        if (control is "date" or "datetime")
        {
            // 日期列没有缺省值，非空时一定按必填校验，不必填就一定可空
            return isRequired ? "toImportInstant($r)" : "$r == null\n      ? null\n      : toImportInstant($r)";
        }

        if (isRequired)
        {
            return "$r as $t";
        }

        return nullable ? "($r as $t | undefined) ?? null" : $"($r as $t | undefined) ?? {zero}";
    }

    // 数字框的小数位：整数列 0 位（否则 1.5 会让整单 400）；decimal 按列定义的小数位；其余不限
    private static int? NumberPrecisionOf(CodeGenColumnModel column, string control, string type)
    {
        if (control != "number")
        {
            return null;
        }

        if (type is "int" or "long" or "short" or "byte")
        {
            return 0;
        }

        return type == "decimal" && column.DecimalDigits is > 0 and <= MaxNumberPrecision ? column.DecimalDigits : null;
    }

    private sealed record FormFacts(bool IsRequired, string? EmptyCheck, string Default, string FromSource, string ToWire, string ImportFromRecord);
}
