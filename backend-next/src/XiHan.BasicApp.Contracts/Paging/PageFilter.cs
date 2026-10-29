// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Contracts.Paging;

/// <summary>
/// 列表的一项筛选条件：字段、比较符与值。
/// </summary>
/// <remarks>
/// 字段名是响应 DTO 的属性名（camelCase），各查询类自己决定接受哪些字段与比较符。
/// </remarks>
/// <param name="Field">字段名。</param>
/// <param name="Operator">比较符：<c>eq</c>、<c>ne</c>、<c>contains</c>、<c>gt</c>、<c>ge</c>、<c>lt</c>、<c>le</c>、<c>in</c>。</param>
/// <param name="Value">比较值；<c>in</c> 用逗号分隔多个值。</param>
public sealed record PageFilter(
    [property: Required, StringLength(64, MinimumLength = 1)] string Field,
    [property: Required, RegularExpression("^(eq|ne|contains|gt|ge|lt|le|in)$")] string Operator,
    [property: StringLength(1024)] string? Value);
