// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块的页面登记
/// </summary>
/// <remarks>
/// 菜单按全局号段取 910～940，排在代码生成之后；页面码、路由与目录都按包
/// </remarks>
public static class AIPages
{
    /// <summary>
    /// 助手页
    /// </summary>
    public static PageDescriptor Assistants { get; } = new("ai.assistants", "/ai/assistants", "ai.assistants.title", "lucide:bot", 920, AIPermissions.AiAssistantsRead, directory: MenuDirectories.AiCenter);

    /// <summary>
    /// 提示词页
    /// </summary>
    public static PageDescriptor Prompts { get; } = new("ai.prompts", "/ai/prompts", "ai.prompts.title", "lucide:file-text", 930, AIPermissions.AiPromptsRead, directory: MenuDirectories.AiCenter);

    /// <summary>
    /// 知识库页：平台与租户各自维护（方案 A2）
    /// </summary>
    public static PageDescriptor KnowledgeDocuments { get; } = new("ai.knowledge-documents", "/ai/knowledge-documents", "ai.knowledge_documents.title", "lucide:book-open", 940, AIPermissions.KnowledgeDocumentsRead, directory: MenuDirectories.AiCenter);

    /// <summary>
    /// 提供商页
    /// </summary>
    public static PageDescriptor Providers { get; } = new("ai.providers", "/ai/providers", "ai.providers.title", "lucide:sparkles", 910, AIPermissions.AiProvidersRead, directory: MenuDirectories.AiCenter);

    /// <summary>
    /// 全部顶层页面
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Assistants, Prompts, KnowledgeDocuments, Providers];
}
