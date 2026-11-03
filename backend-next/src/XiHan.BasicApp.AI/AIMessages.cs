// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块的消息键
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（资源段的 <c>-</c> 换成 <c>_</c>）
/// 业务消息的主题段用复数资源（<c>ai.providers.*</c>），与框架 AI 子系统的 <c>ai.provider.*</c>、<c>ai.prompt.*</c> 等分开
/// </remarks>
public static class AIMessages
{
    /// <summary>
    /// 消息目录名
    /// </summary>
    public const string CatalogName = "basicapp.ai";

    /// <summary>
    /// 操作 <c>ai-providers.list</c> 的说明
    /// </summary>
    public const string ListAiProvidersDescription = "ai_providers.list.description";

    /// <summary>
    /// 操作 <c>ai-providers.get</c> 的说明
    /// </summary>
    public const string GetAiProviderDescription = "ai_providers.get.description";

    /// <summary>
    /// 操作 <c>ai-providers.create</c> 的说明
    /// </summary>
    public const string CreateAiProviderDescription = "ai_providers.create.description";

    /// <summary>
    /// 操作 <c>ai-providers.update</c> 的说明
    /// </summary>
    public const string UpdateAiProviderDescription = "ai_providers.update.description";

    /// <summary>
    /// 操作 <c>ai-providers.set-status</c> 的说明
    /// </summary>
    public const string SetAiProviderStatusDescription = "ai_providers.set_status.description";

    /// <summary>
    /// 操作 <c>ai-providers.set-default</c> 的说明
    /// </summary>
    public const string SetDefaultAiProviderDescription = "ai_providers.set_default.description";

    /// <summary>
    /// 操作 <c>ai-providers.delete</c> 的说明
    /// </summary>
    public const string DeleteAiProvidersDescription = "ai_providers.delete.description";

    /// <summary>
    /// 操作 <c>ai-providers.test</c> 的说明
    /// </summary>
    public const string TestAiProviderDescription = "ai_providers.test.description";

    /// <summary>
    /// 提供商不存在（参数 <c>id</c>）
    /// </summary>
    public const string ProviderNotFound = "ai.providers.not_found";

    /// <summary>
    /// 已有同编码的提供商（参数 <c>code</c>）
    /// </summary>
    public const string ProviderCodeTaken = "ai.providers.code_taken";

    /// <summary>
    /// 停用的提供商不能设为缺省（参数 <c>code</c>）
    /// </summary>
    public const string ProviderDisabled = "ai.providers.disabled";

    /// <summary>
    /// 端点不是绝对的 http 或 https 地址
    /// </summary>
    public const string ProviderEndpointInvalid = "ai.providers.endpoint_invalid";

    /// <summary>
    /// API 密钥解不开（参数 <c>code</c>）
    /// </summary>
    public const string ProviderSecretUnreadable = "ai.providers.secret_unreadable";

    /// <summary>
    /// 没有 OpenAI 兼容协议的模型 Provider
    /// </summary>
    public const string ProviderKindUnavailable = "ai.providers.kind_unavailable";

    /// <summary>
    /// 列表不支持的筛选（参数 <c>field</c>、<c>operator</c>）
    /// </summary>
    public const string FilterUnsupported = "ai.query.filter_unsupported";

    /// <summary>
    /// 列表不支持的排序（参数 <c>field</c>）
    /// </summary>
    public const string SortUnsupported = "ai.query.sort_unsupported";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(AIMessages)));

    /// <summary>
    /// 本包的消息目录
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
