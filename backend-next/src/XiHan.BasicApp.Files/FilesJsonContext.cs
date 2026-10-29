// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Files.Operations.ExportTasks;
using XiHan.BasicApp.Files.Operations.Files;
using XiHan.BasicApp.Files.Operations.ImportHistories;
using XiHan.BasicApp.Files.Operations.StorageConfigs;

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件包操作与上传端点的请求 / 响应序列化上下文（源生成；枚举按字符串）。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(BatchResult))]
[JsonSerializable(typeof(FileResponse))]
[JsonSerializable(typeof(PageResult<FileResponse>))]
[JsonSerializable(typeof(GetFileRequest))]
[JsonSerializable(typeof(UpdateFileRequest))]
[JsonSerializable(typeof(SetFileStatusRequest))]
[JsonSerializable(typeof(FileIdsRequest))]
[JsonSerializable(typeof(PresignFileRequest))]
[JsonSerializable(typeof(PresignFileResponse))]
[JsonSerializable(typeof(FastUploadFileRequest))]
[JsonSerializable(typeof(FastUploadFileResponse))]
[JsonSerializable(typeof(FileStoragesResponse))]
[JsonSerializable(typeof(FileStorageRequest))]
[JsonSerializable(typeof(SetFileStorageStatusRequest))]
[JsonSerializable(typeof(StorageConfigResponse))]
[JsonSerializable(typeof(PageResult<StorageConfigResponse>))]
[JsonSerializable(typeof(StorageConfigIdRequest))]
[JsonSerializable(typeof(CreateStorageConfigRequest))]
[JsonSerializable(typeof(UpdateStorageConfigRequest))]
[JsonSerializable(typeof(SetStorageConfigStatusRequest))]
[JsonSerializable(typeof(StorageConfigIdsRequest))]
[JsonSerializable(typeof(SubmitExportTaskRequest))]
[JsonSerializable(typeof(ExportTaskResponse))]
[JsonSerializable(typeof(PageResult<ExportTaskResponse>))]
[JsonSerializable(typeof(ExportTaskIdRequest))]
[JsonSerializable(typeof(ExportTaskIdsRequest))]
[JsonSerializable(typeof(List<ExportColumnSnapshot>))]
[JsonSerializable(typeof(CreateImportHistoryRequest))]
[JsonSerializable(typeof(ImportHistoryResponse))]
[JsonSerializable(typeof(RecentImportHistoriesRequest))]
[JsonSerializable(typeof(ImportHistoriesResponse))]
public sealed partial class FilesJsonContext : JsonSerializerContext;
