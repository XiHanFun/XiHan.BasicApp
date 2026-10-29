// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Settings.Operations.Configs;
using XiHan.BasicApp.Settings.Operations.DictItems;
using XiHan.BasicApp.Settings.Operations.Dicts;
using XiHan.BasicApp.Settings.Operations.NumberingRules;

namespace XiHan.BasicApp.Settings;

/// <summary>
/// 设置包操作的请求 / 响应序列化上下文。
/// </summary>
/// <remarks>
/// 源生成；枚举按字符串。
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(CreateConfigRequest))]
[JsonSerializable(typeof(GetConfigRequest))]
[JsonSerializable(typeof(UpdateConfigRequest))]
[JsonSerializable(typeof(ConfigIdsRequest))]
[JsonSerializable(typeof(ConfigResponse))]
[JsonSerializable(typeof(PageResult<ConfigResponse>))]
[JsonSerializable(typeof(CreateDictRequest))]
[JsonSerializable(typeof(GetDictRequest))]
[JsonSerializable(typeof(UpdateDictRequest))]
[JsonSerializable(typeof(DictIdsRequest))]
[JsonSerializable(typeof(DictOptionsRequest))]
[JsonSerializable(typeof(DictOptionsResponse))]
[JsonSerializable(typeof(DictResponse))]
[JsonSerializable(typeof(PageResult<DictResponse>))]
[JsonSerializable(typeof(ListDictItemsRequest))]
[JsonSerializable(typeof(DictItemsResponse))]
[JsonSerializable(typeof(GetDictItemRequest))]
[JsonSerializable(typeof(CreateDictItemRequest))]
[JsonSerializable(typeof(UpdateDictItemRequest))]
[JsonSerializable(typeof(DictItemIdsRequest))]
[JsonSerializable(typeof(DictItemResponse))]
[JsonSerializable(typeof(CreateNumberingRuleRequest))]
[JsonSerializable(typeof(GetNumberingRuleRequest))]
[JsonSerializable(typeof(UpdateNumberingRuleRequest))]
[JsonSerializable(typeof(NumberingRuleIdsRequest))]
[JsonSerializable(typeof(ResetNumberingRuleRequest))]
[JsonSerializable(typeof(PreviewNumberingRequest))]
[JsonSerializable(typeof(NumberingPreviewResponse))]
[JsonSerializable(typeof(GenerateNumbersRequest))]
[JsonSerializable(typeof(NumberAllocation))]
[JsonSerializable(typeof(NumberingAllocationsRequest))]
[JsonSerializable(typeof(NumberingRuleResponse))]
[JsonSerializable(typeof(PageResult<NumberingRuleResponse>))]
[JsonSerializable(typeof(PageResult<NumberingAllocationResponse>))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(BatchResult))]
public sealed partial class SettingsJsonContext : JsonSerializerContext;
