// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 客户端信息
/// </summary>
public sealed record ClientInfo(string? Ip, string? Location, string? UserAgent, string? Browser, string? Os);
