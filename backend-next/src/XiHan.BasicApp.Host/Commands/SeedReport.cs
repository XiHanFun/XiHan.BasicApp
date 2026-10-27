// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Host.Commands;

/// <summary>
/// <c>seed</c> 命令写到 stdout 的结果。
/// </summary>
/// <param name="Status"><c>completed</c>（已执行）或 <c>listed</c>（<c>--list</c>，只列出）。</param>
/// <param name="Seeders">涉及的种子，按执行顺序。</param>
public sealed record SeedReport(string Status, IReadOnlyList<SeedEntry> Seeders);

/// <summary>
/// 一个种子。
/// </summary>
/// <param name="Name">种子名（<c>&lt;包&gt;.&lt;主题&gt;</c>）。</param>
/// <param name="Order">号段。</param>
public sealed record SeedEntry(string Name, int Order);
