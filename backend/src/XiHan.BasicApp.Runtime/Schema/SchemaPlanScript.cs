// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个要执行的升级脚本
/// </summary>
/// <param name="Version">所属的版本</param>
/// <param name="Script">脚本名（库类型目录起的相对路径）</param>
/// <param name="Sql">脚本原文</param>
public sealed record SchemaPlanScript(string Version, string Script, string Sql);
