// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 生成的一个操作
/// </summary>
/// <param name="Id">操作 ID</param>
/// <param name="Verb">动作（<c>list</c>、<c>get</c>、<c>create</c>、<c>update</c>、<c>delete</c>、<c>set-status</c>、<c>tree</c>、<c>options</c>）</param>
/// <param name="ClassName">操作类名</param>
/// <param name="DescriptionConst">说明的消息常量名</param>
/// <param name="PermissionConst">权限常量名</param>
/// <param name="Column">选项操作对应的列（其余为空）</param>
public sealed record CodeGenOperationItem(string Id, string Verb, string ClassName, string DescriptionConst, string PermissionConst, CodeGenColumnModel? Column = null)
{
    /// <summary>
    /// 文案键的首段：操作 ID 里的连字符换成下划线（如 <c>sales_orders.list</c>）
    /// </summary>
    public string Key => Id.Replace('-', '_');
}

/// <summary>
/// 生成的一个权限码
/// </summary>
/// <param name="Action">动作（<c>read</c>、<c>create</c>、<c>update</c>、<c>delete</c>）</param>
/// <param name="ConstName">常量名</param>
/// <param name="Code">权限码</param>
public sealed record CodeGenPermissionItem(string Action, string ConstName, string Code)
{
    /// <summary>
    /// 写操作记审计
    /// </summary>
    public bool Audited => Action != "read";

    /// <summary>
    /// 文案键（连字符换成下划线）
    /// </summary>
    public string Key => Code.Replace('-', '_');
}

/// <summary>
/// 生成的一个错误码与它的消息
/// </summary>
/// <param name="ConstName">错误常量名</param>
/// <param name="Kind">种类（<c>NOTFOUND</c>、<c>CONFLICT</c>、<c>VALIDATION</c>）</param>
/// <param name="Code">错误码；加入已有包时序号待填</param>
/// <param name="Messages">消息：常量名、键、用语键与生成时参数</param>
public sealed record CodeGenErrorItem(string ConstName, string Kind, string Code, IReadOnlyList<CodeGenMessageItem> Messages);

/// <summary>
/// 生成的一条消息
/// </summary>
/// <param name="ConstName">消息常量名</param>
/// <param name="Key">消息键</param>
/// <param name="Phrase">用语键（见 <see cref="CodeGenPhrases"/>）</param>
/// <param name="Values">生成时代入的参数</param>
public sealed record CodeGenMessageItem(string ConstName, string Key, string Phrase, IReadOnlyList<(string Name, string Value)> Values);

/// <summary>
/// 一张表要登记的操作、权限码、错误码与消息
/// </summary>
/// <remarks>
/// 模板（新建包的包级文件）、引擎拼的文案与「加入已有包」的登记片段都用这一份，三处不会对不上
/// </remarks>
/// <param name="Operations">操作</param>
/// <param name="Permissions">权限码</param>
/// <param name="Errors">错误码</param>
/// <param name="JsonTypes">要进包的 <c>JsonSerializerContext</c> 的类型（C# 写法）</param>
public sealed record CodeGenItems(IReadOnlyList<CodeGenOperationItem> Operations, IReadOnlyList<CodeGenPermissionItem> Permissions, IReadOnlyList<CodeGenErrorItem> Errors, IReadOnlyList<string> JsonTypes)
{
    /// <summary>
    /// 按上下文列出
    /// </summary>
    /// <param name="context">生成上下文</param>
    public static CodeGenItems Of(CodeGenContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var res = context.ResourcePascal;
        var cls = context.Table.ClassName;
        var canCreate = context.Can(CodeGenActions.Create);
        var canUpdate = context.Can(CodeGenActions.Update);
        var canDelete = context.Can(CodeGenActions.Delete);
        var canStatus = context.StatusColumn is not null;
        var isTree = context.TreeParentColumn is not null;

        var permissions = new List<CodeGenPermissionItem> { Permission(context, "read") };
        if (canCreate)
        {
            permissions.Add(Permission(context, "create"));
        }

        if (canUpdate || canStatus)
        {
            permissions.Add(Permission(context, "update"));
        }

        if (canDelete)
        {
            permissions.Add(Permission(context, "delete"));
        }

        var operations = new List<CodeGenOperationItem>
        {
            Operation(context, "list", $"List{res}", res + "Read"),
            Operation(context, "get", $"Get{cls}", res + "Read"),
        };
        if (canCreate)
        {
            operations.Add(Operation(context, "create", $"Create{cls}", res + "Create"));
        }

        if (canUpdate)
        {
            operations.Add(Operation(context, "update", $"Update{cls}", res + "Update"));
        }

        if (canDelete)
        {
            operations.Add(Operation(context, "delete", $"Delete{res}", res + "Delete"));
        }

        if (canStatus)
        {
            operations.Add(Operation(context, "set-status", $"Set{cls}Status", res + "Update"));
        }

        if (isTree)
        {
            operations.Add(Operation(context, "tree", $"Get{cls}Tree", res + "Read"));
        }

        foreach (var column in context.Columns.Where(static c => c.IsBusiness && c.Relation is not null))
        {
            var verb = CodeGenNaming.Kebabize(column.CSharpProperty) + "-options";
            operations.Add(new CodeGenOperationItem($"{context.Resource}.{verb}", "options", $"List{cls}{column.CSharpProperty}Options", $"List{cls}{column.CSharpProperty}OptionsDescription", res + "Read", column));
        }

        var prefix = context.ModuleName.ToUpperInvariant();
        var numbers = new Dictionary<string, int>(StringComparer.Ordinal);
        string Code(string kind)
        {
            if (context.Table.Target == CodeGenTarget.ExistingPackage)
            {
                return $"{prefix}_{kind}_NNN";
            }

            numbers[kind] = numbers.GetValueOrDefault(kind) + 1;
            return $"{prefix}_{kind}_{numbers[kind]:000}";
        }

        var messagePrefix = $"{context.ModuleCode}.{CodeGenNaming.Snakeize(cls)}";
        var name = ("name", context.DisplayName);
        var errors = new List<CodeGenErrorItem>
        {
            new($"{cls}NotFound", "NOTFOUND", Code("NOTFOUND"), [new($"{cls}NotFound", messagePrefix + ".not_found", "msg.not_found", [name])]),
            new($"{cls}QueryUnsupported", "VALIDATION", Code("VALIDATION"),
            [
                new($"{cls}FilterUnsupported", messagePrefix + ".filter_unsupported", "msg.filter_unsupported", []),
                new($"{cls}SortUnsupported", messagePrefix + ".sort_unsupported", "msg.sort_unsupported", []),
            ]),
        };
        if (canCreate || canUpdate)
        {
            foreach (var column in context.Columns.Where(static c => c.IsBusiness && c.IsUnique))
            {
                var label = ("label", LabelOf(column));
                errors.Add(new($"{cls}{column.CSharpProperty}Taken", "CONFLICT", Code("CONFLICT"), [new($"{cls}{column.CSharpProperty}Taken", $"{messagePrefix}.{CodeGenNaming.Snakeize(column.CSharpProperty)}_taken", "msg.taken", [label])]));
            }
        }

        if (isTree && canDelete)
        {
            errors.Add(new($"{cls}HasChildren", "CONFLICT", Code("CONFLICT"), [new($"{cls}HasChildren", messagePrefix + ".has_children", "msg.has_children", [name])]));
        }

        if (isTree && (canCreate || canUpdate))
        {
            errors.Add(new($"{cls}InvalidParent", "VALIDATION", Code("VALIDATION"), [new($"{cls}InvalidParent", messagePrefix + ".invalid_parent", "msg.invalid_parent", [])]));
        }

        // 与 backend.contracts 模板定义的类型一一对应
        var json = new List<string> { $"Get{cls}Request", $"{cls}Response", $"PageResult<{cls}Response>", "PageRequest" };
        if (canCreate)
        {
            json.Add($"Create{cls}Request");
        }

        if (canUpdate)
        {
            json.Add($"Update{cls}Request");
        }

        if (canDelete)
        {
            json.AddRange([$"{cls}IdsRequest", "BatchResult"]);
        }

        if (canStatus)
        {
            json.Add($"Set{cls}StatusRequest");
        }

        if (isTree)
        {
            json.AddRange([$"{cls}TreeRequest", $"{cls}Node", $"{cls}TreeResponse"]);
        }

        if (operations.Any(static o => o.Verb == "options"))
        {
            json.AddRange([$"{cls}OptionsRequest", $"{cls}Option", $"{cls}OptionsResponse"]);
        }

        return new CodeGenItems(operations, permissions, errors, json);
    }

    /// <summary>
    /// 列在界面上的名字
    /// </summary>
    /// <remarks>
    /// 列注释，没有即属性名（照 4.x）；注释按 <see cref="PhraseOf"/> 去掉句末标点
    /// </remarks>
    /// <param name="column">列</param>
    public static string LabelOf(CodeGenColumnModel column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return PhraseOf(column.ColumnComment) ?? column.CSharpProperty;
    }

    /// <summary>
    /// 注释当名字用的写法
    /// </summary>
    /// <remarks>
    /// 去掉首尾空白与句末的标点：模板把它拼进句子（「所在字典。」「…的网络契约。」）或当字段名，
    /// 6.0 实体的表与列注释来自文档注释、句末带「。」，原样拼会成「所在字典。。」。空白即 <see langword="null"/>
    /// </remarks>
    /// <param name="text">表或列的注释</param>
    public static string? PhraseOf(string? text)
        => CodeGenDataSourceService.Trimmed(CodeGenDataSourceService.Trimmed(text)?.TrimEnd('。', '．', '.', '；', ';', '！', '!', '，', ',', '、'));

    private static CodeGenPermissionItem Permission(CodeGenContext context, string action)
        => new(action, context.ResourcePascal + char.ToUpperInvariant(action[0]) + action[1..], $"{context.ModuleCode}.{context.Resource}.{action}");

    private static CodeGenOperationItem Operation(CodeGenContext context, string verb, string className, string permission)
        => new($"{context.Resource}.{verb}", verb, className, className + "Description", permission);
}
