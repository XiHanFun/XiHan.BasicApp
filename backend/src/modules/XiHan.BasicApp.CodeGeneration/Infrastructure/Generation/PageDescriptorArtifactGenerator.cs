#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PageDescriptorArtifactGenerator
// Guid:f91d4a15-75ac-4082-b677-ac927444fc92
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/21 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using Shared = XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.MenuPermissionArtifactShared;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// PageRegistry 片段二阶产物生成器（{Class}PageRegistry.snippet.txt）
/// </summary>
/// <remarks>
/// 若目标应用走 Saas <c>PageRegistry</c> 单一事实源（而非本模块自带 MenuSeeder），
/// 把片段里的 <c>PageDescriptor</c> / <c>ButtonDescriptor</c> 条目粘贴进 PageRegistry.All / .Buttons 即可。
/// 输出为参考片段（非独立编译文件），纯推导 → 总是覆盖。
/// </remarks>
internal static class PageDescriptorArtifactGenerator
{
    /// <summary>
    /// 构建 PageRegistry 粘贴片段
    /// </summary>
    public static GeneratedArtifact Build(CodeGenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var resource = Shared.Resource(context);
        var display = Shared.Display(context);
        var codes = $"{context.ClassName}PermissionCodes";
        var pageCode = $"{Shared.ModuleLower(context)}.{Shared.Kebab(context)}";
        var path = $"/{Shared.ModuleLower(context)}/{Shared.Kebab(context)}";
        var effective = Shared.EffectiveActions(context);
        var parentNote = context.Options.TryGetValue("ParentMenuId", out var pid) && pid is not null
            ? $"父页面码（表配置 ParentMenuId={pid}，请换成对应父页面的 Code 字符串）"
            : "null（顶级菜单；如需挂父目录，改成父页面码字符串）";

        var sb = new StringBuilder();
        sb.AppendLine($"// {display} PageRegistry 片段（可选：走 Saas PageRegistry 单一事实源时使用，替代 {context.ClassName}MenuSeeder）");
        sb.AppendLine("//");
        sb.AppendLine("// 用法：");
        sb.AppendLine($"//   1) 确保已引用生成的权限码常量类 {codes}（using 到其命名空间）。");
        sb.AppendLine("//   2) 把下面 PageDescriptor 条目粘贴进 PageRegistry.All（父目录须排在子项之前）。");
        sb.AppendLine("//   3) 把 ButtonDescriptor 条目粘贴进 PageRegistry.Buttons。");
        sb.AppendLine("//   4) 按需调整 Icon / Sort / ParentCode。");
        sb.AppendLine("//");
        sb.AppendLine("// —— PageRegistry.All ——");
        sb.AppendLine($"// 参数顺序：Code, Title, I18nKey, MenuType, Path, RouteName, Component, ParentCode, PermissionCode, Icon, Sort");
        sb.AppendLine($"new(\"{pageCode}\", \"{display}\", \"menu.{resource}\", MenuType.Menu, \"{path}\", \"{Shared.RouteName(context)}\",");
        sb.AppendLine($"    \"{Shared.Component(context)}\", /* ParentCode: */ null, {codes}.Read, \"lucide:table\", /* Sort: */ 999),");
        sb.AppendLine($"// 备注：ParentCode 当前 {parentNote}");
        sb.AppendLine();
        sb.AppendLine("// —— PageRegistry.Buttons ——");
        sb.AppendLine("// 参数顺序：Code, Title, ParentCode, PermissionCode, Sort");

        var sort = 1;
        foreach (var button in ButtonPermissionMappings.Buttons)
        {
            // 仅取写操作按钮（新增/编辑/删除…），且该动作已启用；查询/详情走列表页读取权限，无独立按钮
            if (button.Action == "read" || !effective.Contains(button.Action))
            {
                continue;
            }

            sb.AppendLine($"new(\"{pageCode}.{button.Key}\", \"{button.Title}\", \"{pageCode}\", {codes}.{Shared.Pascalize(button.Action)}, {sort}),");
            sort++;
        }

        var fileName = $"{context.ClassName}PageRegistry.snippet.txt";
        return new GeneratedArtifact($"{Shared.OutputFolder}/{fileName}", fileName, sb.ToString(), Shared.TemplateCode);
    }
}
