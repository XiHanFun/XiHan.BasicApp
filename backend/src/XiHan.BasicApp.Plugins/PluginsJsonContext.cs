// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Plugins.Operations.Plugins;

namespace XiHan.BasicApp.Plugins;

/// <summary>
/// 插件模块操作的请求 / 响应序列化上下文
/// </summary>
/// <remarks>
/// 源生成；枚举按字符串。插件包里的旁挂清单（<see cref="PluginDescriptor"/>）也经它读
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(GetPluginRequest))]
[JsonSerializable(typeof(InspectPluginRequest))]
[JsonSerializable(typeof(InstallPluginRequest))]
[JsonSerializable(typeof(GrantPluginRequest))]
[JsonSerializable(typeof(PluginIdsRequest))]
[JsonSerializable(typeof(PluginResponse))]
[JsonSerializable(typeof(PluginDetailResponse))]
[JsonSerializable(typeof(PluginInspectionResponse))]
[JsonSerializable(typeof(PageResult<PluginResponse>))]
[JsonSerializable(typeof(PluginDescriptor))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(BatchResult))]
public sealed partial class PluginsJsonContext : JsonSerializerContext;
