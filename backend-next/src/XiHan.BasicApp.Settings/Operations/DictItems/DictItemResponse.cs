// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Operations.Dicts;

namespace XiHan.BasicApp.Settings.Operations.DictItems;

/// <summary>
/// 字典项的网络契约。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="DictId">所在字典。</param>
/// <param name="ParentId">上级项。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">显示名。</param>
/// <param name="Value">附加值。</param>
/// <param name="Description">说明。</param>
/// <param name="Metadata">扩展元数据（JSON）。</param>
/// <param name="IsDefault">是否为缺省项。</param>
/// <param name="Status">状态。</param>
/// <param name="Order">排序号。</param>
/// <param name="Depth">层级（顶层为 0）。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
public sealed record DictItemResponse(
    Guid Id,
    Guid DictId,
    Guid? ParentId,
    string Code,
    string Name,
    string? Value,
    string? Description,
    string? Metadata,
    bool IsDefault,
    SettingStatus Status,
    int Order,
    int Depth,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static DictItemResponse From(SysDictItem item, int depth)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new DictItemResponse(item.Id, item.DictId, item.ParentId, item.Code, item.Name, item.Value, item.Description, item.Metadata, item.IsDefault, item.Status, item.Order, depth, item.CreatedAt);
    }
}

/// <summary>
/// <c>dict-items.list</c> 的请求。
/// </summary>
/// <param name="DictId">字典主键。</param>
public sealed record ListDictItemsRequest(Guid DictId);

/// <summary>
/// <c>dict-items.list</c> 的响应：深度优先，带层级。
/// </summary>
/// <param name="Items">字典项。</param>
public sealed record DictItemsResponse(IReadOnlyList<DictItemResponse> Items);

/// <summary>
/// <c>dict-items.get</c> 的请求。
/// </summary>
/// <param name="Id">字典项主键。</param>
public sealed record GetDictItemRequest(Guid Id);

/// <summary>
/// <c>dict-items.create</c> 的请求。
/// </summary>
/// <param name="DictId">所在字典。</param>
/// <param name="Code">编码（字典内唯一）。</param>
/// <param name="Name">显示名。</param>
/// <param name="ParentId">上级项（同一字典）。</param>
/// <param name="Value">附加值。</param>
/// <param name="Description">说明。</param>
/// <param name="Metadata">扩展元数据（JSON）。</param>
/// <param name="IsDefault">是否为缺省项。</param>
/// <param name="Order">排序号。</param>
public sealed record CreateDictItemRequest(
    Guid DictId,
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(DictCodes.Pattern)] string Code,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    Guid? ParentId = null,
    [StringLength(500)] string? Value = null,
    [StringLength(500)] string? Description = null,
    [StringLength(65536)] string? Metadata = null,
    bool IsDefault = false,
    [Range(0, 100_000)] int Order = 0);

/// <summary>
/// <c>dict-items.update</c> 的请求：编码不在这里改。
/// </summary>
/// <param name="Id">字典项主键。</param>
/// <param name="Name">显示名。</param>
/// <param name="ParentId">上级项（同一字典，不能是自己或下级）。</param>
/// <param name="Value">附加值。</param>
/// <param name="Description">说明。</param>
/// <param name="Metadata">扩展元数据（JSON）。</param>
/// <param name="IsDefault">是否为缺省项。</param>
/// <param name="Order">排序号。</param>
/// <param name="Status">状态。</param>
public sealed record UpdateDictItemRequest(
    Guid Id,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    Guid? ParentId = null,
    [StringLength(500)] string? Value = null,
    [StringLength(500)] string? Description = null,
    [StringLength(65536)] string? Metadata = null,
    bool IsDefault = false,
    [Range(0, 100_000)] int Order = 0,
    SettingStatus Status = SettingStatus.Active);

/// <summary>
/// <c>dict-items.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">字典项主键，1 到 100 个。</param>
public sealed record DictItemIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
