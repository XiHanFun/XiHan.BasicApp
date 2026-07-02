#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsConfigDomainService
// Guid:4d7f1b92-8e35-4a60-9c2d-5b8f3e6a1c47
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 短信网关配置领域服务实现
/// </summary>
public sealed class SmsConfigDomainService
    : ISmsConfigDomainService
{
    private readonly ISmsConfigRepository _smsConfigRepository;

    private readonly ISmsConfigSecretProtector _secretProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SmsConfigDomainService(
        ISmsConfigRepository smsConfigRepository,
        ISmsConfigSecretProtector secretProtector)
    {
        _smsConfigRepository = smsConfigRepository;
        _secretProtector = secretProtector;
    }

    /// <inheritdoc />
    public async Task<SmsConfigCommandResult> CreateSmsConfigAsync(SmsConfigCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);
        var configCode = command.ConfigCode.Trim();
        if (await _smsConfigRepository.AnyAsync(config => config.ConfigCode == configCode, cancellationToken))
        {
            throw new InvalidOperationException("短信网关配置编码已存在。");
        }

        if (command.IsDefault)
        {
            EnsureEnabledDefault(command.IsEnabled);
            await ClearDefaultConfigsAsync(null, cancellationToken);
        }

        var config = new SysSmsConfig
        {
            ConfigCode = configCode,
            ConfigName = command.ConfigName.Trim(),
            Provider = command.Provider,
            AccessKeyId = command.AccessKeyId.Trim(),
            AccessKeySecret = _secretProtector.Protect(command.AccessKeySecret.Trim())!,
            SdkAppId = NormalizeNullable(command.SdkAppId),
            SignName = command.SignName.Trim(),
            Region = NormalizeNullable(command.Region),
            TemplateMap = NormalizeNullable(command.TemplateMap),
            IsDefault = command.IsDefault,
            IsEnabled = command.IsEnabled,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        return new SmsConfigCommandResult(await _smsConfigRepository.AddAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<SmsConfigCommandResult> UpdateSmsConfigAsync(SmsConfigUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);
        var config = await GetSmsConfigOrThrowAsync(command.BasicId, cancellationToken);

        config.ConfigName = command.ConfigName.Trim();
        config.Provider = command.Provider;
        config.AccessKeyId = command.AccessKeyId.Trim();
        config.SdkAppId = NormalizeNullable(command.SdkAppId);
        config.SignName = command.SignName.Trim();
        config.Region = NormalizeNullable(command.Region);
        config.TemplateMap = NormalizeNullable(command.TemplateMap);
        config.Sort = command.Sort;
        config.Remark = NormalizeNullable(command.Remark);

        // 密钥为空表示保留原密钥（前端脱敏不回显）；提供则加密落库
        var accessKeySecret = NormalizeNullable(command.AccessKeySecret);
        if (accessKeySecret is not null)
        {
            config.AccessKeySecret = _secretProtector.Protect(accessKeySecret)!;
        }

        return new SmsConfigCommandResult(await _smsConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<SmsConfigCommandResult> UpdateSmsConfigStatusAsync(SmsConfigStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "短信网关配置主键必须大于 0。");
        var config = await GetSmsConfigOrThrowAsync(command.BasicId, cancellationToken);
        if (config.IsDefault && !command.IsEnabled)
        {
            throw new InvalidOperationException("默认短信网关配置不能停用，请先将其他启用配置设为默认。");
        }

        config.IsEnabled = command.IsEnabled;
        return new SmsConfigCommandResult(await _smsConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<SmsConfigCommandResult> SetDefaultSmsConfigAsync(SmsConfigDefaultChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "短信网关配置主键必须大于 0。");
        var config = await GetSmsConfigOrThrowAsync(command.BasicId, cancellationToken);
        EnsureEnabledDefault(config.IsEnabled);

        await ClearDefaultConfigsAsync(config.BasicId, cancellationToken);
        config.IsDefault = true;

        return new SmsConfigCommandResult(await _smsConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<SmsConfigCommandResult> DeleteSmsConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = await GetSmsConfigOrThrowAsync(id, cancellationToken);
        if (config.IsDefault)
        {
            throw new InvalidOperationException("默认短信网关配置不能删除，请先将其他配置设为默认。");
        }

        if (!await _smsConfigRepository.DeleteAsync(config, cancellationToken))
        {
            throw new InvalidOperationException("短信网关配置删除失败。");
        }

        return new SmsConfigCommandResult(config);
    }

    private static void EnsureEnabledDefault(bool isEnabled)
    {
        if (!isEnabled)
        {
            throw new InvalidOperationException("默认短信网关配置必须处于启用状态。");
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

    private static void ValidateCommonInput(SmsProviderType provider, string? sdkAppId, string? region, string? templateMap, int sort)
    {
        ValidateEnum(provider, nameof(provider));
        if (sort < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sort), "排序不能小于 0。");
        }

        if (provider == SmsProviderType.TencentCloud)
        {
            if (string.IsNullOrWhiteSpace(sdkAppId))
            {
                throw new InvalidOperationException("腾讯云短信配置必须填写应用ID（SmsSdkAppId）。");
            }

            if (string.IsNullOrWhiteSpace(region))
            {
                throw new InvalidOperationException("腾讯云短信配置必须填写地域（如 ap-guangzhou）。");
            }
        }

        ValidateTemplateMap(templateMap);
    }

    private static void ValidateTemplateMap(string? templateMap)
    {
        if (string.IsNullOrWhiteSpace(templateMap))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(templateMap);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("模板映射必须是 JSON 对象。");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"模板映射不是合法 JSON：{ex.Message}");
        }
    }

    private static void ValidateCreateCommand(SmsConfigCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.AccessKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.AccessKeySecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SignName);
        ValidateCommonInput(command.Provider, command.SdkAppId, command.Region, command.TemplateMap, command.Sort);
    }

    private static void ValidateUpdateCommand(SmsConfigUpdateCommand command)
    {
        EnsureId(command.BasicId, "短信网关配置主键必须大于 0。");
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.AccessKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SignName);
        ValidateCommonInput(command.Provider, command.SdkAppId, command.Region, command.TemplateMap, command.Sort);
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private Task<bool> ClearDefaultConfigsAsync(long? excludeId, CancellationToken cancellationToken)
    {
        return excludeId.HasValue
            ? _smsConfigRepository.UpdateAsync(
                config => new SysSmsConfig { IsDefault = false },
                config => config.IsDefault && config.BasicId != excludeId.Value,
                cancellationToken)
            : _smsConfigRepository.UpdateAsync(
                config => new SysSmsConfig { IsDefault = false },
                config => config.IsDefault,
                cancellationToken);
    }

    private async Task<SysSmsConfig> GetSmsConfigOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "短信网关配置主键必须大于 0。");
        return await _smsConfigRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("短信网关配置不存在。");
    }
}
