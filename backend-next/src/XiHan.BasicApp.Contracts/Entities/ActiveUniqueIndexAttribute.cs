// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 软删除实体上未删除的行在这些列上唯一。
/// </summary>
/// <remarks>
/// 落成部分唯一索引 <c>ux_&lt;表&gt;_&lt;列&gt;… WHERE is_deleted = false</c>：已删除的行不受约束，同一个键删了再建、再删都不冲突（4.x 把 <c>is_deleted</c> 放进唯一列，同键第二次删除即撞约束）。
/// SqlSugar 的 <c>[SugarIndex]</c> 写不出条件，由 <c>Runtime</c> 在建表之后补建，生产库随基线脚本建出；不软删除的实体照用 <c>[SugarIndex]</c> 的唯一索引。
/// 结构测试守护：只用于软删除实体，软删除实体不写唯一的 <c>[SugarIndex]</c>，列必须是实体的属性。
/// </remarks>
/// <param name="columns">属性名，按索引列的顺序。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ActiveUniqueIndexAttribute(params string[] columns) : Attribute
{
    /// <summary>
    /// 属性名。
    /// </summary>
    public IReadOnlyList<string> Columns { get; } = columns;

    /// <summary>
    /// 索引名。
    /// </summary>
    /// <remarks>
    /// 不写即 <c>ux_&lt;表&gt;_&lt;列&gt;…</c>；超过 PostgreSQL 的 63 字符上限时写一个短名。
    /// </remarks>
    public string? Name { get; init; }
}
