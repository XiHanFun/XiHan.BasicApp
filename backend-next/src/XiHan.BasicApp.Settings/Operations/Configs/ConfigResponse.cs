// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;

namespace XiHan.BasicApp.Settings.Operations.Configs;

/// <summary>
/// 参数的网络契约。
/// </summary>
/// <remarks>
/// 加密参数的值不回显（<see cref="Value"/> 为 <c>null</c>，<see cref="HasValue"/> 标出有没有值）。
/// </remarks>
/// <param name="Id">主键。</param>
/// <param name="OwnerTenantId">属主：<c>platform</c> 或租户 ID。</param>
/// <param name="IsPlatform">是否为平台参数。</param>
/// <param name="Key">参数键。</param>
/// <param name="Name">名称。</param>
/// <param name="Group">分组。</param>
/// <param name="DataType">数据类型。</param>
/// <param name="Value">值；加密时为 <c>null</c>。</param>
/// <param name="DefaultValue">缺省值。</param>
/// <param name="HasValue">是否有值（加密参数据此判断）。</param>
/// <param name="IsEncrypted">是否加密。</param>
/// <param name="IsBuiltIn">是否内建。</param>
/// <param name="AllowTenantOverride">平台参数是否允许租户覆盖。</param>
/// <param name="Status">状态。</param>
/// <param name="Order">排序号。</param>
/// <param name="Description">说明。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
/// <param name="ModifiedAt">修改时间（UTC）。</param>
public sealed record ConfigResponse(
    Guid Id,
    string OwnerTenantId,
    bool IsPlatform,
    string Key,
    string Name,
    string? Group,
    ConfigDataType DataType,
    string? Value,
    string? DefaultValue,
    bool HasValue,
    bool IsEncrypted,
    bool IsBuiltIn,
    bool AllowTenantOverride,
    SettingStatus Status,
    int Order,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影（加密值不出库）。
    /// </summary>
    public static ConfigResponse From(SysConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new ConfigResponse(
            config.Id,
            config.OwnerTenantId,
            SettingsScope.IsPlatform(config.OwnerTenantId),
            config.Key,
            config.Name,
            config.Group,
            config.DataType,
            config.IsEncrypted ? null : config.Value,
            config.DefaultValue,
            !string.IsNullOrEmpty(config.Value),
            config.IsEncrypted,
            config.IsBuiltIn,
            config.AllowTenantOverride,
            config.Status,
            config.Order,
            config.Description,
            config.CreatedAt,
            config.ModifiedAt);
    }
}

/// <summary>
/// 参数键的文法。
/// </summary>
/// <remarks>
/// 点分小写，至少两段，段内可有数字与连字符（如 <c>auditing.retention-days</c>）。
/// </remarks>
public static class ConfigKeys
{
    /// <summary>
    /// 键的正则。
    /// </summary>
    public const string Pattern = "^[a-z][a-z0-9-]*(\\.[a-z0-9][a-z0-9-]*)+$";
}

/// <summary>
/// <c>configs.create</c> 的请求。
/// </summary>
/// <param name="Key">参数键（本侧唯一）。</param>
/// <param name="Name">名称。</param>
/// <param name="DataType">数据类型。</param>
/// <param name="Group">分组。</param>
/// <param name="Value">值。</param>
/// <param name="DefaultValue">缺省值。</param>
/// <param name="IsEncrypted">值是否加密落库。</param>
/// <param name="AllowTenantOverride">平台参数是否允许租户覆盖（租户参数忽略）。</param>
/// <param name="Order">排序号。</param>
/// <param name="Description">说明。</param>
public sealed record CreateConfigRequest(
    [Required, StringLength(128, MinimumLength = 3), RegularExpression(ConfigKeys.Pattern)] string Key,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    ConfigDataType DataType = ConfigDataType.Text,
    [StringLength(100)] string? Group = null,
    [StringLength(65536)] string? Value = null,
    [StringLength(65536)] string? DefaultValue = null,
    bool IsEncrypted = false,
    bool AllowTenantOverride = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Description = null);

/// <summary>
/// <c>configs.get</c> 的请求。
/// </summary>
/// <param name="Id">参数主键。</param>
public sealed record GetConfigRequest(Guid Id);

/// <summary>
/// <c>configs.update</c> 的请求。
/// </summary>
/// <remarks>
/// 键不在这里改；加密参数的 <see cref="Value"/> 为空即保留原值。
/// </remarks>
/// <param name="Id">参数主键。</param>
/// <param name="Name">名称。</param>
/// <param name="DataType">数据类型（内建参数不能改）。</param>
/// <param name="Group">分组。</param>
/// <param name="Value">值。</param>
/// <param name="DefaultValue">缺省值。</param>
/// <param name="IsEncrypted">值是否加密落库。</param>
/// <param name="AllowTenantOverride">平台参数是否允许租户覆盖（租户参数忽略）。</param>
/// <param name="Order">排序号。</param>
/// <param name="Description">说明。</param>
/// <param name="Status">状态。</param>
public sealed record UpdateConfigRequest(
    Guid Id,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    ConfigDataType DataType = ConfigDataType.Text,
    [StringLength(100)] string? Group = null,
    [StringLength(65536)] string? Value = null,
    [StringLength(65536)] string? DefaultValue = null,
    bool IsEncrypted = false,
    bool AllowTenantOverride = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Description = null,
    SettingStatus Status = SettingStatus.Active);

/// <summary>
/// <c>configs.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">参数主键，1 到 100 个。</param>
public sealed record ConfigIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
