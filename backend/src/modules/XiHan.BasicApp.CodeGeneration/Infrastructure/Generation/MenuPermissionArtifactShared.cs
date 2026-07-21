#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuPermissionArtifactShared
// Guid:f91d4a15-75ac-4082-b677-ac927444fc90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/21 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.RegularExpressions;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

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

    /// <summary>二阶产物统一模板编码（用于产物溯源标识）</summary>
    public const string TemplateCode = "_menu_permission";

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
        ["import"] = new("导入", true, false)
    };

    /// <summary>
    /// 生效动作集：读取基线 read 恒在，追加已启用写操作（引擎已归一化 EnabledActions 为 create/update/delete 子集）
    /// </summary>
    public static IReadOnlyList<string> EffectiveActions(CodeGenerationContext context)
    {
        var actions = new List<string> { "read" };
        foreach (var action in context.EnabledActions)
        {
            if (!actions.Contains(action))
            {
                actions.Add(action);
            }
        }

        return actions;
    }

    /// <summary>
    /// 动作元数据查询（未知动作回退为非审计、非危险，标题取原值）
    /// </summary>
    public static ActionMeta MetaOf(string action)
        => ActionMetas.TryGetValue(action, out var meta) ? meta : new ActionMeta(action, false, false);

    /// <summary>
    /// 资源编码（权限码资源段）= 表名（snake，全局唯一）
    /// </summary>
    public static string Resource(CodeGenerationContext context) => context.TableName;

    /// <summary>
    /// 展示名（业务名优先，回退类名）
    /// </summary>
    public static string Display(CodeGenerationContext context)
        => string.IsNullOrWhiteSpace(context.BusinessName) ? context.ClassName : context.BusinessName!.Trim();

    /// <summary>
    /// 命名空间（表配置命名空间优先，回退模块段/类名）
    /// </summary>
    public static string ResolveNamespace(CodeGenerationContext context)
        => string.IsNullOrWhiteSpace(context.Namespace)
            ? ModuleSegment(context)
            : context.Namespace!.Trim();

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
