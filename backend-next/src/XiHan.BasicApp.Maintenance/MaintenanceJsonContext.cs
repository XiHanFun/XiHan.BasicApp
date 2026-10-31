// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Maintenance.Operations.Caches;
using XiHan.BasicApp.Maintenance.Operations.JobRuns;
using XiHan.BasicApp.Maintenance.Operations.Jobs;
using XiHan.BasicApp.Maintenance.Operations.Migrations;
using XiHan.BasicApp.Maintenance.Operations.Server;
using XiHan.BasicApp.Maintenance.Operations.Versions;

namespace XiHan.BasicApp.Maintenance;

/// <summary>
/// 运维包操作请求与响应的 JSON 源生成上下文。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(PageResult<JobResponse>))]
[JsonSerializable(typeof(JobResponse))]
[JsonSerializable(typeof(JobIdRequest))]
[JsonSerializable(typeof(JobIdsRequest))]
[JsonSerializable(typeof(JobBatchResult))]
[JsonSerializable(typeof(JobQueuedResponse))]
[JsonSerializable(typeof(ValidateScheduleRequest))]
[JsonSerializable(typeof(ScheduleCheckResponse))]
[JsonSerializable(typeof(RescheduleJobRequest))]
[JsonSerializable(typeof(CreateJobRequest))]
[JsonSerializable(typeof(JobTypesRequest))]
[JsonSerializable(typeof(JobTypesResponse))]
[JsonSerializable(typeof(CacheNamespacesRequest))]
[JsonSerializable(typeof(CacheNamespacesResponse))]
[JsonSerializable(typeof(CacheKeysRequest))]
[JsonSerializable(typeof(CacheKeysResponse))]
[JsonSerializable(typeof(CacheKeyRequest))]
[JsonSerializable(typeof(CacheEntryResponse))]
[JsonSerializable(typeof(UpdateCacheRequest))]
[JsonSerializable(typeof(CacheKeysDeleteRequest))]
[JsonSerializable(typeof(CacheDeleteResult))]
[JsonSerializable(typeof(CachePrefixRequest))]
[JsonSerializable(typeof(CachePrefixDeleteResult))]
[JsonSerializable(typeof(ServerInfoRequest))]
[JsonSerializable(typeof(ServerInfoResponse))]
[JsonSerializable(typeof(PageResult<JobRunResponse>))]
[JsonSerializable(typeof(JobRunResponse))]
[JsonSerializable(typeof(VersionIdRequest))]
[JsonSerializable(typeof(MigrationIdRequest))]
[JsonSerializable(typeof(PageResult<SchemaVersionResponse>))]
[JsonSerializable(typeof(SchemaVersionResponse))]
[JsonSerializable(typeof(PageResult<MigrationResponse>))]
[JsonSerializable(typeof(MigrationResponse))]
public sealed partial class MaintenanceJsonContext : JsonSerializerContext;
