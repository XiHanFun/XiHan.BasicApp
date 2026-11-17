// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个要建或要加的列
/// </summary>
/// <param name="Column">列名</param>
/// <param name="Type">列类型（按这个库的类型名，带长度）</param>
/// <param name="IsNullable">是否可空</param>
public sealed record SchemaPlanColumn(string Column, string Type, bool IsNullable);
