// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块的权限码
/// </summary>
/// <remarks>
/// 4.x 的 <c>ai:*</c> 按资源拆开（6.0 权限码的资源段要与操作一致），动作照 4.x 各方法要的那个码：启停与设缺省归 <c>update</c>，测试连接归 <c>execute</c>
/// 提供商与提示词只在平台一侧（照 4.x：平台配置、租户经助手用到）；知识文档两侧都有（方案 A2：租户维护自己的知识）
/// </remarks>
public static class AIPermissions
{
    /// <summary>
    /// 查看提供商
    /// </summary>
    public const string AiProvidersRead = "ai.ai-providers.read";

    /// <summary>
    /// 新建提供商
    /// </summary>
    public const string AiProvidersCreate = "ai.ai-providers.create";

    /// <summary>
    /// 修改、启停提供商与设为缺省
    /// </summary>
    public const string AiProvidersUpdate = "ai.ai-providers.update";

    /// <summary>
    /// 删除提供商
    /// </summary>
    public const string AiProvidersDelete = "ai.ai-providers.delete";

    /// <summary>
    /// 测试提供商的连接
    /// </summary>
    public const string AiProvidersExecute = "ai.ai-providers.execute";

    /// <summary>
    /// 查看提示词
    /// </summary>
    public const string AiPromptsRead = "ai.ai-prompts.read";

    /// <summary>
    /// 新建提示词
    /// </summary>
    public const string AiPromptsCreate = "ai.ai-prompts.create";

    /// <summary>
    /// 修改与启停提示词
    /// </summary>
    public const string AiPromptsUpdate = "ai.ai-prompts.update";

    /// <summary>
    /// 删除提示词
    /// </summary>
    public const string AiPromptsDelete = "ai.ai-prompts.delete";

    /// <summary>
    /// 查看知识文档
    /// </summary>
    public const string KnowledgeDocumentsRead = "ai.knowledge-documents.read";

    /// <summary>
    /// 摄入知识文档
    /// </summary>
    public const string KnowledgeDocumentsCreate = "ai.knowledge-documents.create";

    /// <summary>
    /// 重建知识文档的索引
    /// </summary>
    public const string KnowledgeDocumentsUpdate = "ai.knowledge-documents.update";

    /// <summary>
    /// 删除知识文档
    /// </summary>
    public const string KnowledgeDocumentsDelete = "ai.knowledge-documents.delete";

    /// <summary>
    /// 检索知识库与知识问答
    /// </summary>
    public const string KnowledgeDocumentsExecute = "ai.knowledge-documents.execute";

    /// <summary>
    /// 全部权限描述
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(AiProvidersRead, "Read AI providers", PermissionScope.Platform, description: "View the model providers; API keys are never shown."),
        new(AiProvidersCreate, "Create AI providers", PermissionScope.Platform, audited: true, description: "Add OpenAI-compatible model providers."),
        new(AiProvidersUpdate, "Update AI providers", PermissionScope.Platform, audited: true, description: "Edit, enable, disable and set the default model provider."),
        new(AiProvidersDelete, "Delete AI providers", PermissionScope.Platform, audited: true, description: "Delete model providers."),
        new(AiProvidersExecute, "Test AI providers", PermissionScope.Platform, description: "Send a test request to a model provider's chat and embedding models."),
        new(AiPromptsRead, "Read AI prompts", PermissionScope.Platform, description: "View the prompts and their content."),
        new(AiPromptsCreate, "Create AI prompts", PermissionScope.Platform, audited: true, description: "Add prompts."),
        new(AiPromptsUpdate, "Update AI prompts", PermissionScope.Platform, audited: true, description: "Edit, enable and disable prompts."),
        new(AiPromptsDelete, "Delete AI prompts", PermissionScope.Platform, audited: true, description: "Delete prompts."),
        new(KnowledgeDocumentsRead, "Read knowledge documents", PermissionScope.Both, description: "View this side's knowledge documents and their content."),
        new(KnowledgeDocumentsCreate, "Ingest knowledge documents", PermissionScope.Both, audited: true, description: "Add text documents to this side's knowledge base."),
        new(KnowledgeDocumentsUpdate, "Reindex knowledge documents", PermissionScope.Both, audited: true, description: "Rebuild the index of a knowledge document from its content."),
        new(KnowledgeDocumentsDelete, "Delete knowledge documents", PermissionScope.Both, audited: true, description: "Delete knowledge documents and their vectors."),
        new(KnowledgeDocumentsExecute, "Search the knowledge base", PermissionScope.Both, description: "Search this side's knowledge base and ask questions against it."),
    ];
}
