// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成模块的页面登记
/// </summary>
/// <remarks>
/// 只有代码生成一页、只在平台一侧，四个页签（表配置、数据源、模板、生成历史）各按自己资源的读权限显示
/// 页面码、路由与目录都按包
/// </remarks>
public static class CodeGenerationPages
{
    /// <summary>
    /// 代码生成页
    /// </summary>
    public static PageDescriptor CodeGen { get; } = new("codegeneration.code-gen", "/codegeneration/code-gen", "codegeneration.code_gen.title", "lucide:code-xml", 810, CodeGenerationPermissions.TablesRead, directory: MenuDirectories.OnlineDev);

    /// <summary>
    /// 全部顶层页面
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [CodeGen];
}
