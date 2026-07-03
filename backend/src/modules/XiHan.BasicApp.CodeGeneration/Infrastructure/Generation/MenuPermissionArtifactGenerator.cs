#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuPermissionArtifactGenerator
// Guid:c0de9e00-0403-4a00-9000-000000000403
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text;
using System.Text.RegularExpressions;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 菜单/权限二阶产物生成器（生成"待并入源码"的权限码常量 + 落地 README）
/// </summary>
/// <remarks>
/// 形态：生成代码片段并入源码 → 重建库经既有 Seeder 链生效，符合 XiHan 单一事实源约定，
/// 不做运行时写库（避免与重建库覆盖冲突）。权限码沿用 {资源}:{操作} 两段式，资源段取表名（snake）。
/// </remarks>
public static class MenuPermissionArtifactGenerator
{
    private const string OutputFolder = "_GeneratedMenuPermission";

    /// <summary>
    /// 构建菜单/权限二阶产物（权限码常量 + 落地 README）
    /// </summary>
    public static IReadOnlyList<GeneratedArtifact> Build(CodeGenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return
        [
            BuildPermissionCodes(context),
            BuildReadme(context)
        ];
    }

    /// <summary>
    /// 权限码常量类（drop-in：放入 {模块}/Domain/Permissions/）
    /// </summary>
    private static GeneratedArtifact BuildPermissionCodes(CodeGenerationContext context)
    {
        var resource = context.TableName;
        var ns = ResolveNamespace(context);
        var className = $"{context.ClassName}PermissionCodes";

        var sb = new StringBuilder();
        sb.AppendLine($"namespace {ns}.Domain.Permissions;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {context.ClassName} 权限码常量（资源 {resource} × 标准操作）");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>资源编码</summary>");
        sb.AppendLine($"    public const string Resource = \"{resource}\";");
        foreach (var action in ButtonPermissionMappings.Actions)
        {
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>{action} 权限</summary>");
            sb.AppendLine($"    public const string {Pascalize(action)} = \"{resource}:{action}\";");
        }

        sb.AppendLine("}");

        var fileName = $"{className}.cs";
        return new GeneratedArtifact($"{OutputFolder}/{fileName}", fileName, sb.ToString(), "_menu_permission");
    }

    /// <summary>
    /// 落地 README（权限码/按钮映射/菜单规格 + 并入源码 → 重建库步骤）
    /// </summary>
    private static GeneratedArtifact BuildReadme(CodeGenerationContext context)
    {
        var resource = context.TableName;
        var ns = ResolveNamespace(context);
        var menuCode = resource;
        var kebab = Kebabize(context.ClassName);
        var component = $"{Pascalize(SafeSegment(context.ModuleName) ?? context.ClassName)}/{context.ClassName}/Index";
        var routeName = $"{Pascalize(SafeSegment(context.ModuleName) ?? string.Empty)}{context.ClassName}";
        var display = string.IsNullOrWhiteSpace(context.BusinessName) ? context.ClassName : context.BusinessName;

        var sb = new StringBuilder();
        sb.AppendLine($"# {display} 菜单与按钮权限 · 落地说明");
        sb.AppendLine();
        sb.AppendLine("> 本目录产物为「待并入源码」的代码片段，不是运行时写库。并入后**重建数据库**，经既有 Seeder 链生效（符合 XiHan 单一事实源 + 菜单即绑约定）。");
        sb.AppendLine();
        sb.AppendLine("## 1. 权限码");
        sb.AppendLine();
        sb.AppendLine($"资源 `{resource}` × 标准操作，两段式 `{{资源}}:{{操作}}`（同 `code_gen:read`）：");
        sb.AppendLine();
        sb.AppendLine("| 权限码 | 说明 |");
        sb.AppendLine("| --- | --- |");
        foreach (var action in ButtonPermissionMappings.Actions)
        {
            sb.AppendLine($"| `{resource}:{action}` | {action} |");
        }

        sb.AppendLine();
        sb.AppendLine($"常量类见同目录 `{context.ClassName}PermissionCodes.cs`，放入 `{ns}/Domain/Permissions/`，并在生成的 AppService 各方法上加 `[PermissionAuthorize({context.ClassName}PermissionCodes.Xxx)]`。");
        sb.AppendLine();
        sb.AppendLine("## 2. 标准按钮 → 权限码");
        sb.AppendLine();
        sb.AppendLine("| 按钮 | 键 | 权限码 |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var button in ButtonPermissionMappings.Buttons)
        {
            sb.AppendLine($"| {button.Title} | `{button.Key}` | `{resource}:{button.Action}` |");
        }

        sb.AppendLine();
        sb.AppendLine("## 3. 菜单条目规格（SysMenu）");
        sb.AppendLine();
        sb.AppendLine("| 字段 | 值 |");
        sb.AppendLine("| --- | --- |");
        sb.AppendLine($"| MenuCode | `{menuCode}` |");
        sb.AppendLine("| MenuType | `Menu` |");
        sb.AppendLine($"| Path | `/{kebab}` |");
        sb.AppendLine($"| Component | `{component}` |");
        sb.AppendLine($"| RouteName | `{routeName}` |");
        sb.AppendLine($"| Title | `{display}` |");
        sb.AppendLine($"| I18nKey | `menu.{resource}` |");
        sb.AppendLine($"| PermissionId | 绑定 `{resource}:read`（菜单可见性，菜单即绑） |");
        sb.AppendLine($"| ParentId | {(context.Options.TryGetValue("ParentMenuId", out var pid) && pid is not null ? $"表配置 ParentMenuId=`{pid}`" : "未设置 → 顶级菜单（如需挂父菜单，设置表配置 ParentMenuId）")} |");
        sb.AppendLine();
        sb.AppendLine("## 4. 落地步骤");
        sb.AppendLine();
        sb.AppendLine($"1. 将 `{context.ClassName}PermissionCodes.cs` 放入 `{ns}/Domain/Permissions/`。");
        sb.AppendLine("2. 在生成的 AppService/QueryService 方法上应用 `[PermissionAuthorize(...)]`（按第 2 节映射）。");
        sb.AppendLine("3. 为本模块编写「资源 + 权限 + 菜单 + 超管授权」种子，**镜像**代码生成模块的既有样板：");
        sb.AppendLine("   - `XiHan.BasicApp.CodeGeneration/Infrastructure/Seeders/System/SysResourceSeeder.cs`（登记资源 `" + resource + "`）");
        sb.AppendLine("   - `.../SysPermissionSeeder.cs`（资源 × 操作 → `" + resource + ":*` 权限）");
        sb.AppendLine("   - `.../SysMenuSeeder.cs`（建菜单 + 按钮，绑定 `" + resource + ":read` 可见性）");
        sb.AppendLine("   - `.../SysRolePermissionSeeder.cs`（授超管）");
        sb.AppendLine("   - Order 用 **200+** 段，避免与 CodeGeneration(100 段)/Saas 交叠；顺序仍为 资源→权限→菜单→授权。");
        sb.AppendLine("4. 在模块的 `ServiceCollectionExtensions` 里 `AddDataSeeder<>` 注册上述种子。");
        sb.AppendLine("5. **重建数据库**，菜单与按钮权限即到位。");
        sb.AppendLine();
        sb.AppendLine("> 说明：权限码唯一性以资源段（表名）保证；若跨模块出现同表名，请调整资源段避免冲突。菜单树选择器（ParentMenuId 可视化）为后续增强。");

        return new GeneratedArtifact($"{OutputFolder}/README.md", "README.md", sb.ToString(), "_menu_permission");
    }

    /// <summary>
    /// 解析命名空间（表配置命名空间优先，回退模块名/类名）
    /// </summary>
    private static string ResolveNamespace(CodeGenerationContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Namespace))
        {
            return context.Namespace.Trim();
        }

        return SafeSegment(context.ModuleName) ?? context.ClassName;
    }

    private static string? SafeSegment(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// 首字母大写（read → Read）
    /// </summary>
    private static string Pascalize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// PascalCase → kebab-case（SysProduct → sys-product）
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
}
