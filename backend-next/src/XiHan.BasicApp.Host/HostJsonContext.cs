// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 宿主自己的端点返回类型的源生成序列化上下文。
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(HostInfo))]
internal sealed partial class HostJsonContext : JsonSerializerContext;
