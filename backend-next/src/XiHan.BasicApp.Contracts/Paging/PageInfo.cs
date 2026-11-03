// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace XiHan.BasicApp.Contracts.Paging;

/// <summary>
/// 页码与页大小
/// </summary>
/// <remarks>
/// 页码 1 到 <see cref="MaxIndex"/>，页大小上限 <see cref="MaxSize"/>；约束折进请求 Schema，由 Validate 阶段以 400 拒绝越界值（框架的字段文案，十种语言）
/// </remarks>
public sealed record PageInfo
{
    /// <summary>
    /// 页大小上限
    /// </summary>
    public const int MaxSize = 500;

    /// <summary>
    /// 页码上限
    /// </summary>
    /// <remarks>
    /// 页码换算成要跳过的行数按 <see cref="int"/> 计算：原先只限到 <see cref="int.MaxValue"/>，跳过的行数溢出成负数，PostgreSQL 报错成 500、SQLite 返回第一页
    /// 十万页、每页最多 500 行，跳过的行数不超过 5 千万，远在溢出之下；更深的翻页用筛选缩小范围
    /// </remarks>
    public const int MaxIndex = 100_000;

    /// <summary>
    /// 缺省页大小
    /// </summary>
    public const int DefaultSize = 20;

    /// <summary>
    /// 第一页、缺省页大小
    /// </summary>
    public static PageInfo Default { get; } = new();

    /// <summary>
    /// 页码，1 到 <see cref="MaxIndex"/>
    /// </summary>
    [Range(1, MaxIndex)]
    public int Index { get; set; } = 1;

    /// <summary>
    /// 页大小，1 到 <see cref="MaxSize"/>
    /// </summary>
    [Range(1, MaxSize)]
    public int Size { get; set; } = DefaultSize;

    /// <summary>
    /// 本页之前要跳过的行数
    /// </summary>
    /// <remarks>
    /// 派生值，不进 JSON 与 Schema
    /// </remarks>
    [JsonIgnore]
    public int Skip => (Index - 1) * Size;
}
