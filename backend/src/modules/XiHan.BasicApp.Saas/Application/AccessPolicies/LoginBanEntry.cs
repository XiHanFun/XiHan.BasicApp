// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 一条封禁
/// </summary>
/// <param name="Ip">客户端 IP</param>
/// <param name="Until">解封时间</param>
public sealed record LoginBanEntry(string Ip, DateTimeOffset Until);
