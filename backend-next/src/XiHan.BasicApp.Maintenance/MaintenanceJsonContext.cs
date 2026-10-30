// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Maintenance.Operations.JobRuns;
using XiHan.BasicApp.Maintenance.Operations.Jobs;

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
[JsonSerializable(typeof(PageResult<JobRunResponse>))]
[JsonSerializable(typeof(JobRunResponse))]
public sealed partial class MaintenanceJsonContext : JsonSerializerContext;
