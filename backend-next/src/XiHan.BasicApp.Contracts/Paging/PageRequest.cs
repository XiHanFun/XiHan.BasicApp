// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Contracts.Paging;

/// <summary>
/// 分页列表请求：页码与页大小、字段筛选、关键字、多列排序。
/// 带它的 <c>list</c> 操作因含嵌套对象而落到 <c>POST /&lt;资源&gt;:list</c>。
/// 有缺省值的属性用 <c>set</c> 而不是 <c>init</c>：源生成的序列化把 init 属性当构造参数，JSON 里没写的成员会以 <see langword="null"/> 覆盖缺省值。
/// </summary>
public sealed record PageRequest
{
    /// <summary>
    /// 页码与页大小；缺省第一页、20 条。
    /// </summary>
    public PageInfo Page { get; set; } = PageInfo.Default;

    /// <summary>
    /// 字段筛选条件；多项之间是 AND。
    /// </summary>
    [MaxLength(32)]
    public IReadOnlyList<PageFilter> Filters { get; set; } = [];

    /// <summary>
    /// 关键字；由各查询类决定匹配哪些列。
    /// </summary>
    [StringLength(256)]
    public string? Keyword { get; set; }

    /// <summary>
    /// 排序；按顺序逐列生效，空时由查询类取缺省排序。
    /// </summary>
    [MaxLength(8)]
    public IReadOnlyList<PageSort> Sorts { get; set; } = [];
}
