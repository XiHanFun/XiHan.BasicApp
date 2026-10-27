// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Paging;

/// <summary>
/// 分页列表响应：本页数据、总数与回显的页信息。
/// </summary>
/// <typeparam name="T">列表项类型。</typeparam>
/// <param name="Items">本页数据。</param>
/// <param name="Total">满足条件的总行数。</param>
/// <param name="Page">本页的页码与页大小。</param>
public sealed record PageResult<T>(IReadOnlyList<T> Items, long Total, PageInfo Page);
