// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.AI.Application.Pages;

/// <summary>
/// AI 模块页面登记表 — 本模块页面的单一事实源，菜单种子数据从此处生成
/// </summary>
/// <remarks>
/// 沿用 Saas 侧 <see cref="PageDescriptor"/> 的一致性约定（Component 对应 src/views 目录、
/// I18nKey 为 menu.{Code 中 . 与 - 替换为 _} 并在前端 menu.ts 维护双语文案）。
/// 开发工具目录跨模块共用：AI 与代码生成各自登记同一份定义，按 MenuCode 幂等落库，谁先跑谁建。
/// </remarks>
public static class PageRegistry
{
    /// <summary>
    /// AI 应用目录码
    /// </summary>
    public const string AiAppDirectoryCode = "ai_app";

    /// <summary>
    /// AI 应用目录定义
    /// </summary>
    public static PageDescriptor AiAppDirectory { get; } =
        new(AiAppDirectoryCode, "AI 应用", "menu.ai_app", MenuType.Directory, "/ai", "AiApp", null, null, null, "lucide:brain-circuit", 100);

    /// <summary>
    /// 所有已登记页面（父目录必须排在子项之前，种子依顺序解析 ParentId）
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } =
    [
        AiAppDirectory,
        new("ai_assistant", "AI 助手", "menu.ai_assistant", MenuType.Menu, "/develop/aiAssistant", "DevelopAiAssistant", "Develop/AiAssistant/Index", AiAppDirectoryCode, AiAssistantPermissionCodes.Read, "lucide:bot", 110),
        new("ai_prompt", "AI 提示词", "menu.ai_prompt", MenuType.Menu, "/develop/aiPrompt", "DevelopAiPrompt", "Develop/AiPrompt/Index", AiAppDirectoryCode, AiPromptPermissionCodes.Read, "lucide:file-text", 120),
        new("knowledge_base", "AI 知识库", "menu.knowledge_base", MenuType.Menu, "/develop/knowledge", "DevelopKnowledge", "Develop/Knowledge/Index", AiAppDirectoryCode, KnowledgePermissionCodes.Read, "lucide:book-open", 130),
        new("ai_provider", "AI 提供商", "menu.ai_provider", MenuType.Menu, "/develop/aiProvider", "DevelopAiProvider", "Develop/AiProvider/Index", AiAppDirectoryCode, AiPermissionCodes.Read, "lucide:sparkles", 140),
    ];

    /// <summary>
    /// 页面内按钮（菜单种子据此生成按钮节点，前端按按钮码门控页面动作）
    /// </summary>
    /// <remarks>
    /// 设为默认、启停与编辑是同一个更新权限，共用编辑按钮；提供商的连通性测试要执行权限，单列一个按钮。
    /// </remarks>
    public static IReadOnlyList<ButtonDescriptor> Buttons { get; } =
    [
        new("ai_assistant.create", "新增", "ai_assistant", AiAssistantPermissionCodes.Create, 1),
        new("ai_assistant.update", "编辑", "ai_assistant", AiAssistantPermissionCodes.Update, 2),
        new("ai_assistant.delete", "删除", "ai_assistant", AiAssistantPermissionCodes.Delete, 3),

        new("ai_prompt.create", "新增", "ai_prompt", AiPromptPermissionCodes.Create, 1),
        new("ai_prompt.update", "编辑", "ai_prompt", AiPromptPermissionCodes.Update, 2),
        new("ai_prompt.delete", "删除", "ai_prompt", AiPromptPermissionCodes.Delete, 3),

        new("knowledge_base.create", "新增", "knowledge_base", KnowledgePermissionCodes.Create, 1),
        new("knowledge_base.update", "重建索引", "knowledge_base", KnowledgePermissionCodes.Update, 2),
        new("knowledge_base.delete", "删除", "knowledge_base", KnowledgePermissionCodes.Delete, 3),

        new("ai_provider.create", "新增", "ai_provider", AiPermissionCodes.Create, 1),
        new("ai_provider.update", "编辑", "ai_provider", AiPermissionCodes.Update, 2),
        new("ai_provider.delete", "删除", "ai_provider", AiPermissionCodes.Delete, 3),
        new("ai_provider.test", "连通性测试", "ai_provider", AiPermissionCodes.Execute, 4),
    ];
}
