// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Organization.Domain.Entities;

namespace XiHan.BasicApp.Organization.Operations.Positions;

/// <summary>
/// 岗位的网络契约。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Status">状态。</param>
/// <param name="Order">排序号。</param>
/// <param name="Description">说明。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
public sealed record PositionResponse(
    Guid Id,
    string Code,
    string Name,
    OrganizationStatus Status,
    int Order,
    string? Description,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static PositionResponse From(SysPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return new PositionResponse(position.Id, position.Code, position.Name, position.Status, position.Order, position.Description, position.CreatedAt);
    }
}

/// <summary>
/// <c>positions.create</c> 的请求。
/// </summary>
/// <param name="Code">编码（租户内唯一）。</param>
/// <param name="Name">名称。</param>
/// <param name="Order">排序号。</param>
/// <param name="Description">说明。</param>
public sealed record CreatePositionRequest(
    [Required, StringLength(64, MinimumLength = 1), RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")] string Code,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(512)] string? Description = null);

/// <summary>
/// <c>positions.get</c> 的请求。
/// </summary>
/// <param name="Id">岗位主键。</param>
public sealed record GetPositionRequest(Guid Id);

/// <summary>
/// <c>positions.update</c> 的请求：编码不在这里改。
/// </summary>
/// <param name="Id">岗位主键。</param>
/// <param name="Name">名称。</param>
/// <param name="Order">排序号。</param>
/// <param name="Description">说明。</param>
/// <param name="Status">状态。</param>
public sealed record UpdatePositionRequest(
    Guid Id,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(512)] string? Description = null,
    OrganizationStatus Status = OrganizationStatus.Active);

/// <summary>
/// <c>positions.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">岗位主键，1 到 100 个。</param>
public sealed record PositionIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
