// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Host;

/// <summary>
/// 根路径 <c>GET /</c> 的响应：应用名、版本与环境。
/// </summary>
/// <param name="Name">应用名。</param>
/// <param name="Version">应用版本。</param>
/// <param name="Environment">宿主环境名。</param>
public sealed record HostInfo(string Name, string Version, string Environment);
