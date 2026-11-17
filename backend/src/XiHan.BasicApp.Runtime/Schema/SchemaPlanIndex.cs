// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个要建的索引
/// </summary>
/// <param name="Table">所在的表</param>
/// <param name="Index">索引名</param>
public sealed record SchemaPlanIndex(string Table, string Index);
