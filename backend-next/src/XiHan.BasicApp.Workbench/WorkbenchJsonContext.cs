// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Workbench;
using XiHan.BasicApp.Workbench.Operations.Workbench;

namespace XiHan.BasicApp.Workbench;

/// <summary>
/// 工作台模块操作的 JSON 源生成上下文。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(WorkbenchWidgetsRequest))]
[JsonSerializable(typeof(WorkbenchWidgetsResponse))]
[JsonSerializable(typeof(WorkbenchWidgetDataRequest))]
[JsonSerializable(typeof(WorkbenchWidgetData))]
public sealed partial class WorkbenchJsonContext : JsonSerializerContext;
