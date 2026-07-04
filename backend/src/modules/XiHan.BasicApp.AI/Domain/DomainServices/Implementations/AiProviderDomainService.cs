#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiProviderDomainService
// Guid:a11c0de0-1007-4a10-9a00-00000000ai16
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Diagnostics;
using Microsoft.Extensions.AI;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.Framework.AI.Abstractions.Configuration;
using XiHan.Framework.AI.Providers;

namespace XiHan.BasicApp.AI.Domain.DomainServices.Implementations;

/// <summary>
/// AI Provider 领域服务实现
/// </summary>
public sealed class AiProviderDomainService : IAiProviderDomainService
{
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiProviderSecretProtector _secretProtector;
    private readonly OpenAiCompatibleChatClientFactory _chatClientFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiProviderDomainService(
        IAiProviderRepository providerRepository,
        IAiProviderSecretProtector secretProtector,
        OpenAiCompatibleChatClientFactory chatClientFactory)
    {
        _providerRepository = providerRepository;
        _secretProtector = secretProtector;
        _chatClientFactory = chatClientFactory;
    }

    /// <inheritdoc />
    public async Task<AiProviderCommandResult> CreateProviderAsync(AiProviderCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureEnum(command.Status, nameof(command.Status));
        EnsureTemperature(command.Temperature);

        var configCode = Required(command.ConfigCode, 100, nameof(command.ConfigCode), "配置编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(configCode, "配置编码不能包含空白字符。");
        if (await _providerRepository.ExistsCodeAsync(configCode, null, cancellationToken))
        {
            throw new InvalidOperationException("配置编码已存在。");
        }

        var provider = new SysAiProvider
        {
            ConfigCode = configCode,
            ConfigName = Required(command.ConfigName, 200, nameof(command.ConfigName), "配置名称不能超过 200 个字符。"),
            Provider = Required(command.Provider, 50, nameof(command.Provider), "提供商标识不能超过 50 个字符。"),
            Model = Required(command.Model, 100, nameof(command.Model), "模型名称不能超过 100 个字符。"),
            BaseUrl = Optional(command.BaseUrl, 500, nameof(command.BaseUrl), "端点地址不能超过 500 个字符。"),
            ApiKey = _secretProtector.Protect(NormalizeNullable(command.ApiKey)),
            MaxOutputTokens = command.MaxOutputTokens,
            Temperature = command.Temperature,
            TimeoutSeconds = command.TimeoutSeconds,
            ExtraJson = NormalizeNullable(command.ExtraJson),
            IsDefault = command.IsDefault,
            IsEnabled = command.IsEnabled,
            Sort = command.Sort,
            Status = command.Status,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        var created = await _providerRepository.AddAsync(provider, cancellationToken);
        if (created.IsDefault)
        {
            await ClearOtherDefaultsAsync(created.BasicId, cancellationToken);
        }

        return new AiProviderCommandResult(created);
    }

    /// <inheritdoc />
    public async Task<AiProviderCommandResult> UpdateProviderAsync(AiProviderUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "provider 主键必须大于 0。");
        EnsureTemperature(command.Temperature);

        var provider = await GetProviderOrThrowAsync(command.BasicId, cancellationToken);

        // 配置编码不可变，故此处不更新 ConfigCode。
        provider.ConfigName = Required(command.ConfigName, 200, nameof(command.ConfigName), "配置名称不能超过 200 个字符。");
        provider.Provider = Required(command.Provider, 50, nameof(command.Provider), "提供商标识不能超过 50 个字符。");
        provider.Model = Required(command.Model, 100, nameof(command.Model), "模型名称不能超过 100 个字符。");
        provider.BaseUrl = Optional(command.BaseUrl, 500, nameof(command.BaseUrl), "端点地址不能超过 500 个字符。");

        // ApiKey 仅在传入新值时重新加密；为空表示保留原密钥。
        var newApiKey = NormalizeNullable(command.ApiKey);
        if (newApiKey is not null)
        {
            provider.ApiKey = _secretProtector.Protect(newApiKey);
        }

        provider.MaxOutputTokens = command.MaxOutputTokens;
        provider.Temperature = command.Temperature;
        provider.TimeoutSeconds = command.TimeoutSeconds;
        provider.ExtraJson = NormalizeNullable(command.ExtraJson);
        provider.IsDefault = command.IsDefault;
        provider.IsEnabled = command.IsEnabled;
        provider.Sort = command.Sort;
        provider.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        var updated = await _providerRepository.UpdateAsync(provider, cancellationToken);
        if (updated.IsDefault)
        {
            await ClearOtherDefaultsAsync(updated.BasicId, cancellationToken);
        }

        return new AiProviderCommandResult(updated);
    }

    /// <inheritdoc />
    public async Task<AiProviderCommandResult> UpdateProviderStatusAsync(AiProviderStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "provider 主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));

        var provider = await GetProviderOrThrowAsync(command.BasicId, cancellationToken);
        provider.Status = command.Status;
        provider.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? provider.Remark;

        return new AiProviderCommandResult(await _providerRepository.UpdateAsync(provider, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<AiProviderCommandResult> SetDefaultAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var provider = await GetProviderOrThrowAsync(id, cancellationToken);
        if (!provider.IsEnabled)
        {
            throw new InvalidOperationException("已禁用的 provider 不能设为默认。");
        }

        await ClearOtherDefaultsAsync(provider.BasicId, cancellationToken);
        if (!provider.IsDefault)
        {
            provider.IsDefault = true;
            provider = await _providerRepository.UpdateAsync(provider, cancellationToken);
        }

        return new AiProviderCommandResult(provider);
    }

    /// <inheritdoc />
    public async Task DeleteProviderAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var provider = await GetProviderOrThrowAsync(id, cancellationToken);
        if (!await _providerRepository.DeleteAsync(provider, cancellationToken))
        {
            throw new InvalidOperationException("provider 删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<AiProviderTestResult> TestConnectionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var provider = await GetProviderOrThrowAsync(id, cancellationToken);

        var options = new AiProviderOptions
        {
            Provider = provider.ConfigCode,
            ApiKey = _secretProtector.Unprotect(provider.ApiKey),
            BaseUrl = provider.BaseUrl,
            Model = provider.Model,
            MaxOutputTokens = provider.MaxOutputTokens,
            Temperature = provider.Temperature,
            TimeoutSeconds = provider.TimeoutSeconds,
            ExtraJson = provider.ExtraJson
        };

        // 用一次性客户端测试（不入解析器缓存），可测试尚未启用的配置。
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var client = _chatClientFactory.Create(options);
            try
            {
                var response = await client.GetResponseAsync(
                    [new ChatMessage(ChatRole.User, "ping")],
                    new ChatOptions { MaxOutputTokens = 16 },
                    cancellationToken);
                stopwatch.Stop();
                return new AiProviderTestResult(true, response.FinishReason?.ToString(), stopwatch.ElapsedMilliseconds, provider.Model);
            }
            finally
            {
                (client as IDisposable)?.Dispose();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new AiProviderTestResult(false, ex.Message, stopwatch.ElapsedMilliseconds, provider.Model);
        }
    }

    /// <summary>
    /// 清除除 keepId 外其它行的默认标记（单默认互斥）
    /// </summary>
    private async Task ClearOtherDefaultsAsync(long keepId, CancellationToken cancellationToken)
    {
        var others = await _providerRepository.GetOtherDefaultsAsync(keepId, cancellationToken);
        foreach (var other in others)
        {
            other.IsDefault = false;
            await _providerRepository.UpdateAsync(other, cancellationToken);
        }
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static void EnsureTemperature(float? temperature)
    {
        if (temperature is < 0f or > 2f)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature), "采样温度须在 0~2 之间。");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private async Task<SysAiProvider> GetProviderOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "provider 主键必须大于 0。");
        return await _providerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("provider 不存在。");
    }
}
