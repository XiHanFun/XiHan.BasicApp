// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成模块的页面登记
/// </summary>
/// <remarks>
/// 只有代码生成一页、只在平台一侧，四个页签照 4.x（表配置、数据源、模板、生成历史），各页签按自己资源的读权限显示
/// 4.x 挂在「开发中心」目录下（<c>/develop/codeGen</c>），6.0 的页面码与路由按包、目录按包，旧路由不保留
/// </remarks>
public static class CodeGenerationPages
{
    /// <summary>
    /// 代码生成页
    /// </summary>
    public static PageDescriptor CodeGen { get; } = new("codegeneration.code-gen", "/codegeneration/code-gen", "codegeneration.code_gen.title", "lucide:code-xml", 810, CodeGenerationPermissions.TablesRead);

    /// <summary>
    /// 全部顶层页面
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [CodeGen];
}
