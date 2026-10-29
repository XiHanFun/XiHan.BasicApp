// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace XiHan.BasicApp.Contracts.Paging;

/// <summary>
/// 页码与页大小。
/// </summary>
/// <remarks>
/// 页码从 1 起，页大小上限 <see cref="MaxSize"/>；约束折进请求 Schema，由 Validate 阶段拒绝越界值。
/// </remarks>
public sealed record PageInfo
{
    /// <summary>
    /// 页大小上限。
    /// </summary>
    public const int MaxSize = 500;

    /// <summary>
    /// 缺省页大小。
    /// </summary>
    public const int DefaultSize = 20;

    /// <summary>
    /// 第一页、缺省页大小。
    /// </summary>
    public static PageInfo Default { get; } = new();

    /// <summary>
    /// 页码，从 1 起。
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Index { get; set; } = 1;

    /// <summary>
    /// 页大小，1 到 <see cref="MaxSize"/>。
    /// </summary>
    [Range(1, MaxSize)]
    public int Size { get; set; } = DefaultSize;

    /// <summary>
    /// 本页之前要跳过的行数。
    /// </summary>
    /// <remarks>
    /// 派生值，不进 JSON 与 Schema。
    /// </remarks>
    [JsonIgnore]
    public int Skip => (Index - 1) * Size;
}
