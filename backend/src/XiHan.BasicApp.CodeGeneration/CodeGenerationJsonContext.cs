// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenDatasources;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenHistories;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenRuntime;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenTableColumns;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenTables;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenTemplates;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成模块操作的请求 / 响应序列化上下文
/// </summary>
/// <remarks>
/// 源生成；枚举按字符串。表与列配置里人工改过的字段集合（字符串数组）也经它往返
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
[JsonSerializable(typeof(CodegenTableIdRequest))]
[JsonSerializable(typeof(ListCodegenTableOptionsRequest))]
[JsonSerializable(typeof(ListCodegenDbTablesRequest))]
[JsonSerializable(typeof(ImportCodegenTablesRequest))]
[JsonSerializable(typeof(UpdateCodegenTableRequest))]
[JsonSerializable(typeof(SetCodegenTableStatusRequest))]
[JsonSerializable(typeof(CodegenTableIdsRequest))]
[JsonSerializable(typeof(CodegenTableResponse))]
[JsonSerializable(typeof(CodegenTableDetailResponse))]
[JsonSerializable(typeof(CodegenTableOptionsResponse))]
[JsonSerializable(typeof(CodegenImportResponse))]
[JsonSerializable(typeof(CodegenSyncResponse))]
[JsonSerializable(typeof(PageResult<CodegenTableResponse>))]
[JsonSerializable(typeof(PageResult<CodegenDbTableResponse>))]
[JsonSerializable(typeof(ListCodegenTableColumnsRequest))]
[JsonSerializable(typeof(SaveCodegenTableColumnsRequest))]
[JsonSerializable(typeof(CodegenTableColumnsResponse))]
[JsonSerializable(typeof(PreviewCodegenTableRequest))]
[JsonSerializable(typeof(CodegenPreviewResponse))]
[JsonSerializable(typeof(CodegenTemplateResponse))]
[JsonSerializable(typeof(CodegenTemplateDetailResponse))]
[JsonSerializable(typeof(PageResult<CodegenTemplateResponse>))]
[JsonSerializable(typeof(CodegenTemplateIdRequest))]
[JsonSerializable(typeof(CreateCodegenTemplateRequest))]
[JsonSerializable(typeof(UpdateCodegenTemplateRequest))]
[JsonSerializable(typeof(SetCodegenTemplateStatusRequest))]
[JsonSerializable(typeof(CodegenTemplateIdsRequest))]
[JsonSerializable(typeof(ValidateCodegenTemplateRequest))]
[JsonSerializable(typeof(CodegenTemplateValidationResponse))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(GenerateCodegenTableRequest))]
[JsonSerializable(typeof(CodegenGenerateResponse))]
[JsonSerializable(typeof(CodegenHistoryIdRequest))]
[JsonSerializable(typeof(CodegenHistoryResponse))]
[JsonSerializable(typeof(CodegenHistoryDetailResponse))]
[JsonSerializable(typeof(CodegenTableSnapshot))]
[JsonSerializable(typeof(PageResult<CodegenHistoryResponse>))]
[JsonSerializable(typeof(CodegenRuntimeIdRequest))]
[JsonSerializable(typeof(CodegenRuntimeSchemaResponse))]
[JsonSerializable(typeof(ListCodegenRuntimeRequest))]
[JsonSerializable(typeof(PageResult<IReadOnlyDictionary<string, JsonElement>>))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(BatchResult))]
public sealed partial class CodeGenerationJsonContext : JsonSerializerContext;
