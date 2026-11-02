// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenDatasources;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成模块操作的请求 / 响应序列化上下文。
/// </summary>
/// <remarks>
/// 源生成；枚举按字符串。
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(CodegenDatasourceIdRequest))]
[JsonSerializable(typeof(ListCodegenDatasourceOptionsRequest))]
[JsonSerializable(typeof(CreateCodegenDatasourceRequest))]
[JsonSerializable(typeof(UpdateCodegenDatasourceRequest))]
[JsonSerializable(typeof(SetCodegenDatasourceStatusRequest))]
[JsonSerializable(typeof(CodegenDatasourceIdsRequest))]
[JsonSerializable(typeof(CodegenDatasourceResponse))]
[JsonSerializable(typeof(CodegenDatasourceDetailResponse))]
[JsonSerializable(typeof(CodegenDatasourceOptionsResponse))]
[JsonSerializable(typeof(CodegenDatasourceTestResponse))]
[JsonSerializable(typeof(PageResult<CodegenDatasourceResponse>))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(BatchResult))]
public sealed partial class CodeGenerationJsonContext : JsonSerializerContext;
