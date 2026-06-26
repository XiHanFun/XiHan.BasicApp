#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:StorageConfigCommandModels
// Guid:c5e9a2b7-4d18-4f6c-9b3a-1e8f7d2c6a59
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 存储配置创建命令
/// </summary>
/// <remarks>BucketName 在 Local 类型时存放本地根路径</remarks>
public sealed record StorageConfigCreateCommand(
    string ConfigCode,
    string ConfigName,
    StorageConfigType StorageType,
    string? Endpoint,
    string? Region,
    string? BucketName,
    string? AccessKeyId,
    string? SecretAccessKey,
    bool IsDefault,
    bool IsEnabled,
    int Sort,
    string? Remark);

/// <summary>
/// 存储配置更新命令
/// </summary>
/// <remarks>SecretAccessKey 为空表示保留原密钥；BucketName 在 Local 类型时存放本地根路径</remarks>
public sealed record StorageConfigUpdateCommand(
    long BasicId,
    string ConfigName,
    StorageConfigType StorageType,
    string? Endpoint,
    string? Region,
    string? BucketName,
    string? AccessKeyId,
    string? SecretAccessKey,
    int Sort,
    string? Remark);

/// <summary>
/// 存储配置状态变更命令
/// </summary>
public sealed record StorageConfigStatusChangeCommand(long BasicId, bool IsEnabled);

/// <summary>
/// 存储配置默认变更命令
/// </summary>
public sealed record StorageConfigDefaultChangeCommand(long BasicId);

/// <summary>
/// 存储配置命令结果
/// </summary>
public sealed record StorageConfigCommandResult(SysStorageConfig Config);
