// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块的页面登记
/// </summary>
/// <remarks>
/// 图标与相对次序照 4.x（助手、提示词、知识库、提供商，4.x 的 110～140）；菜单按全局号段排，6.0 取 910～940，排在代码生成之后
/// 4.x 挂在「AI 应用」目录下、路由在 <c>/develop/*</c>，6.0 的页面码与路由按包、目录按包，旧路由不保留
/// </remarks>
public static class AIPages
{
    /// <summary>
    /// 提示词页
    /// </summary>
    public static PageDescriptor Prompts { get; } = new("ai.prompts", "/ai/prompts", "ai.prompts.title", "lucide:file-text", 920, AIPermissions.AiPromptsRead);

    /// <summary>
    /// 提供商页
    /// </summary>
    public static PageDescriptor Providers { get; } = new("ai.providers", "/ai/providers", "ai.providers.title", "lucide:sparkles", 940, AIPermissions.AiProvidersRead);

    /// <summary>
    /// 全部顶层页面
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Prompts, Providers];
}
