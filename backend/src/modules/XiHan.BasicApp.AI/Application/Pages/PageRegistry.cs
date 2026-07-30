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
    /// 开发工具目录码（跨模块共用的父级）
    /// </summary>
    public const string DevelopDirectoryCode = "develop";

    /// <summary>
    /// 开发工具目录定义（AI 与代码生成模块共用，两边必须保持一致）
    /// </summary>
    public static PageDescriptor DevelopDirectory { get; } =
        new(DevelopDirectoryCode, "开发工具", "menu.develop", MenuType.Directory, "/develop", "Develop", null, null, null, "lucide:hammer", 801);

    /// <summary>
    /// 所有已登记页面（父目录必须排在子项之前，种子依顺序解析 ParentId）
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } =
    [
        DevelopDirectory,
        new("ai_provider", "AI 提供商", "menu.ai_provider", MenuType.Menu, "/develop/aiProvider", "DevelopAiProvider", "Develop/AiProvider/Index", DevelopDirectoryCode, AiPermissionCodes.Read, "lucide:sparkles", 802),
        new("knowledge_base", "知识库", "menu.knowledge_base", MenuType.Menu, "/develop/knowledge", "DevelopKnowledge", "Develop/Knowledge/Index", DevelopDirectoryCode, KnowledgePermissionCodes.Read, "lucide:book-open", 803),
        new("ai_prompt", "AI 提示词", "menu.ai_prompt", MenuType.Menu, "/develop/aiPrompt", "DevelopAiPrompt", "Develop/AiPrompt/Index", DevelopDirectoryCode, AiPromptPermissionCodes.Read, "lucide:file-text", 804),
        new("ai_assistant", "AI 助手", "menu.ai_assistant", MenuType.Menu, "/develop/aiAssistant", "DevelopAiAssistant", "Develop/AiAssistant/Index", DevelopDirectoryCode, AiAssistantPermissionCodes.Read, "lucide:bot", 805),
    ];

    /// <summary>
    /// 页面内按钮（本模块暂无独立按钮级权限）
    /// </summary>
    public static IReadOnlyList<ButtonDescriptor> Buttons { get; } = [];
}
