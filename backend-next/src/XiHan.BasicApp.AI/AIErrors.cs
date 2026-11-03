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
    /// 已有同编码的提供商
    /// </summary>
    public const string ProviderCodeTaken = "AI_CONFLICT_101";

    /// <summary>
    /// 停用的提供商不能设为缺省
    /// </summary>
    public const string ProviderDisabled = "AI_CONFLICT_102";

    /// <summary>
    /// 列表不支持该筛选或排序
    /// </summary>
    public const string QueryUnsupported = "AI_VALIDATION_101";

    /// <summary>
    /// 提供商的 API 密钥解不开
    /// </summary>
    public const string ProviderSecretUnreadable = "AI_UNAVAILABLE_101";

    /// <summary>
    /// 没有 OpenAI 兼容协议的模型 Provider
    /// </summary>
    public const string ProviderKindUnavailable = "AI_UNAVAILABLE_102";
}
