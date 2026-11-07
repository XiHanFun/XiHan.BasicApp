// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.AI.Abstractions.Providers;
using XiHan.Framework.AI.OpenAI;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Domain.Services;

/// <summary>
/// 提供商的规则
/// </summary>
/// <remarks>
/// 编码唯一、缺省最多一个（照 4.x）；API 密钥经 <see cref="ISecretProtector"/> 加解密，解不开按未配置处理（照 4.x 的 fail-closed，不当明文用）
/// 写入之后经 <see cref="AiProviderChanges"/> 记下，工作单元提交之后重载 Provider 目录（<c>AiProviderCatalogMiddleware</c>）
/// </remarks>
/// <param name="providers">提供商仓储</param>
/// <param name="secrets">密钥保护</param>
/// <param name="changes">本次调用对提供商的改动</param>
public sealed class AiProviderService(IRepository<SysAiProvider, Guid> providers, ISecretProtector secrets, AiProviderChanges changes)
{
    /// <summary>
    /// API 密钥的加密用途
    /// </summary>
    public const string ApiKeyPurpose = "XiHan.BasicApp.AI.ProviderApiKey";

    /// <summary>
    /// 协议种类
    /// </summary>
    /// <remarks>
    /// 只有 OpenAI 兼容一种（照 4.x）
    /// </remarks>
    public const string Kind = OpenAIChatClientFactory.KindName;

    /// <summary>
    /// 不存在
    /// </summary>
    /// <param name="id">提供商 ID</param>
    public static Error NotFound(Guid id)
        => Error.NotFound(AIErrors.ProviderNotFound, AIMessages.ProviderNotFound, MessageArguments.Of(("id", id)));

    /// <summary>
    /// 去掉首尾空白，空白即 <see langword="null"/>
    /// </summary>
    /// <param name="value">原值</param>
    public static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// 是否是绝对的 http 或 https 地址
    /// </summary>
    /// <remarks>
    /// 框架的 Provider 配置只认这样的端点，别的写法构造即抛出
    /// </remarks>
    /// <param name="value">端点</param>
    public static bool IsEndpoint(string? value)
        => string.IsNullOrWhiteSpace(value)
           || (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

    /// <summary>
    /// 厂商不是 OpenAI 却没写端点
    /// </summary>
    /// <remarks>
    /// 厂商只用于分组，不决定地址：端点留空走 SDK 缺省的 OpenAI 官方地址，别家的密钥就发给了 OpenAI
    /// </remarks>
    public static bool MissesEndpoint(string? vendor, string? endpoint)
        => string.IsNullOrWhiteSpace(endpoint)
           && !string.IsNullOrWhiteSpace(vendor)
           && !string.Equals(vendor.Trim(), "OpenAI", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 按一行提供商得出框架的 Provider 配置
    /// </summary>
    /// <param name="provider">提供商</param>
    /// <param name="secrets">密钥保护</param>
    /// <returns>配置；API 密钥解不开即 <c>AI_UNAVAILABLE_101</c></returns>
    public static Result<AIProviderOptions> OptionsOf(SysAiProvider provider, ISecretProtector secrets)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(secrets);
        string? apiKey = null;
        if (!string.IsNullOrEmpty(provider.ApiKey) && !secrets.TryUnprotect(ApiKeyPurpose, provider.ApiKey, out apiKey))
        {
            return Error.Unavailable(AIErrors.ProviderSecretUnreadable, AIMessages.ProviderSecretUnreadable, MessageArguments.Of(("code", provider.Code)));
        }

        var options = new AIProviderOptions(provider.Code, Kind)
        {
            Endpoint = string.IsNullOrWhiteSpace(provider.Endpoint) ? null : new Uri(provider.Endpoint, UriKind.Absolute),
            ApiKey = apiKey,
            ChatModel = provider.ChatModel,
            EmbeddingModel = provider.EmbeddingModel,
        };
        return provider.TimeoutSeconds is > 0 and var seconds ? options with { Timeout = TimeSpan.FromSeconds(seconds) } : options;
    }

    /// <summary>
    /// 按 ID 取提供商
    /// </summary>
    /// <param name="id">提供商 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SysAiProvider>> FindAsync(Guid id, CancellationToken cancellationToken)
        => (await providers.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var provider) ? provider : NotFound(id);

    /// <summary>
    /// 编码是否已被别的提供商占用
    /// </summary>
    /// <param name="code">编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<bool> CodeTakenAsync(string code, CancellationToken cancellationToken)
        => await providers.CountAsync(EntityQuery.All<SysAiProvider>().Where(new Specification<SysAiProvider>(p => p.Code == code)), cancellationToken).ConfigureAwait(false) > 0;

    /// <summary>
    /// 加密 API 密钥
    /// </summary>
    /// <param name="plaintext">明文，空白即不存</param>
    /// <returns>密文；空白为 <see langword="null"/></returns>
    public string? Protect(string? plaintext)
        => string.IsNullOrWhiteSpace(plaintext) ? null : secrets.Protect(ApiKeyPurpose, plaintext.Trim());

    /// <summary>
    /// 按一行提供商得出框架的 Provider 配置
    /// </summary>
    /// <param name="provider">提供商</param>
    public Result<AIProviderOptions> OptionsOf(SysAiProvider provider) => OptionsOf(provider, secrets);

    /// <summary>
    /// 取消别的提供商的缺省标记
    /// </summary>
    /// <remarks>
    /// 缺省提供商最多一个（照 4.x）
    /// </remarks>
    /// <param name="except">保留缺省的提供商</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result> ClearOtherDefaultsAsync(Guid except, CancellationToken cancellationToken)
    {
        var others = await providers.ListAsync(EntityQuery.All<SysAiProvider>().Where(new Specification<SysAiProvider>(p => p.IsDefault && p.Id != except)), cancellationToken).ConfigureAwait(false);
        foreach (var other in others)
        {
            other.IsDefault = false;
            var updated = await providers.UpdateAsync(other, cancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 记下提供商变了
    /// </summary>
    /// <remarks>
    /// 调用成功、工作单元提交之后重载 Provider 目录，新的调用即用新配置
    /// </remarks>
    public void Changed() => changes.Mark();
}
