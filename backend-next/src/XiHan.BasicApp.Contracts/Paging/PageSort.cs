// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Contracts.Paging;

/// <summary>
/// 列表的一项排序：字段与方向。
/// </summary>
/// <remarks>
/// 字段名是响应 DTO 的属性名（camelCase），各查询类自己决定接受哪些字段。
/// </remarks>
/// <param name="Field">字段名。</param>
/// <param name="Descending">是否降序。</param>
public sealed record PageSort(
    [property: Required, StringLength(64, MinimumLength = 1)] string Field,
    bool Descending = false);
