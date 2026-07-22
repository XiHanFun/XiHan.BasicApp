// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统配置创建 DTO
/// </summary>
public sealed class ConfigCreateDto
{
    public bool IsGlobal { get; set; }
    public string ConfigName { get; set; } = string.Empty;
    public string? ConfigGroup { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string? ConfigValue { get; set; }
    public string? DefaultValue { get; set; }
    public ConfigType ConfigType { get; set; } = ConfigType.Feature;
    public ConfigDataType DataType { get; set; } = ConfigDataType.String;
    public string? ConfigDescription { get; set; }
    public bool IsEncrypted { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 系统配置更新 DTO
/// </summary>
public sealed class ConfigUpdateDto : BasicAppUDto
{
    public string ConfigName { get; set; } = string.Empty;
    public string? ConfigGroup { get; set; }
    public string? ConfigValue { get; set; }
    public string? DefaultValue { get; set; }
    public ConfigType ConfigType { get; set; } = ConfigType.Feature;
    public ConfigDataType DataType { get; set; } = ConfigDataType.String;
    public string? ConfigDescription { get; set; }
    public bool IsEncrypted { get; set; }
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 系统配置状态更新 DTO
/// </summary>
public sealed class ConfigStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// 系统字典创建 DTO
/// </summary>
public sealed class DictCreateDto
{
    public string DictCode { get; set; } = string.Empty;
    public string DictName { get; set; } = string.Empty;
    public string DictType { get; set; } = string.Empty;
    public string? DictDescription { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 系统字典更新 DTO
/// </summary>
public sealed class DictUpdateDto : BasicAppUDto
{
    public string DictName { get; set; } = string.Empty;
    public string DictType { get; set; } = string.Empty;
    public string? DictDescription { get; set; }
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 系统字典状态更新 DTO
/// </summary>
public sealed class DictStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// 系统字典项创建 DTO
/// </summary>
public sealed class DictItemCreateDto
{
    public long DictId { get; set; }
    public long? ParentId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? ItemValue { get; set; }
    public string? ItemDescription { get; set; }
    public string? Metadata { get; set; }
    public bool IsDefault { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 系统字典项更新 DTO
/// </summary>
public sealed class DictItemUpdateDto : BasicAppUDto
{
    public long? ParentId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemValue { get; set; }
    public string? ItemDescription { get; set; }
    public string? Metadata { get; set; }
    public bool IsDefault { get; set; }
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 系统字典项状态更新 DTO
/// </summary>
public sealed class DictItemStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// 系统版本创建 DTO
/// </summary>
public sealed class VersionCreateDto
{
    public string AppVersion { get; set; } = string.Empty;
    public string DbVersion { get; set; } = "0.0.0";
    public string? MinSupportVersion { get; set; }
    public bool IsUpgrading { get; set; }
    public string? UpgradeNode { get; set; }
    public DateTimeOffset? UpgradeStartTime { get; set; }
}

/// <summary>
/// 系统版本更新 DTO
/// </summary>
public sealed class VersionUpdateDto : BasicAppUDto
{
    public string AppVersion { get; set; } = string.Empty;
    public string DbVersion { get; set; } = "0.0.0";
    public string? MinSupportVersion { get; set; }
    public bool IsUpgrading { get; set; }
    public string? UpgradeNode { get; set; }
    public DateTimeOffset? UpgradeStartTime { get; set; }
}

/// <summary>
/// 系统版本升级开始 DTO
/// </summary>
public sealed class VersionUpgradeStartDto : BasicAppDto
{
    public string? UpgradeNode { get; set; }
    public DateTimeOffset? UpgradeStartTime { get; set; }
}

/// <summary>
/// 系统版本升级完成 DTO
/// </summary>
public sealed class VersionUpgradeFinishDto : BasicAppDto
{
    public string? AppVersion { get; set; }
    public string? DbVersion { get; set; }
    public string? MinSupportVersion { get; set; }
}
