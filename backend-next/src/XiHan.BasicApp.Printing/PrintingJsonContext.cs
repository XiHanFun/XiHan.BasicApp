// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Printing.Domain.Services;
using XiHan.BasicApp.Printing.Operations.PrintDataSources;
using XiHan.BasicApp.Printing.Operations.PrintTemplates;

namespace XiHan.BasicApp.Printing;

/// <summary>
/// 打印模块操作的请求 / 响应序列化上下文。
/// </summary>
/// <remarks>
/// 源生成；枚举按字符串。缓存里的模板（<see cref="CachedPrintTemplate"/>）也经它往返。
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(GetPrintTemplateRequest))]
[JsonSerializable(typeof(ResolvePrintTemplateRequest))]
[JsonSerializable(typeof(CreatePrintTemplateRequest))]
[JsonSerializable(typeof(UpdatePrintTemplateRequest))]
[JsonSerializable(typeof(SetPrintTemplateStatusRequest))]
[JsonSerializable(typeof(PrintTemplateIdsRequest))]
[JsonSerializable(typeof(PrintTemplateResponse))]
[JsonSerializable(typeof(PrintTemplateDetailResponse))]
[JsonSerializable(typeof(ResolvedPrintTemplateResponse))]
[JsonSerializable(typeof(PageResult<PrintTemplateResponse>))]
[JsonSerializable(typeof(ListPrintDataSourcesRequest))]
[JsonSerializable(typeof(PrintDataSourcesResponse))]
[JsonSerializable(typeof(CachedPrintTemplate))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(BatchResult))]
public sealed partial class PrintingJsonContext : JsonSerializerContext;
