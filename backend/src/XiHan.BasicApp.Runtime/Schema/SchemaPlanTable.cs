// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一张要建的表，或一张已有的表上要加的列
/// </summary>
/// <param name="Table">表名</param>
/// <param name="IsNew">是否新表（是即 <see cref="Columns"/> 为全部列）</param>
/// <param name="Columns">要建或要加的列</param>
public sealed record SchemaPlanTable(string Table, bool IsNew, IReadOnlyList<SchemaPlanColumn> Columns);
