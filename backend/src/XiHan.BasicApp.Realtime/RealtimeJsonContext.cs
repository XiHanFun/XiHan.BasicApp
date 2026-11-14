// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Realtime.Handlers;
using XiHan.BasicApp.Realtime.Infrastructure;

namespace XiHan.BasicApp.Realtime;

/// <summary>
/// 实时包的源生成序列化上下文
/// </summary>
/// <remarks>
/// 共享缓存里的在线登记、断开连接的广播消息与在线状态变化的消息
/// </remarks>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(PresenceSnapshot))]
[JsonSerializable(typeof(PresenceNodes))]
[JsonSerializable(typeof(DisconnectRequest))]
[JsonSerializable(typeof(PresenceChanged))]
internal sealed partial class RealtimeJsonContext : JsonSerializerContext;
