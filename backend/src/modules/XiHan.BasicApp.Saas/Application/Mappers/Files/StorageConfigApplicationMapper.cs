// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 存储配置应用层映射器
/// </summary>
public static class StorageConfigApplicationMapper
{
    /// <summary>
    /// 映射存储配置创建命令
    /// </summary>
    public static StorageConfigCreateCommand ToCreateCommand(StorageConfigCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new StorageConfigCreateCommand(
            input.ConfigCode,
            input.ConfigName,
            input.StorageType,
            input.Endpoint,
            input.Region,
            input.BucketName,
            input.AccessKeyId,
            input.SecretAccessKey,
            input.IsDefault,
            input.IsEnabled,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射存储配置更新命令
    /// </summary>
    public static StorageConfigUpdateCommand ToUpdateCommand(StorageConfigUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new StorageConfigUpdateCommand(
            input.BasicId,
            input.ConfigName,
            input.StorageType,
            input.Endpoint,
            input.Region,
            input.BucketName,
            input.AccessKeyId,
            input.SecretAccessKey,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射存储配置状态命令
    /// </summary>
    public static StorageConfigStatusChangeCommand ToStatusCommand(StorageConfigStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new StorageConfigStatusChangeCommand(input.BasicId, input.IsEnabled);
    }

    /// <summary>
    /// 映射存储配置默认变更命令
    /// </summary>
    public static StorageConfigDefaultChangeCommand ToDefaultCommand(StorageConfigDefaultUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new StorageConfigDefaultChangeCommand(input.BasicId);
    }

    /// <summary>
    /// 映射存储配置列表项
    /// </summary>
    /// <param name="config">存储配置</param>
    /// <returns>存储配置列表项 DTO</returns>
    public static StorageConfigListItemDto ToListItemDto(SysStorageConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new StorageConfigListItemDto
        {
            BasicId = config.BasicId,
            ConfigCode = config.ConfigCode,
            ConfigName = config.ConfigName,
            StorageType = config.StorageType,
            Endpoint = config.Endpoint,
            Region = config.Region,
            BucketName = config.BucketName,
            IsDefault = config.IsDefault,
            IsEnabled = config.IsEnabled,
            Sort = config.Sort,
            CreatedTime = config.CreatedTime,
            ModifiedTime = config.ModifiedTime
        };
    }

    /// <summary>
    /// 映射存储配置详情（SecretAccessKey 脱敏，仅标识是否已配置）
    /// </summary>
    /// <param name="config">存储配置</param>
    /// <returns>存储配置详情 DTO</returns>
    public static StorageConfigDetailDto ToDetailDto(SysStorageConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new StorageConfigDetailDto
        {
            BasicId = config.BasicId,
            ConfigCode = config.ConfigCode,
            ConfigName = config.ConfigName,
            StorageType = config.StorageType,
            Endpoint = config.Endpoint,
            Region = config.Region,
            BucketName = config.BucketName,
            AccessKeyId = config.AccessKeyId,
            HasSecretAccessKey = !string.IsNullOrWhiteSpace(config.SecretAccessKey),
            IsDefault = config.IsDefault,
            IsEnabled = config.IsEnabled,
            Sort = config.Sort,
            Remark = config.Remark,
            CreatedTime = config.CreatedTime,
            CreatedId = config.CreatedId,
            CreatedBy = config.CreatedBy,
            ModifiedTime = config.ModifiedTime,
            ModifiedId = config.ModifiedId,
            ModifiedBy = config.ModifiedBy
        };
    }
}
