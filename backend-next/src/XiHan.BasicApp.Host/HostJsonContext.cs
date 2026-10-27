// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Host.Commands;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 宿主自有端点与命令输出的源生成序列化上下文（应用自述、种子报告）。操作的请求 / 响应由各包的 JsonContext 经登记表处理。
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(HostInfo))]
[JsonSerializable(typeof(SeedReport))]
internal sealed partial class HostJsonContext : JsonSerializerContext;
