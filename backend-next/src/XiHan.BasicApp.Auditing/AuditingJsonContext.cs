// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Auditing.Operations.AccessLogs;
using XiHan.BasicApp.Auditing.Operations.ConditionHits;
using XiHan.BasicApp.Auditing.Operations.DiffLogs;
using XiHan.BasicApp.Auditing.Operations.ExceptionLogs;
using XiHan.BasicApp.Auditing.Operations.LoginLogs;
using XiHan.BasicApp.Auditing.Operations.MyActivity;
using XiHan.BasicApp.Auditing.Operations.MyLoginLogs;
using XiHan.BasicApp.Auditing.Operations.OpenApiLogs;
using XiHan.BasicApp.Auditing.Operations.OperationLogs;
using XiHan.BasicApp.Auditing.Operations.PermissionChangeLogs;
using XiHan.BasicApp.Auditing.Operations.Traces;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.Auditing;

/// <summary>
/// 审计包操作的请求 / 响应序列化上下文（源生成；枚举按字符串）。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(GetAccessLogRequest))]
[JsonSerializable(typeof(AccessLogResponse))]
[JsonSerializable(typeof(PageResult<AccessLogResponse>))]
[JsonSerializable(typeof(GetExceptionLogRequest))]
[JsonSerializable(typeof(ExceptionLogDetailResponse))]
[JsonSerializable(typeof(PageResult<ExceptionLogResponse>))]
[JsonSerializable(typeof(GetLoginLogRequest))]
[JsonSerializable(typeof(LoginLogResponse))]
[JsonSerializable(typeof(PageResult<LoginLogResponse>))]
[JsonSerializable(typeof(MyLoginLogsRequest))]
[JsonSerializable(typeof(MyLoginLogResponse))]
[JsonSerializable(typeof(PageResult<MyLoginLogResponse>))]
[JsonSerializable(typeof(MyActivityRequest))]
[JsonSerializable(typeof(MyActivityResponse))]
[JsonSerializable(typeof(GetOperationLogRequest))]
[JsonSerializable(typeof(OperationLogDetailResponse))]
[JsonSerializable(typeof(PageResult<OperationLogResponse>))]
[JsonSerializable(typeof(GetDiffLogRequest))]
[JsonSerializable(typeof(DiffLogDetailResponse))]
[JsonSerializable(typeof(PageResult<DiffLogResponse>))]
[JsonSerializable(typeof(GetPermissionChangeLogRequest))]
[JsonSerializable(typeof(PermissionChangeLogResponse))]
[JsonSerializable(typeof(PageResult<PermissionChangeLogResponse>))]
[JsonSerializable(typeof(GetConditionHitRequest))]
[JsonSerializable(typeof(ConditionHitResponse))]
[JsonSerializable(typeof(PageResult<ConditionHitResponse>))]
[JsonSerializable(typeof(GetOpenApiLogRequest))]
[JsonSerializable(typeof(OpenApiLogDetailResponse))]
[JsonSerializable(typeof(PageResult<OpenApiLogResponse>))]
[JsonSerializable(typeof(TraceTimelineRequest))]
[JsonSerializable(typeof(TraceTimelineResponse))]
public sealed partial class AuditingJsonContext : JsonSerializerContext;
