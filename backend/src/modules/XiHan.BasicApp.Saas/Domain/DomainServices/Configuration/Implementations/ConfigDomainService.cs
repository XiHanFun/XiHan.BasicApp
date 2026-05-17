#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigDomainService
// Guid:aef17379-638b-4a10-9fb9-2b8c2d9a47b1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 参数配置领域服务实现
/// </summary>
public sealed class ConfigDomainService
    : IConfigDomainService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigDomainService(IConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    private readonly IConfigRepository _configRepository;

    /// <inheritdoc />
    public async Task<ConfigCommandResult> CreateConfigAsync(ConfigCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateConfigCommand(command);
        var configKey = Required(command.ConfigKey, 100, nameof(command.ConfigKey), "配置键不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(configKey, "配置键不能包含空白字符。");
        if (await _configRepository.AnyAsync(config => config.ConfigKey == configKey, cancellationToken))
        {
            throw new InvalidOperationException("配置键已存在。");
        }

        var config = new SysConfig
        {
            IsGlobal = command.IsGlobal,
            ConfigName = Required(command.ConfigName, 100, nameof(command.ConfigName), "配置名称不能超过 100 个字符。"),
            ConfigGroup = Optional(command.ConfigGroup, 100, nameof(command.ConfigGroup), "配置分组不能超过 100 个字符。"),
            ConfigKey = configKey,
            ConfigValue = NormalizeNullable(command.ConfigValue),
            DefaultValue = NormalizeNullable(command.DefaultValue),
            ConfigType = command.ConfigType,
            DataType = command.DataType,
            ConfigDescription = Optional(command.ConfigDescription, 500, nameof(command.ConfigDescription), "配置描述不能超过 500 个字符。"),
            IsBuiltIn = false,
            IsEncrypted = command.IsEncrypted,
            Status = command.Status,
            Sort = command.Sort,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        return new ConfigCommandResult(await _configRepository.AddAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task DeleteConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = await GetConfigOrThrowAsync(id, cancellationToken);
        if (config.IsBuiltIn)
        {
            throw new InvalidOperationException("内置系统配置不能删除。");
        }

        if (!await _configRepository.DeleteAsync(config, cancellationToken))
        {
            throw new InvalidOperationException("系统配置删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<ConfigCommandResult> UpdateConfigAsync(ConfigUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateConfigCommand(command);
        var config = await GetConfigOrThrowAsync(command.BasicId, cancellationToken);
        config.ConfigName = Required(command.ConfigName, 100, nameof(command.ConfigName), "配置名称不能超过 100 个字符。");
        config.ConfigGroup = Optional(command.ConfigGroup, 100, nameof(command.ConfigGroup), "配置分组不能超过 100 个字符。");
        config.ConfigValue = NormalizeNullable(command.ConfigValue);
        config.DefaultValue = NormalizeNullable(command.DefaultValue);
        config.ConfigType = command.ConfigType;
        config.DataType = command.DataType;
        config.ConfigDescription = Optional(command.ConfigDescription, 500, nameof(command.ConfigDescription), "配置描述不能超过 500 个字符。");
        config.IsEncrypted = command.IsEncrypted;
        config.Sort = command.Sort;
        config.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new ConfigCommandResult(await _configRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ConfigCommandResult> UpdateConfigStatusAsync(ConfigStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统配置主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));

        var config = await GetConfigOrThrowAsync(command.BasicId, cancellationToken);
        config.Status = command.Status;
        config.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? config.Remark;

        return new ConfigCommandResult(await _configRepository.UpdateAsync(config, cancellationToken));
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void ValidateCreateConfigCommand(ConfigCreateCommand command)
    {
        EnsureEnum(command.ConfigType, nameof(command.ConfigType));
        EnsureEnum(command.DataType, nameof(command.DataType));
        EnsureEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateUpdateConfigCommand(ConfigUpdateCommand command)
    {
        EnsureId(command.BasicId, "系统配置主键必须大于 0。");
        EnsureEnum(command.ConfigType, nameof(command.ConfigType));
        EnsureEnum(command.DataType, nameof(command.DataType));
    }

    private async Task<SysConfig> GetConfigOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统配置主键必须大于 0。");
        return await _configRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统配置不存在。");
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
}
