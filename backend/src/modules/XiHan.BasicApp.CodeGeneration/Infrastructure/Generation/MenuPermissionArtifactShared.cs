// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Text.RegularExpressions;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 菜单/权限二阶产物生成器共享工具（命名解析、动作集、大小写转换）
/// </summary>
/// <remarks>
/// 四个二阶生成器（权限码常量 / 权限定义 / PageRegistry 片段 / 种子骨架）共用这些推导，
/// 避免各自维护一份命名与动作规则导致不一致。
/// </remarks>
internal static class MenuPermissionArtifactShared
{
    /// <summary>二阶产物统一输出目录</summary>
    public const string OutputFolder = "_GeneratedMenuPermission";

    /// <summary>
    /// 权限码常量与权限定义的位置（相对后端模块项目根）
    /// </summary>
    public const string PermissionsFolder = "Domain/Permissions";

    /// <summary>
    /// 种子骨架的位置（相对后端模块项目根）
    /// </summary>
    public const string SeedersFolder = "Infrastructure/Seeders";

    /// <summary>二阶产物统一模板编码（用于产物溯源标识）</summary>
    public const string TemplateCode = "_menu_permission";

    /// <summary>C# 产物的标准版权文件头（缺了会被仓内分析器 XHFH001 报警）</summary>
    public static readonly IReadOnlyList<string> CSharpFileHeaderLines =
    [
        "// Copyright (c) 2021-Present XiHanFun and contributors.",
        "// Licensed under the MIT License. See LICENSE in the project root for license information."
    ];

    /// <summary>
    /// 写入 C# 产物文件头（逐行 AppendLine，换行与正文一致）
    /// </summary>
    /// <param name="sb">产物内容</param>
    public static void AppendCSharpFileHeader(StringBuilder sb)
    {
        foreach (var line in CSharpFileHeaderLines)
        {
            sb.AppendLine(line);
        }
    }

    /// <summary>
    /// 动作元数据（对齐平台操作字典 SysOperation：标题 / 是否审计 / 是否危险）
    /// </summary>
    private static readonly IReadOnlyDictionary<string, ActionMeta> ActionMetas = new Dictionary<string, ActionMeta>
    {
        ["read"] = new("查看", false, false),
        ["create"] = new("创建", true, false),
        ["update"] = new("更新", true, false),
        ["delete"] = new("删除", true, true),
        ["export"] = new("导出", false, false),
        ["import"] = new("导入", true, false),
        ["status"] = new("状态", true, false)
    };

    /// <summary>
    /// 生效动作集（权限码的操作段）：读取基线 read 恒在，追加已启用操作
    /// </summary>
    /// <remarks>
    /// 打印不是权限动作：打印按钮跟列表的读取权限走（取模板另需打印模板的使用权限），不派生独立权限码。
    /// </remarks>
    public static IReadOnlyList<string> EffectiveActions(CodeGenerationContext context)
    {
        var actions = new List<string> { "read" };
        foreach (var action in context.EnabledActions)
        {
            if (action != CodeGenActions.Print && !actions.Contains(action))
            {
                actions.Add(action);
            }
        }

        return actions;
    }

    /// <summary>
    /// 要登记的页面按钮
    /// </summary>
    /// <remarks>
    /// 写操作按钮随其动作启用；打印按钮随「打印」启用，挂读取权限；查询与详情走列表页读取权限，没有独立按钮。
    /// </remarks>
    public static IEnumerable<CodeGenButtonPermission> EnabledButtons(CodeGenerationContext context)
    {
        var effective = EffectiveActions(context);
        return ButtonPermissionMappings.Buttons.Where(button => button.Key == CodeGenActions.Print
            ? context.EnabledActions.Contains(CodeGenActions.Print)
            : button.Action != "read" && effective.Contains(button.Action));
    }

    /// <summary>
    /// 动作元数据查询（未知动作回退为非审计、非危险，标题取原值）
    /// </summary>
    public static ActionMeta MetaOf(string action)
        => ActionMetas.TryGetValue(action, out var meta) ? meta : new ActionMeta(action, false, false);

    /// <summary>
    /// 资源编码（权限码资源段）= 表名（snake，全局唯一）
    /// </summary>
    public static string Resource(CodeGenerationContext context) => context.TableName.ToLowerInvariant();

    /// <summary>
    /// 展示名（业务名优先，其次表注释，最后类名）
    /// </summary>
    /// <remarks>
    /// 菜单、权限名与页面上的文案（页面名、按钮、确认语、提示）共用这一个名字，避免菜单叫「示例便签」、页面却叫「示例便签表」。
    /// </remarks>
    public static string Display(CodeGenerationContext context)
        => !string.IsNullOrWhiteSpace(context.BusinessName) ? context.BusinessName!.Trim()
            : !string.IsNullOrWhiteSpace(context.TableComment) ? context.TableComment!.Trim()
            : context.ClassName;

    /// <summary>
    /// 命名空间（表配置命名空间优先，回退模块段/类名）
    /// </summary>
    public static string ResolveNamespace(CodeGenerationContext context)
        => string.IsNullOrWhiteSpace(context.Namespace)
            ? EnsureDistinctFromClassName(ModuleSegment(context), context.ClassName)
            : context.Namespace!.Trim();

    /// <summary>
    /// 命名空间根段与实体类名去重
    /// </summary>
    /// <remarks>
    /// 模块名与命名空间都为空时，模块段回退为类名；同名会让 CS0118（同一个名字既是命名空间又是类型）。
    /// </remarks>
    private static string EnsureDistinctFromClassName(string ns, string className)
        => string.Equals(ns, className, StringComparison.Ordinal) ? ns + "Generated" : ns;

    /// <summary>
    /// 模块段（原样，用于命名空间/组件路径；模块名为空时回退类名）
    /// </summary>
    public static string ModuleSegment(CodeGenerationContext context)
        => SafeSegment(context.ModuleName) ?? context.ClassName;

    /// <summary>
    /// 模块段（Pascal，用于组件路径/路由名前缀）
    /// </summary>
    public static string ModulePascal(CodeGenerationContext context) => Pascalize(ModuleSegment(context));

    /// <summary>
    /// 模块段（小写，用于页面码/路由路径）
    /// </summary>
    public static string ModuleLower(CodeGenerationContext context) => ModuleSegment(context).ToLowerInvariant();

    /// <summary>
    /// 类名 kebab（sys-product）
    /// </summary>
    public static string Kebab(CodeGenerationContext context) => Kebabize(context.ClassName);

    /// <summary>
    /// 页面码（{module}.{kebab}）
    /// </summary>
    /// <remarks>
    /// 前端 schema 的 pageCode、按钮码前缀、PageRegistry 片段的 PageDescriptor.Code 共用这一个推导，
    /// 任何一处另算都会让按钮码对不上、按钮静默不显示。
    /// </remarks>
    public static string PageCode(CodeGenerationContext context) => $"{ModuleLower(context)}.{Kebab(context)}";

    /// <summary>
    /// 前端组件路径（{module}/{kebab}/index，对齐生成的 Vue 页面落点 src/views/{module}/{kebab}/index.vue）
    /// </summary>
    public static string Component(CodeGenerationContext context) => $"{ModuleLower(context)}/{Kebab(context)}/index";

    /// <summary>
    /// 路由名（{Module}{Class}）
    /// </summary>
    public static string RouteName(CodeGenerationContext context) => $"{ModulePascal(context)}{context.ClassName}";

    /// <summary>
    /// 非空段（去空白，全空返回 null）
    /// </summary>
    public static string? SafeSegment(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// 首字母大写（read → Read）
    /// </summary>
    public static string Pascalize(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];

    /// <summary>
    /// PascalCase → kebab-case（SysProduct → sys-product）
    /// </summary>
    public static string Kebabize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = Regex.Replace(value, "([A-Z]+)([A-Z][a-z])", "$1-$2");
        result = Regex.Replace(result, "([a-z0-9])([A-Z])", "$1-$2");
        return result.Replace('_', '-').ToLowerInvariant();
    }

    /// <summary>
    /// 动作元数据
    /// </summary>
    /// <param name="Title">动作标题（查看/创建/…）</param>
    /// <param name="IsRequireAudit">是否需要审计</param>
    /// <param name="IsDangerous">是否危险操作</param>
    public sealed record ActionMeta(string Title, bool IsRequireAudit, bool IsDangerous);
}
