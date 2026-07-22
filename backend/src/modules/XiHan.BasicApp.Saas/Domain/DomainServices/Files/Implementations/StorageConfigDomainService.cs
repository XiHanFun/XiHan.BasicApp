// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 存储配置领域服务实现
/// </summary>
public sealed class StorageConfigDomainService
    : IStorageConfigDomainService
{
    private readonly IFileStorageRepository _fileStorageRepository;

    private readonly IStorageConfigRepository _storageConfigRepository;

    private readonly IStorageSecretProtector _secretProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StorageConfigDomainService(
        IStorageConfigRepository storageConfigRepository,
        IFileStorageRepository fileStorageRepository,
        IStorageSecretProtector secretProtector)
    {
        _storageConfigRepository = storageConfigRepository;
        _fileStorageRepository = fileStorageRepository;
        _secretProtector = secretProtector;
    }

    /// <inheritdoc />
    public async Task<StorageConfigCommandResult> CreateStorageConfigAsync(StorageConfigCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);
        var configCode = command.ConfigCode.Trim();
        if (await _storageConfigRepository.AnyAsync(config => config.ConfigCode == configCode, cancellationToken))
        {
            throw new InvalidOperationException("存储配置编码已存在。");
        }

        if (command.IsDefault)
        {
            EnsureEnabledDefault(command.IsEnabled);
            await ClearDefaultConfigsAsync(null, cancellationToken);
        }

        var config = new SysStorageConfig
        {
            ConfigCode = configCode,
            ConfigName = command.ConfigName.Trim(),
            StorageType = command.StorageType,
            Endpoint = NormalizeNullable(command.Endpoint),
            Region = NormalizeNullable(command.Region),
            BucketName = NormalizeNullable(command.BucketName),
            AccessKeyId = NormalizeNullable(command.AccessKeyId),
            SecretAccessKey = _secretProtector.Protect(NormalizeNullable(command.SecretAccessKey)),
            IsDefault = command.IsDefault,
            IsEnabled = command.IsEnabled,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        return new StorageConfigCommandResult(await _storageConfigRepository.AddAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<StorageConfigCommandResult> UpdateStorageConfigAsync(StorageConfigUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);
        var config = await GetStorageConfigOrThrowAsync(command.BasicId, cancellationToken);

        config.ConfigName = command.ConfigName.Trim();
        config.StorageType = command.StorageType;
        config.Endpoint = NormalizeNullable(command.Endpoint);
        config.Region = NormalizeNullable(command.Region);
        config.BucketName = NormalizeNullable(command.BucketName);
        config.AccessKeyId = NormalizeNullable(command.AccessKeyId);
        config.Sort = command.Sort;
        config.Remark = NormalizeNullable(command.Remark);

        // 密钥为空表示保留原密钥（前端脱敏不回显）；提供则加密落库
        var secretAccessKey = NormalizeNullable(command.SecretAccessKey);
        if (secretAccessKey is not null)
        {
            config.SecretAccessKey = _secretProtector.Protect(secretAccessKey);
        }

        return new StorageConfigCommandResult(await _storageConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<StorageConfigCommandResult> UpdateStorageConfigStatusAsync(StorageConfigStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "存储配置主键必须大于 0。");
        var config = await GetStorageConfigOrThrowAsync(command.BasicId, cancellationToken);
        if (config.IsDefault && !command.IsEnabled)
        {
            throw new InvalidOperationException("默认存储配置不能停用，请先将其他启用配置设为默认。");
        }

        config.IsEnabled = command.IsEnabled;
        return new StorageConfigCommandResult(await _storageConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<StorageConfigCommandResult> SetDefaultStorageConfigAsync(StorageConfigDefaultChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "存储配置主键必须大于 0。");
        var config = await GetStorageConfigOrThrowAsync(command.BasicId, cancellationToken);
        EnsureEnabledDefault(config.IsEnabled);

        await ClearDefaultConfigsAsync(config.BasicId, cancellationToken);
        config.IsDefault = true;

        return new StorageConfigCommandResult(await _storageConfigRepository.UpdateAsync(config, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<StorageConfigCommandResult> DeleteStorageConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = await GetStorageConfigOrThrowAsync(id, cancellationToken);
        if (config.IsDefault)
        {
            throw new InvalidOperationException("默认存储配置不能删除，请先将其他配置设为默认。");
        }

        if (await _fileStorageRepository.AnyAsync(storage => storage.StorageConfigId == id, cancellationToken))
        {
            throw new InvalidOperationException("存储配置已被文件存储记录引用，禁止删除。");
        }

        if (!await _storageConfigRepository.DeleteAsync(config, cancellationToken))
        {
            throw new InvalidOperationException("存储配置删除失败。");
        }

        return new StorageConfigCommandResult(config);
    }

    private static void EnsureEnabledDefault(bool isEnabled)
    {
        if (!isEnabled)
        {
            throw new InvalidOperationException("默认存储配置必须处于启用状态。");
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

    private static void ValidateCommonInput(StorageConfigType storageType, string? bucketName, string? accessKeyId, int sort)
    {
        ValidateEnum(storageType, nameof(storageType));
        if (sort < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sort), "排序不能小于 0。");
        }

        if (storageType == StorageConfigType.Local)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException("对象存储配置必须填写存储桶名称。");
        }

        if (string.IsNullOrWhiteSpace(accessKeyId))
        {
            throw new InvalidOperationException("对象存储配置必须填写访问密钥ID。");
        }
    }

    private static void ValidateCreateCommand(StorageConfigCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigName);
        ValidateCommonInput(command.StorageType, command.BucketName, command.AccessKeyId, command.Sort);
        if (command.StorageType != StorageConfigType.Local && string.IsNullOrWhiteSpace(command.SecretAccessKey))
        {
            throw new InvalidOperationException("对象存储配置必须填写访问密钥。");
        }
    }

    private static void ValidateUpdateCommand(StorageConfigUpdateCommand command)
    {
        EnsureId(command.BasicId, "存储配置主键必须大于 0。");
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfigName);
        ValidateCommonInput(command.StorageType, command.BucketName, command.AccessKeyId, command.Sort);
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
            ? _storageConfigRepository.UpdateAsync(
                config => new SysStorageConfig { IsDefault = false },
                config => config.IsDefault && config.BasicId != excludeId.Value,
                cancellationToken)
            : _storageConfigRepository.UpdateAsync(
                config => new SysStorageConfig { IsDefault = false },
                config => config.IsDefault,
                cancellationToken);
    }

    private async Task<SysStorageConfig> GetStorageConfigOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "存储配置主键必须大于 0。");
        return await _storageConfigRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("存储配置不存在。");
    }
}
