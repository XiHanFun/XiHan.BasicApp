#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuPermissionArtifactGenerator
// Guid:f91d4a15-75ac-4082-b677-ac927444fc8a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using Shared = XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.MenuPermissionArtifactShared;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 菜单/权限二阶产物生成器（权限码常量类 + 落地 README）
/// </summary>
/// <remarks>
/// 形态：生成"待并入源码"的代码片段 → 重建库经既有 Seeder 链生效，符合 XiHan 单一事实源约定，
/// 不做运行时写库（避免与重建库覆盖冲突）。权限码沿用 {资源}:{操作} 两段式，资源段取表名（snake）。
/// 权限码集合随表配置 EnabledActions 裁剪：只生成实际会产出接口/按钮的动作（读取基线 read 恒在）。
/// </remarks>
public static class MenuPermissionArtifactGenerator
{
    /// <summary>
    /// 构建权限码常量 + 落地 README
    /// </summary>
    /// <param name="context">生成上下文</param>
    /// <param name="collidingCodes">已存在于系统的权限码（生成前查库得到，用于 README 顶部醒目告警；为空表示无冲突）</param>
    public static IReadOnlyList<GeneratedArtifact> Build(CodeGenerationContext context, IReadOnlyCollection<string> collidingCodes)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(collidingCodes);

        return
        [
            BuildPermissionCodes(context),
            BuildReadme(context, collidingCodes)
        ];
    }

    /// <summary>
    /// 权限码常量类（drop-in：放入 {模块}/Domain/Permissions/）
    /// </summary>
    private static GeneratedArtifact BuildPermissionCodes(CodeGenerationContext context)
    {
        var resource = Shared.Resource(context);
        var ns = Shared.ResolveNamespace(context);
        var className = $"{context.ClassName}PermissionCodes";

        var sb = new StringBuilder();
        sb.AppendLine($"namespace {ns}.Domain.Permissions;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {context.ClassName} 权限码常量（资源 {resource} × 已启用操作）");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>资源编码</summary>");
        sb.AppendLine($"    public const string Resource = \"{resource}\";");
        foreach (var action in Shared.EffectiveActions(context))
        {
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>{Shared.MetaOf(action).Title}权限</summary>");
            sb.AppendLine($"    public const string {Shared.Pascalize(action)} = \"{resource}:{action}\";");
        }

        sb.AppendLine("}");

        var fileName = $"{className}.cs";
        return new GeneratedArtifact($"{Shared.OutputFolder}/{fileName}", fileName, sb.ToString(), Shared.TemplateCode);
    }

    /// <summary>
    /// 落地 README（产物清单 + 3 步落地；权限码冲突时顶部醒目告警）
    /// </summary>
    private static GeneratedArtifact BuildReadme(CodeGenerationContext context, IReadOnlyCollection<string> collidingCodes)
    {
        var resource = Shared.Resource(context);
        var ns = Shared.ResolveNamespace(context);
        var display = Shared.Display(context);
        var kebab = Shared.Kebab(context);
        var actions = Shared.EffectiveActions(context);

        var sb = new StringBuilder();
        sb.AppendLine($"# {display} 菜单与权限 · 落地说明");
        sb.AppendLine();

        if (collidingCodes.Count > 0)
        {
            sb.AppendLine("> ⚠️ **权限码冲突**：以下权限码在系统中已存在，直接并入会与既有资源撞码。");
            sb.AppendLine("> 请先确认表名（资源段）是否与既有模块重名，必要时调整表名或资源段后重新生成：");
            sb.AppendLine(">");
            foreach (var code in collidingCodes)
            {
                sb.AppendLine($"> - `{code}`");
            }

            sb.AppendLine();
        }

        sb.AppendLine("> 本目录产物为「待并入源码」的代码片段，不是运行时写库。并入后**重建数据库**，经既有 Seeder 链生效（符合 XiHan 单一事实源约定）。");
        sb.AppendLine();
        sb.AppendLine("## 1. 权限码");
        sb.AppendLine();
        sb.AppendLine($"资源 `{resource}` × 已启用操作，两段式 `{{资源}}:{{操作}}`（同 `code_gen:read`）：");
        sb.AppendLine();
        sb.AppendLine("| 权限码 | 说明 | 需审计 |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var action in actions)
        {
            var meta = Shared.MetaOf(action);
            sb.AppendLine($"| `{resource}:{action}` | {meta.Title} | {(meta.IsRequireAudit ? "是" : "否")} |");
        }

        sb.AppendLine();
        sb.AppendLine("## 2. 产物清单（本目录）");
        sb.AppendLine();
        sb.AppendLine("| 文件 | 目标位置 | 写入策略 |");
        sb.AppendLine("| --- | --- | --- |");
        sb.AppendLine($"| `{context.ClassName}PermissionCodes.cs` | `{ns}/Domain/Permissions/` | 总是覆盖（纯推导） |");
        sb.AppendLine($"| `{context.ClassName}PermissionDefinitions.cs` | `{ns}/Domain/Permissions/` | 总是覆盖（纯推导） |");
        sb.AppendLine($"| `{context.ClassName}PermissionSeeder.cs` | `{ns}/Infrastructure/Seeders/` | 仅首次创建（Order 需人工确认） |");
        sb.AppendLine($"| `{context.ClassName}MenuSeeder.cs` | `{ns}/Infrastructure/Seeders/` | 仅首次创建（Order 需人工确认） |");
        sb.AppendLine($"| `{context.ClassName}PageRegistry.snippet.txt` | 粘贴到 Saas `PageRegistry`（可选，替代 MenuSeeder） | 参考片段 |");
        sb.AppendLine();
        sb.AppendLine("## 3. 落地步骤（3 步）");
        sb.AppendLine();
        sb.AppendLine("1. **复制文件**：按上表把权限码常量类、权限定义类、两个种子骨架复制到目标模块对应目录。");
        sb.AppendLine("   同时在生成的 AppService/QueryService 方法上按需应用 `[PermissionAuthorize(" + context.ClassName + "PermissionCodes.Xxx)]`。");
        sb.AppendLine("2. **确认 Order 与注册**：种子骨架里的 `Order` 为占位（默认 200 段），确认不与既有 Seeder 冲突；");
        sb.AppendLine("   在模块 `ServiceCollectionExtensions` 里 `AddDataSeeder<>` 注册两个种子。");
        sb.AppendLine("   菜单如走 Saas `PageRegistry` 单一事实源，则用 `" + context.ClassName + "PageRegistry.snippet.txt` 的条目替代 MenuSeeder。");
        sb.AppendLine("3. **重建数据库**：菜单、权限、超管授权即到位，页面可访问。");
        sb.AppendLine();
        sb.AppendLine("> 菜单规格：MenuCode=`" + resource + "`、Path=`/" + Shared.ModuleLower(context) + "/" + kebab + "`、Component=`" + Shared.Component(context) + "`、RouteName=`" + Shared.RouteName(context) + "`、I18nKey=`menu." + resource + "`、绑定 `" + resource + ":read` 可见性。");
        sb.AppendLine($"> ParentId：{(context.Options.TryGetValue("ParentMenuId", out var pid) && pid is not null ? $"表配置 ParentMenuId=`{pid}`" : "未设置 → 顶级菜单（设置表配置 ParentMenuId 可挂父菜单）")}。");
        sb.AppendLine();
        sb.AppendLine("> **勿改 `.Generated.cs`**：生成产物分机器文件（`.Generated.cs`/`.generated.ts`，总是覆盖）与人类文件（首次创建、永不覆盖）。自定义代码写在人类文件里。");

        return new GeneratedArtifact($"{Shared.OutputFolder}/README.md", "README.md", sb.ToString(), Shared.TemplateCode);
    }
}
