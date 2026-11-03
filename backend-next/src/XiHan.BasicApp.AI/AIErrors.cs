// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块的错误码
/// </summary>
/// <remarks>
/// 与框架 AI 子系统的错误码同一个前缀 <c>AI_</c>：框架占 001 段，BasicApp 自己的各类从 101 起编（照 Workflow 的做法），前端按码就分得清是谁报的
/// 框架的失败（<c>AI_NOTFOUND_001</c> 提供商未配置、<c>AI_UNAVAILABLE_001</c> 模型不可达等）原样返回，不另起码。并发戳过期由框架仓储报（<c>PERSISTENCE_CONFLICT_*</c>）
/// </remarks>
public static class AIErrors
{
    /// <summary>
    /// 提供商不存在
    /// </summary>
    public const string ProviderNotFound = "AI_NOTFOUND_101";

    /// <summary>
    /// 提示词不存在
    /// </summary>
    public const string PromptNotFound = "AI_NOTFOUND_102";

    /// <summary>
    /// 知识文档不存在
    /// </summary>
    public const string KnowledgeDocumentNotFound = "AI_NOTFOUND_103";

    /// <summary>
    /// 助手不存在
    /// </summary>
    public const string AssistantNotFound = "AI_NOTFOUND_104";

    /// <summary>
    /// 已有同编码的提供商
    /// </summary>
    public const string ProviderCodeTaken = "AI_CONFLICT_101";

    /// <summary>
    /// 停用的提供商不能设为缺省
    /// </summary>
    public const string ProviderDisabled = "AI_CONFLICT_102";

    /// <summary>
    /// 已有同编码的提示词
    /// </summary>
    public const string PromptCodeTaken = "AI_CONFLICT_103";

    /// <summary>
    /// 提供商还被助手或知识文档引用，不能删除
    /// </summary>
    public const string ProviderInUse = "AI_CONFLICT_104";

    /// <summary>
    /// 已有同编码的助手
    /// </summary>
    public const string AssistantCodeTaken = "AI_CONFLICT_105";

    /// <summary>
    /// 停用的助手不能设为缺省
    /// </summary>
    public const string AssistantDisabled = "AI_CONFLICT_106";

    /// <summary>
    /// 提示词还被助手引用，不能删除
    /// </summary>
    public const string PromptInUse = "AI_CONFLICT_107";

    /// <summary>
    /// 列表不支持该筛选或排序
    /// </summary>
    public const string QueryUnsupported = "AI_VALIDATION_101";

    /// <summary>
    /// 知识文档指定的嵌入提供商不存在或已停用
    /// </summary>
    public const string KnowledgeProviderUnavailable = "AI_VALIDATION_102";

    /// <summary>
    /// 知识文档切不出块
    /// </summary>
    public const string KnowledgeNoChunks = "AI_VALIDATION_103";

    /// <summary>
    /// 助手引用的提供商不存在或已停用
    /// </summary>
    public const string AssistantProviderUnavailable = "AI_VALIDATION_104";

    /// <summary>
    /// 助手引用的提示词不存在或已停用
    /// </summary>
    public const string AssistantPromptUnavailable = "AI_VALIDATION_105";

    /// <summary>
    /// 助手勾选的工具不在工具目录里
    /// </summary>
    public const string AssistantToolUnavailable = "AI_VALIDATION_106";

    /// <summary>
    /// 提供商的 API 密钥解不开
    /// </summary>
    public const string ProviderSecretUnreadable = "AI_UNAVAILABLE_101";

    /// <summary>
    /// 没有 OpenAI 兼容协议的模型 Provider
    /// </summary>
    public const string ProviderKindUnavailable = "AI_UNAVAILABLE_102";

    /// <summary>
    /// 助手引用的提示词不存在或已停用，回复不了
    /// </summary>
    public const string AssistantPromptDisabled = "AI_UNAVAILABLE_103";
}
