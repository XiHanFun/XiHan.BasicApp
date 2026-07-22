// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using Shared = XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.MenuPermissionArtifactShared;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 权限定义二阶产物生成器（{Class}PermissionDefinitions.cs：资源 × 操作的单一事实源）
/// </summary>
/// <remarks>
/// 产出一份自包含的声明式定义（不依赖 Saas 类型），由生成的 {Class}PermissionSeeder 消费，
/// 使"资源 + 操作 + 权限项"在种子与常量之间只有一处描述。纯推导 → 总是覆盖。
/// </remarks>
internal static class PermissionSeedArtifactGenerator
{
    /// <summary>
    /// 构建权限定义类
    /// </summary>
    public static GeneratedArtifact Build(CodeGenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var resource = Shared.Resource(context);
        var ns = Shared.ResolveNamespace(context);
        var display = Shared.Display(context);
        var className = $"{context.ClassName}PermissionDefinitions";
        var itemClass = $"{context.ClassName}PermissionItem";
        var resourcePath = $"/api/{Shared.ModuleLower(context)}/{Shared.Kebab(context)}";

        var sb = new StringBuilder();
        sb.AppendLine($"namespace {ns}.Domain.Permissions;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {display} 权限定义（资源 × 已启用操作的单一事实源，供 {context.ClassName}PermissionSeeder 消费）");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>资源编码（权限码资源段，取表名，需全局唯一）</summary>");
        sb.AppendLine($"    public const string Resource = \"{resource}\";");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>资源名称</summary>");
        sb.AppendLine($"    public const string ResourceName = \"{display}\";");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>资源 API 路径（登记 SysResource 用）</summary>");
        sb.AppendLine($"    public const string ResourcePath = \"{resourcePath}\";");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>权限项（= 资源 × 操作；顺序即种子插入序；操作字典由平台统一维护）</summary>");
        sb.AppendLine($"    public static readonly IReadOnlyList<{itemClass}> Items =");
        sb.AppendLine("    [");
        foreach (var action in Shared.EffectiveActions(context))
        {
            var meta = Shared.MetaOf(action);
            var audit = meta.IsRequireAudit ? "true" : "false";
            sb.AppendLine($"        new(\"{action}\", \"{display}-{meta.Title}\", \"{meta.Title}{display}\", {audit}),");
        }

        sb.AppendLine("    ];");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {display} 权限项");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"Action\">操作码（对齐平台操作字典 SysOperation）</param>");
        sb.AppendLine("/// <param name=\"Name\">权限名称</param>");
        sb.AppendLine("/// <param name=\"Description\">权限说明</param>");
        sb.AppendLine("/// <param name=\"IsRequireAudit\">是否需要审计</param>");
        sb.AppendLine($"public sealed record {itemClass}(string Action, string Name, string Description, bool IsRequireAudit);");

        var fileName = $"{className}.cs";
        return new GeneratedArtifact($"{Shared.OutputFolder}/{fileName}", fileName, sb.ToString(), Shared.TemplateCode);
    }
}
