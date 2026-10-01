// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using Shared = XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.MenuPermissionArtifactShared;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// PageRegistry 片段二阶产物生成器（{Class}PageRegistry.snippet.txt）
/// </summary>
/// <remarks>
/// 生成的 MenuPages 已登记页面与按钮（由平台汇总种子写入）；要并进模块自己的 <c>PageRegistry</c> 时，
/// 把片段里的 <c>PageDescriptor</c> / <c>ButtonDescriptor</c> 条目粘贴进 PageRegistry.All / .Buttons，并删掉 MenuPages。
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
        // 片段是要粘进 C# 的：显示名进字面量按 C# 字符串转义
        var displayLiteral = TemplateTextEscaper.CSharpString(display);
        var codes = $"{context.ClassName}PermissionCodes";
        var pageCode = Shared.PageCode(context);
        var path = $"/{Shared.ModuleLower(context)}/{Shared.Kebab(context)}";
        var parentLiteral = context.ParentMenuCode is null ? "null" : $"\"{TemplateTextEscaper.CSharpString(context.ParentMenuCode)}\"";
        var parentNote = context.ParentMenuCode is null
            ? "null（顶级菜单；如需挂父目录，改成父页面码字符串）"
            : $"表配置所选目录的菜单码 {context.ParentMenuCode}";

        var sb = new StringBuilder();
        sb.AppendLine($"// {displayLiteral} PageRegistry 片段");
        sb.AppendLine($"// 与 {context.ClassName}MenuPages 二选一：它已登记同样的页面行与按钮行，由平台汇总种子写入。");
        sb.AppendLine($"// 要并进模块自己的 PageRegistry 时把下面的条目粘进去，并删掉 {context.ClassName}MenuPages，不要两边都登记。");
        sb.AppendLine($"// 生成页面的操作按钮用按钮码 {pageCode}.{{create|update|delete|export|import}} 门控，按钮行不能漏。");
        sb.AppendLine("//");
        sb.AppendLine("// 用法：");
        sb.AppendLine($"//   1) 确保已引用生成的权限码常量类 {codes}（using 到其命名空间）。");
        sb.AppendLine("//   2) 把下面 PageDescriptor 条目粘贴进 PageRegistry.All（父目录须排在子项之前）。");
        sb.AppendLine("//   3) 把 ButtonDescriptor 条目粘贴进 PageRegistry.Buttons。");
        sb.AppendLine("//   4) 按需调整 Icon / Sort / ParentCode。");
        sb.AppendLine("//");
        sb.AppendLine("// —— PageRegistry.All ——");
        sb.AppendLine($"// 参数顺序：Code, Title, I18nKey, MenuType, Path, RouteName, Component, ParentCode, PermissionCode, Icon, Sort");
        sb.AppendLine($"new(\"{pageCode}\", \"{displayLiteral}\", I18nKey: null, MenuType.Menu, \"{path}\", \"{Shared.RouteName(context)}\",");
        sb.AppendLine($"    \"{Shared.Component(context)}\", /* ParentCode: */ {parentLiteral}, {codes}.Read, \"lucide:table\", /* Sort: */ 999),");
        sb.AppendLine($"// 备注：ParentCode 当前 {parentNote}");
        sb.AppendLine();
        sb.AppendLine("// —— PageRegistry.Buttons ——");
        sb.AppendLine("// 参数顺序：Code, Title, ParentCode, PermissionCode, Sort");

        var sort = 1;
        foreach (var button in Shared.EnabledButtons(context))
        {
            sb.AppendLine($"new(\"{pageCode}.{button.Key}\", \"{button.Title}\", \"{pageCode}\", {codes}.{Shared.Pascalize(button.Action)}, {sort}),");
            sort++;
        }

        var fileName = $"{context.ClassName}PageRegistry.snippet.txt";
        return new GeneratedArtifact($"{Shared.OutputFolder}/{fileName}", fileName, sb.ToString(), Shared.TemplateCode, Side: ArtifactSide.Backend);
    }
}
