// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 链路追踪时间线条目 DTO（跨日志类型归一化）
/// </summary>
public sealed class TraceTimelineItemDto : IFieldSecurityMultiSourceDto
{
    /// <summary>
    /// 日志类型
    /// </summary>
    public TraceLogType LogType { get; set; }

    /// <summary>
    /// 日志主键（用于按类型回查完整详情）
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 条目时间（各类日志的主时间：访问/请求/操作/登录/异常/审计/变更时间）
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// 标题（动作摘要，如 "GET /api/users"、异常类型、操作标题）
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 摘要（次要说明）
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// 归一化状态（success / warning / error / info / default），驱动时间线颜色
    /// </summary>
    public string Status { get; set; } = "default";

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 会话标识
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 链路追踪 ID
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// IP 地址
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// 地理位置
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 请求路径 / 资源路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 响应状态码（无则为 null）
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// 执行耗时（毫秒，无则为 null）
    /// </summary>
    public long? ExecutionTime { get; set; }

    /// <summary>
    /// 各日志类型的字段来源（与 TraceApplicationMapper 的映射一致）：字段安全按来源日志的规则给本条打码。
    /// 拼出来的标题与摘要取其中最敏感的来源字段，来源字段受保护时整条一起打码。
    /// </summary>
    private static readonly IReadOnlyDictionary<TraceLogType, (Type Entity, IReadOnlyDictionary<string, string> Sources)> FieldSecuritySources =
        new Dictionary<TraceLogType, (Type, IReadOnlyDictionary<string, string>)>
        {
            [TraceLogType.Access] = (typeof(SysAccessLog), Sources(
                (nameof(Time), nameof(SysAccessLog.AccessTime)), (nameof(Title), nameof(SysAccessLog.ResourcePath)),
                (nameof(Summary), nameof(SysAccessLog.ResourceName)), (nameof(Status), nameof(SysAccessLog.AccessResult)),
                (nameof(SessionId), nameof(SysAccessLog.UserSessionId)), (nameof(Ip), nameof(SysAccessLog.AccessIp)),
                (nameof(Location), nameof(SysAccessLog.AccessLocation)), (nameof(Path), nameof(SysAccessLog.ResourcePath)))),
            [TraceLogType.Api] = (typeof(SysOpenApiLog), Sources(
                (nameof(Time), nameof(SysOpenApiLog.RequestTime)), (nameof(Title), nameof(SysOpenApiLog.ApiPath)),
                (nameof(Summary), nameof(SysOpenApiLog.ApiName)), (nameof(Status), nameof(SysOpenApiLog.IsSuccess)),
                (nameof(SessionId), nameof(SysOpenApiLog.UserSessionId)), (nameof(Ip), nameof(SysOpenApiLog.RequestIp)),
                (nameof(Location), nameof(SysOpenApiLog.RequestLocation)), (nameof(Path), nameof(SysOpenApiLog.ApiPath)))),
            [TraceLogType.Operation] = (typeof(SysOperationLog), Sources(
                (nameof(Time), nameof(SysOperationLog.OperationTime)), (nameof(Title), nameof(SysOperationLog.Title)),
                (nameof(Summary), nameof(SysOperationLog.Description)), (nameof(Status), nameof(SysOperationLog.Result)),
                (nameof(SessionId), nameof(SysOperationLog.UserSessionId)), (nameof(Ip), nameof(SysOperationLog.OperationIp)),
                (nameof(Location), nameof(SysOperationLog.OperationLocation)), (nameof(Path), nameof(SysOperationLog.RequestUrl)))),
            [TraceLogType.Login] = (typeof(SysLoginLog), Sources(
                (nameof(Time), nameof(SysLoginLog.LoginTime)), (nameof(Title), nameof(SysLoginLog.LoginResult)),
                (nameof(Summary), nameof(SysLoginLog.Message)), (nameof(Status), nameof(SysLoginLog.LoginResult)),
                (nameof(Ip), nameof(SysLoginLog.LoginIp)), (nameof(Location), nameof(SysLoginLog.LoginLocation)))),
            [TraceLogType.Exception] = (typeof(SysExceptionLog), Sources(
                (nameof(Time), nameof(SysExceptionLog.ExceptionTime)), (nameof(Title), nameof(SysExceptionLog.ExceptionType)),
                (nameof(Summary), nameof(SysExceptionLog.ExceptionMessage)), (nameof(Status), nameof(SysExceptionLog.SeverityLevel)),
                (nameof(Ip), nameof(SysExceptionLog.OperationIp)), (nameof(Location), nameof(SysExceptionLog.OperationLocation)),
                (nameof(Method), nameof(SysExceptionLog.RequestMethod)), (nameof(Path), nameof(SysExceptionLog.RequestPath)))),
            [TraceLogType.Diff] = (typeof(SysDiffLog), Sources(
                (nameof(Time), nameof(SysDiffLog.AuditTime)), (nameof(Title), nameof(SysDiffLog.EntityName)),
                (nameof(Summary), nameof(SysDiffLog.ChangeDescription)), (nameof(Status), nameof(SysDiffLog.IsSuccess)),
                (nameof(Ip), nameof(SysDiffLog.OperationIp))))
        };

    /// <summary>
    /// 来源实体：权限变更日志不受字段安全管理，为空
    /// </summary>
    Type? IFieldSecurityMultiSourceDto.FieldSecurityEntity =>
        FieldSecuritySources.TryGetValue(LogType, out var source) ? source.Entity : null;

    /// <summary>
    /// 属性来源：列出的按映射取，其余与来源同名（如 UserName、TraceId）
    /// </summary>
    string? IFieldSecurityMultiSourceDto.FieldSecuritySourceOf(string dtoProperty) =>
        FieldSecuritySources.TryGetValue(LogType, out var source)
            ? source.Sources.GetValueOrDefault(dtoProperty, dtoProperty)
            : null;

    private static IReadOnlyDictionary<string, string> Sources(params (string DtoProperty, string EntityField)[] pairs) =>
        pairs.ToDictionary(pair => pair.DtoProperty, pair => pair.EntityField, StringComparer.Ordinal);
}
