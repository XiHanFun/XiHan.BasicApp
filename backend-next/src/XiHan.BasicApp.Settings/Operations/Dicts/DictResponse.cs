// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;

namespace XiHan.BasicApp.Settings.Operations.Dicts;

/// <summary>
/// 字典的网络契约。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OwnerTenantId">属主：<c>platform</c> 或租户 ID。</param>
/// <param name="IsPlatform">是否为平台字典。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Category">分类。</param>
/// <param name="Description">说明。</param>
/// <param name="IsBuiltIn">是否内建。</param>
/// <param name="Status">状态。</param>
/// <param name="Order">排序号。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
public sealed record DictResponse(
    Guid Id,
    string OwnerTenantId,
    bool IsPlatform,
    string Code,
    string Name,
    string? Category,
    string? Description,
    bool IsBuiltIn,
    SettingStatus Status,
    int Order,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static DictResponse From(SysDict dict)
    {
        ArgumentNullException.ThrowIfNull(dict);
        return new DictResponse(dict.Id, dict.OwnerTenantId, SettingsScope.IsPlatform(dict.OwnerTenantId), dict.Code, dict.Name, dict.Category, dict.Description, dict.IsBuiltIn, dict.Status, dict.Order, dict.CreatedAt);
    }
}

/// <summary>
/// 字典与字典项编码的文法：字母开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 100。
/// </summary>
public static class DictCodes
{
    /// <summary>
    /// 编码的正则。
    /// </summary>
    public const string Pattern = "^[A-Za-z][A-Za-z0-9._-]{0,99}$";
}

/// <summary>
/// <c>dicts.create</c> 的请求。
/// </summary>
/// <param name="Code">编码（平台与租户之间也不重）。</param>
/// <param name="Name">名称。</param>
/// <param name="Category">分类。</param>
/// <param name="Description">说明。</param>
/// <param name="Order">排序号。</param>
public sealed record CreateDictRequest(
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(DictCodes.Pattern)] string Code,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(50)] string? Category = null,
    [StringLength(500)] string? Description = null,
    [Range(0, 100_000)] int Order = 0);

/// <summary>
/// <c>dicts.get</c> 的请求。
/// </summary>
/// <param name="Id">字典主键。</param>
public sealed record GetDictRequest(Guid Id);

/// <summary>
/// <c>dicts.update</c> 的请求：编码不在这里改。
/// </summary>
/// <param name="Id">字典主键。</param>
/// <param name="Name">名称。</param>
/// <param name="Category">分类。</param>
/// <param name="Description">说明。</param>
/// <param name="Order">排序号。</param>
/// <param name="Status">状态。</param>
public sealed record UpdateDictRequest(
    Guid Id,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(50)] string? Category = null,
    [StringLength(500)] string? Description = null,
    [Range(0, 100_000)] int Order = 0,
    SettingStatus Status = SettingStatus.Active);

/// <summary>
/// <c>dicts.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">字典主键，1 到 100 个。</param>
public sealed record DictIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>dicts.options</c> 的请求。
/// </summary>
/// <param name="Code">字典编码。</param>
public sealed record DictOptionsRequest([Required, StringLength(100, MinimumLength = 1), RegularExpression(DictCodes.Pattern)] string Code);

/// <summary>
/// 一个下拉选项：值是字典项编码（业务数据按它存）。
/// </summary>
/// <param name="Value">字典项编码。</param>
/// <param name="Label">显示名。</param>
/// <param name="ParentValue">上级项编码；顶层为 <c>null</c>。</param>
/// <param name="IsDefault">是否为缺省项。</param>
/// <param name="Disabled">是否停用（只用于回显历史数据，下拉里不能再选）。</param>
public sealed record DictOption(string Value, string Label, string? ParentValue, bool IsDefault, bool Disabled);

/// <summary>
/// <c>dicts.options</c> 的响应：深度优先展平。
/// </summary>
/// <param name="Items">选项。</param>
public sealed record DictOptionsResponse(IReadOnlyList<DictOption> Items);
