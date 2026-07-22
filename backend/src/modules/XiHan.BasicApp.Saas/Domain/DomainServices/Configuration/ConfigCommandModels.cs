// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 参数配置创建命令
/// </summary>
public sealed record ConfigCreateCommand(
    bool IsGlobal,
    string ConfigName,
    string? ConfigGroup,
    string ConfigKey,
    string? ConfigValue,
    string? DefaultValue,
    ConfigType ConfigType,
    ConfigDataType DataType,
    string? ConfigDescription,
    bool IsEncrypted,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 参数配置更新命令
/// </summary>
public sealed record ConfigUpdateCommand(
    long BasicId,
    string ConfigName,
    string? ConfigGroup,
    string? ConfigValue,
    string? DefaultValue,
    ConfigType ConfigType,
    ConfigDataType DataType,
    string? ConfigDescription,
    bool IsEncrypted,
    int Sort,
    string? Remark);

/// <summary>
/// 参数配置状态变更命令
/// </summary>
public sealed record ConfigStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 参数配置命令结果
/// </summary>
public sealed record ConfigCommandResult(SysConfig Config);
