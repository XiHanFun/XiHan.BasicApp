// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个库升到当前应用版本还要做的结构变更
/// </summary>
/// <remarks>
/// 只读预览，不执行、不建临时表；升级时 ORM 只建缺的表、加缺的列与索引，列出的就是会做的全部，库里多出来的或与实体不一致的列不动、不列
/// </remarks>
/// <param name="Database">库名：平台库为 <see cref="SchemaUpgrader.PlatformDatabase"/>，独立库为租户 ID</param>
/// <param name="Provider">库类型</param>
/// <param name="From">台账上的结构版本；新库为 <see langword="null"/></param>
/// <param name="To">应用带的结构版本</param>
/// <param name="SyncDue">这次升级会不会按实体建表补列：新库、有脚本要执行或应用版本变了才做</param>
/// <param name="Scripts">要执行的脚本，按执行顺序</param>
/// <param name="Tables">要建的表与要加的列</param>
/// <param name="Indexes">要建的索引</param>
public sealed record SchemaPlan(
    string Database,
    string Provider,
    string? From,
    string To,
    bool SyncDue,
    IReadOnlyList<SchemaPlanScript> Scripts,
    IReadOnlyList<SchemaPlanTable> Tables,
    IReadOnlyList<SchemaPlanIndex> Indexes);
