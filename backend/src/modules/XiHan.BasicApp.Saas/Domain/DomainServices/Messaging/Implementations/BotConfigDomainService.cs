// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 机器人配置领域服务实现（Webhook 型：钉钉/飞书/企业微信）
/// </summary>
/// <remarks>
/// 默认互斥按 Provider 维度：同一租户同一服务商下有且仅有一条 IsDefault=true
/// （注意与短信/邮件配置的全表互斥不同）。
/// </remarks>
public sealed class BotConfigDomainService
    : IBotConfigDomainService
{
    private readonly IBotConfigRepository _botConfigRepository;

    private readonly IBotConfigSecretProtector _secretProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public BotConfigDomainService(
        IBotConfigRepository botConfigRepository,
        IBotConfigSecretProtector secretProtector)
    {
        _botConfigRepository = botConfigRepository;
        _secretProtector = secretProtector;
    }

    /// <inheritdoc />
    public async Task<BotConfigCommandResult> CreateBotConfigAsync(BotConfigCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);
        var configCode = command.ConfigCode.Trim();
        if (await _botConfigRepository.AnyAsync(config => config.ConfigCode == configCode, cancellationToken))
        {
            throw new InvalidOperationException("机器人配置编码已存在。");
        }

        if (command.IsDefault)
        {
            EnsureEnabledDefault(command.IsEnabled);
            await ClearDefaultConfigsAsync(command.Provider, null, cancellationToken);
        }

        var config = new SysBotConfig
        {
            ConfigCode = configCode,
            ConfigName = command.ConfigName.Trim(),
            Provider = command.Provider,
            WebhookUrl = command.WebhookUrl.Trim(),
            Secret = _secretProtector.Protect(NormalizeNullable(command.Secret)),
            Keyword = NormalizeNullable(command.Keyword),
            IsDefault = command.IsDefault,
            IsEnabled = command.IsEnabled,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        return new BotConfigCommandResult(await _botConfigRepository.AddAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<BotConfigCommandResult> UpdateBotConfigAsync(BotConfigUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);
        var config = await GetBotConfigOrThrowAsync(command.BasicId, cancellationToken);

        // 默认行变更服务商时，须在目标服务商分组内重新保证互斥（否则目标分组可能出现两条默认行）
        if (config.IsDefault && config.Provider != command.Provider)
        {
            await ClearDefaultConfigsAsync(command.Provider, config.BasicId, cancellationToken);
        }

        config.ConfigName = command.ConfigName.Trim();
        config.Provider = command.Provider;
        config.WebhookUrl = command.WebhookUrl.Trim();
        config.Keyword = NormalizeNullable(command.Keyword);
        config.Sort = command.Sort;
        config.Remark = NormalizeNullable(command.Remark);

        // 秘钥为空表示保留原秘钥（前端脱敏不回显）；提供则加密落库
        var secret = NormalizeNullable(command.Secret);
        if (secret is not null)
        {
            config.Secret = _secretProtector.Protect(secret);
        }

        return new BotConfigCommandResult(await _botConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<BotConfigCommandResult> UpdateBotConfigStatusAsync(BotConfigStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "机器人配置主键必须大于 0。");
        var config = await GetBotConfigOrThrowAsync(command.BasicId, cancellationToken);
        if (config.IsDefault && !command.IsEnabled)
        {
            throw new InvalidOperationException("默认机器人配置不能停用，请先将同服务商其他启用配置设为默认。");
        }

        config.IsEnabled = command.IsEnabled;
        return new BotConfigCommandResult(await _botConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<BotConfigCommandResult> SetDefaultBotConfigAsync(BotConfigDefaultChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "机器人配置主键必须大于 0。");
        var config = await GetBotConfigOrThrowAsync(command.BasicId, cancellationToken);
        EnsureEnabledDefault(config.IsEnabled);

        await ClearDefaultConfigsAsync(config.Provider, config.BasicId, cancellationToken);
        config.IsDefault = true;

        return new BotConfigCommandResult(await _botConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<BotConfigCommandResult> DeleteBotConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = await GetBotConfigOrThrowAsync(id, cancellationToken);
        if (config.IsDefault)
        {
            throw new InvalidOperationException("默认机器人配置不能删除，请先将同服务商其他配置设为默认。");
        }

        if (!await _botConfigRepository.DeleteAsync(config, cancellationToken))
        {
            throw new InvalidOperationException("机器人配置删除失败。");
        }

        return new BotConfigCommandResult(config);
    }

    private static void EnsureEnabledDefault(bool isEnabled)
    {
        if (!isEnabled)
        {
            throw new InvalidOperationException("默认机器人配置必须处于启用状态。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateCommonInput(BotProviderType provider, string webhookUrl, int sort)
    {
        ValidateEnum(provider, nameof(provider));
        if (sort < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sort), "排序不能小于 0。");
        }

        ValidateWebhookUrl(webhookUrl);
    }

    private static void ValidateWebhookUrl(string webhookUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookUrl);
        if (!Uri.TryCreate(webhookUrl.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException("Webhook 地址必须是合法的 http/https 绝对地址。");
        }
    }

    private static void ValidateCreateCommand(BotConfigCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigName);
        ValidateCommonInput(command.Provider, command.WebhookUrl, command.Sort);
    }

    private static void ValidateUpdateCommand(BotConfigUpdateCommand command)
    {
        EnsureId(command.BasicId, "机器人配置主键必须大于 0。");
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigName);
        ValidateCommonInput(command.Provider, command.WebhookUrl, command.Sort);
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private Task<bool> ClearDefaultConfigsAsync(BotProviderType provider, long? excludeId, CancellationToken cancellationToken)
    {
        return excludeId.HasValue
            ? _botConfigRepository.UpdateAsync(
                config => new SysBotConfig { IsDefault = false },
                config => config.Provider == provider && config.IsDefault && config.BasicId != excludeId.Value,
                cancellationToken)
            : _botConfigRepository.UpdateAsync(
                config => new SysBotConfig { IsDefault = false },
                config => config.Provider == provider && config.IsDefault,
                cancellationToken);
    }

    private async Task<SysBotConfig> GetBotConfigOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "机器人配置主键必须大于 0。");
        return await _botConfigRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("机器人配置不存在。");
    }
}
