// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Domain.Services;

/// <summary>
/// 机器人配置内容（签名密钥为空即保留原值）。
/// </summary>
/// <param name="Name">名称。</param>
/// <param name="Provider">服务商。</param>
/// <param name="WebhookUrl">Webhook 地址。</param>
/// <param name="Secret">签名密钥。</param>
/// <param name="Keyword">安全关键词。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record BotConfigDraft(string Name, BotProvider Provider, string WebhookUrl, string? Secret, string? Keyword, bool IsEnabled, int Order, string? Remark);

/// <summary>
/// 机器人配置的规则。
/// </summary>
/// <remarks>
/// 编码在同一侧唯一、建好不改；每侧每个服务商至多一个缺省，这个服务商的第一个自动成为缺省；缺省的须启用，不能停用或删除；
/// Webhook 地址只认各服务商的官方 https 地址（租户能填的地址不许指向别处）；发送只用本侧的，不借平台的机器人。签名密钥加密存储。
/// </remarks>
/// <param name="bots">配置仓储。</param>
/// <param name="secrets">密钥保护。</param>
/// <param name="ids">标识生成器。</param>
public sealed class BotConfigService(IRepository<SysBotConfig, Guid> bots, ISecretProtector secrets, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 签名密钥的加密用途。
    /// </summary>
    public const string SecretPurpose = "XiHan.BasicApp.Notification.BotSecret";

    /// <summary>
    /// 各服务商的官方 Webhook 主机。
    /// </summary>
    private static readonly Dictionary<BotProvider, string[]> _hosts = new()
    {
        [BotProvider.DingTalk] = ["oapi.dingtalk.com"],
        [BotProvider.Lark] = ["open.feishu.cn", "open.larksuite.com"],
        [BotProvider.WeCom] = ["qyapi.weixin.qq.com"],
    };

    /// <summary>
    /// 取当前一侧的机器人配置。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="id">配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>配置；不存在即 <c>NOTIFICATION_NOTFOUND_008</c>。</returns>
    public async Task<Result<SysBotConfig>> FindAsync(string owner, Guid id, CancellationToken cancellationToken)
    {
        var found = await bots.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var config) && string.Equals(config.OwnerTenantId, owner, StringComparison.Ordinal)
            ? config
            : Error.NotFound(NotificationErrors.BotConfigNotFound, NotificationMessages.BotConfigNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 本侧每个服务商启用的缺省机器人（通知的机器人渠道发到这些）。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按服务商排好的缺省机器人；一个都没有为空。</returns>
    public async Task<IReadOnlyList<SysBotConfig>> DefaultsAsync(string owner, CancellationToken cancellationToken)
    {
        var rows = await bots.ListAsync(EntityQuery.All<SysBotConfig>().Where(new Specification<SysBotConfig>(c => c.OwnerTenantId == owner && c.IsDefault && c.IsEnabled)), cancellationToken).ConfigureAwait(false);
        return rows.GroupBy(static c => c.Provider).OrderBy(static g => g.Key).Select(static g => g.First()).ToList();
    }

    /// <summary>
    /// 新建（本侧这个服务商还没有缺省即成为缺省）。
    /// </summary>
    /// <param name="owner">侧。</param>
    /// <param name="code">编码。</param>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新配置。</returns>
    public async Task<Result<SysBotConfig>> CreateAsync(string owner, string code, BotConfigDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var valid = Validate(draft);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        var existing = await bots.ListAsync(EntityQuery.All<SysBotConfig>().Where(new Specification<SysBotConfig>(c => c.OwnerTenantId == owner)), cancellationToken).ConfigureAwait(false);
        if (existing.Any(c => string.Equals(c.Code, code, StringComparison.Ordinal)))
        {
            return Error.Conflict(NotificationErrors.ConfigCodeTaken, NotificationMessages.ConfigCodeTaken, MessageArguments.Of(("code", code)));
        }

        var config = new SysBotConfig { Id = ids.NewId(), OwnerTenantId = owner, Code = code, IsDefault = !existing.Any(c => c.Provider == draft.Provider && c.IsDefault) };
        Apply(config, draft);
        config.IsEnabled = config.IsEnabled || config.IsDefault;
        var added = await bots.AddAsync(config, cancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : config;
    }

    /// <summary>
    /// 修改机器人配置。
    /// </summary>
    /// <remarks>
    /// 编码不改；缺省的不能停用，也不能换服务商。
    /// </remarks>
    /// <param name="config">配置。</param>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> UpdateAsync(SysBotConfig config, BotConfigDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(draft);
        var valid = Validate(draft);
        if (valid.IsFailure)
        {
            return valid;
        }

        if (config.IsDefault && (!draft.IsEnabled || draft.Provider != config.Provider))
        {
            return DefaultLocked(config.Code);
        }

        Apply(config, draft);
        return await bots.UpdateAsync(config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 设为本侧这个服务商的缺省机器人（须启用）。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> SetDefaultAsync(SysBotConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (!config.IsEnabled)
        {
            return DefaultLocked(config.Code);
        }

        var owner = config.OwnerTenantId;
        var provider = config.Provider;
        var id = config.Id;
        var others = await bots.ListAsync(EntityQuery.All<SysBotConfig>().Where(new Specification<SysBotConfig>(c => c.OwnerTenantId == owner && c.Provider == provider && c.IsDefault && c.Id != id)), cancellationToken).ConfigureAwait(false);
        foreach (var other in others)
        {
            other.IsDefault = false;
        }

        config.IsDefault = true;
        foreach (var outcome in await bots.UpdateRangeAsync([.. others, config], cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 批量删除机器人配置。
    /// </summary>
    /// <remarks>
    /// 缺省的逐项拒绝。
    /// </remarks>
    /// <param name="owner">侧。</param>
    /// <param name="configIds">配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> DeleteAsync(string owner, IReadOnlyList<Guid> configIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configIds);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in configIds.Distinct())
        {
            var found = await FindAsync(owner, id, cancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var config))
            {
                failed.Add(new BatchFailure(id, NotificationErrors.BotConfigNotFound));
            }
            else if (config.IsDefault)
            {
                failed.Add(new BatchFailure(id, NotificationErrors.DefaultConfigLocked));
            }
            else if ((await bots.RemoveAsync(config, cancellationToken).ConfigureAwait(false)) is { IsFailure: true } removed)
            {
                failed.Add(new BatchFailure(id, removed.Error!.Code));
            }
            else
            {
                succeeded.Add(id);
            }
        }

        return new BatchResult(succeeded, failed);
    }

    /// <summary>
    /// 发送用的机器人（签名密钥解密）。
    /// </summary>
    /// <remarks>
    /// 签名密钥解不开按不签名处理，由服务商拒绝。
    /// </remarks>
    /// <param name="config">配置。</param>
    /// <returns>机器人。</returns>
    public BotEndpoint EndpointOf(SysBotConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var secret = config.Secret is not null && secrets.TryUnprotect(SecretPurpose, config.Secret, out var plain) ? plain : null;
        return new BotEndpoint((BotChannel)(int)config.Provider, config.WebhookUrl, secret, config.Keyword);
    }

    private static Result Validate(BotConfigDraft draft)
        => Uri.TryCreate(draft.WebhookUrl.Trim(), UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
            && uri.IsDefaultPort
            && _hosts.TryGetValue(draft.Provider, out var hosts)
            && hosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase)
            ? Result.Success()
            : Error.Validation(NotificationErrors.BotWebhookInvalid, NotificationMessages.BotWebhookInvalid, MessageArguments.Of(("url", draft.WebhookUrl)));

    private static Error DefaultLocked(string code) => Error.Conflict(NotificationErrors.DefaultConfigLocked, NotificationMessages.DefaultConfigLocked, MessageArguments.Of(("code", code)));

    private void Apply(SysBotConfig config, BotConfigDraft draft)
    {
        config.Name = draft.Name.Trim();
        config.Provider = draft.Provider;
        config.WebhookUrl = draft.WebhookUrl.Trim();
        if (!string.IsNullOrEmpty(draft.Secret))
        {
            config.Secret = secrets.Protect(SecretPurpose, draft.Secret);
        }

        config.Keyword = string.IsNullOrWhiteSpace(draft.Keyword) ? null : draft.Keyword.Trim();
        config.IsEnabled = draft.IsEnabled;
        config.Order = draft.Order;
        config.Remark = string.IsNullOrWhiteSpace(draft.Remark) ? null : draft.Remark.Trim();
    }
}
