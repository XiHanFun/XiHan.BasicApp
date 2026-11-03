// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.AI.Operations.AiProviders;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块的 JSON 源生成上下文
/// </summary>
/// <remarks>
/// 本包全部请求与响应；枚举按名称字符串
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AiProviderIdRequest))]
[JsonSerializable(typeof(CreateAiProviderRequest))]
[JsonSerializable(typeof(UpdateAiProviderRequest))]
[JsonSerializable(typeof(SetAiProviderStatusRequest))]
[JsonSerializable(typeof(AiProviderIdsRequest))]
[JsonSerializable(typeof(AiProviderResponse))]
[JsonSerializable(typeof(AiProviderTestResponse))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(PageResult<AiProviderResponse>))]
[JsonSerializable(typeof(BatchResult))]
public sealed partial class AIJsonContext : JsonSerializerContext;
