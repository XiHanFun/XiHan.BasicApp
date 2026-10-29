// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.OpenPlatform.Operations.Open;

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台包操作的请求 / 响应的序列化上下文（源生成；枚举按字符串）。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(OpenPingRequest))]
[JsonSerializable(typeof(OpenPingResponse))]
[JsonSerializable(typeof(OpenEchoRequest))]
[JsonSerializable(typeof(OpenEchoResponse))]
public sealed partial class OpenPlatformJsonContext : JsonSerializerContext;
