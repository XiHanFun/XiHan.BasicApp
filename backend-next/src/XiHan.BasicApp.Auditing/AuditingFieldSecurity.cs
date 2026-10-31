// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Operations.AccessLogs;
using XiHan.BasicApp.Auditing.Operations.ConditionHits;
using XiHan.BasicApp.Auditing.Operations.DiffLogs;
using XiHan.BasicApp.Auditing.Operations.ExceptionLogs;
using XiHan.BasicApp.Auditing.Operations.LoginLogs;
using XiHan.BasicApp.Auditing.Operations.OpenApiLogs;
using XiHan.BasicApp.Auditing.Operations.OperationLogs;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Auditing;

/// <summary>
/// 审计包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 4.x 同七类日志：访问、开放接口、操作、登录、异常、数据变更与条件命中（本人的「我的登录日志」与活跃度不套用）。
/// 字段取详情的响应类型（列表项是它的子集），规则由 Access 的字段安全中间件在列表与详情上统一套用；日志只读，没有写入。
/// </remarks>
public static class AuditingFieldSecurity
{
    /// <summary>
    /// 共用的字段显示名前缀（日志页的列名都在 <c>auditing.common</c> 下）。
    /// </summary>
    private const string Common = "auditing.common.";

    /// <summary>
    /// 登记本包的实体与操作。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("auditing", AuditingJsonContext.Default, fields => fields
            .Entity<AccessLogResponse>("auditing.access-logs", "auditing.access_logs.title", Common)
                .Reads(ListAccessLogs.Id, GetAccessLog.Id)
                .Keyword("operationId", "path", "errorCode")
            .Entity<OpenApiLogDetailResponse>("auditing.open-api-logs", "auditing.open_api_logs.title", Common)
                .Reads(ListOpenApiLogs.Id, GetOpenApiLog.Id)
                .Keyword("path", "clientId", "appName")
            .Entity<OperationLogDetailResponse>("auditing.operation-logs", "auditing.operation_logs.title", Common)
                .Reads(ListOperationLogs.Id, GetOperationLog.Id)
                .Keyword("operationId", "resource", "errorCode")
            .Entity<LoginLogResponse>("auditing.login-logs", "auditing.login_logs.title", Common)
                .Reads(ListLoginLogs.Id, GetLoginLog.Id)
                .Keyword("userName", "errorCode", "clientIp")
            .Entity<ExceptionLogDetailResponse>("auditing.exception-logs", "auditing.exception_logs.title", Common)
                .Reads(ListExceptionLogs.Id, GetExceptionLog.Id)
                .Keyword("exceptionType", "message", "operationId")
            .Entity<DiffLogDetailResponse>("auditing.diff-logs", "auditing.diff_logs.title", Common)
                .Reads(ListDiffLogs.Id, GetDiffLog.Id)
                .Keyword("entityType", "tableName", "entityId")
            .Entity<ConditionHitResponse>("auditing.condition-hits", "auditing.condition_hits.title", Common)
                .Reads(ListConditionHits.Id, GetConditionHit.Id)
                .Keyword("policyCode", "policyName", "path"));
}
