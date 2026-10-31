// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Runtime.Clients;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Auditing.Abstractions.Pipeline;
using XiHan.Framework.Hosting.AspNetCore;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;
using XiHan.Framework.Protocols.Http.Routing;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Network;

namespace XiHan.BasicApp.Auditing.Middleware;

/// <summary>
/// Observe 阶段的审计日志中间件。
/// </summary>
/// <remarks>
/// 排在框架 <c>AuditMiddleware</c> 里面：
/// 调用开始前对标了审计的操作显式声明审计动作（操作日志）；调用结束后补上耗时、登录名与会话，以及客户端地址、浏览器与系统、归属地等细节（框架随即把操作审计落库，4.x 的操作日志同样在写入时记），
/// 再按最终结果写访问日志、异常日志、调用里登记的登录日志与权限变更日志（只在成功时记），以及仓储写入的数据变更（只在成功时记）。
/// 客户端地址、User-Agent、协议与请求目标取调用来源（ADR-0043）。日志只进有界队列，不影响调用结果。
/// </remarks>
/// <param name="queue">日志队列。</param>
/// <param name="options">日志选项。</param>
/// <param name="audited">进操作日志的操作。</param>
/// <param name="ids">日志主键生成器。</param>
/// <param name="clock">时间源。</param>
/// <param name="services">根服务（取宿主环境名）。</param>
public sealed class AuditLogMiddleware(AuditLogWriter queue, AuditLogOptions options, AuditedOperations audited, IIdGenerator<Guid> ids, TimeProvider clock, IServiceProvider services) : IPipelineMiddleware
{
    // 框架的协议传输端点名（xihan.protocols.mcp、xihan.protocols.cloudevents）
    private const string TransportEndpointPrefix = "xihan.protocols.";

    private readonly string? _environmentName = services.GetService<IHostEnvironment>()?.EnvironmentName;

    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        var operation = context.GetOperation().TryGetValue(out var descriptor) ? descriptor : null;
        if (operation is not null && audited.IsAudited(operation))
        {
            context.SetAuditAction(operation.Id);
        }

        var started = Stopwatch.GetTimestamp();
        Exception? thrown = null;
        try
        {
            await nextStep(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // 异常照常向外传给框架的 Observe 中间件转成结果；这里只记住它
            thrown = exception;
            throw;
        }
        finally
        {
            Record(context, operation, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds, thrown);
        }
    }

    private void Record(InvocationContext context, OperationDescriptor? operation, long durationMs, Exception? thrown)
    {
        var occurredAt = clock.GetUtcNow().UtcDateTime;
        var outcome = OutcomeOf(context, thrown);
        var caller = AuditCaller.From(context);
        var origin = context.GetOrigin().TryGetValue(out var source) ? source : null;
        var (method, path) = TargetOf(origin);
        var agent = UserAgentInfo.Parse(origin?.UserAgent);
        // 普通端点没有操作描述：身份取框架带进上下文的端点名（ADR-0048），授权阶段就被拒的调用也认得出；执行到了的由宿主的端点过滤器补记实际状态码，
        // 按同一套规则记访问与数据变更。协议的传输端点（MCP、CloudEvents）里面的操作各自记一条，不记传输本身
        var endpoint = operation is null && context.GetAuditedEndpoint().TryGetValue(out var registered) ? registered : null;
        var named = operation is null && origin?.EndpointName is { Length: > 0 } name && !name.StartsWith(TransportEndpointPrefix, StringComparison.Ordinal) ? name : null;
        var target = operation?.Id ?? endpoint?.Id ?? named;

        // 操作审计的细节：框架的审计中间件在本中间件返回之后才写事件
        if (context.GetAuditAction().HasValue)
        {
            context.AddAuditDetail("durationMs", durationMs);
            context.AddAuditDetail("userName", caller.UserName);
            context.AddAuditDetail("sessionId", caller.SessionId?.ToString("D"));
            context.AddAuditDetail("clientIp", Clip(origin?.RemoteAddress, 64));
            context.AddAuditDetail("userAgent", Clip(origin?.UserAgent, 512));
            context.AddAuditDetail("browser", agent.Browser);
            context.AddAuditDetail("os", agent.Os);
            context.AddAuditDetail("device", agent.Device);
            context.AddAuditDetail("location", LocationOf(origin?.RemoteAddress, context));
        }

        if (target is not null && options.AccessLogEnabled && !options.AccessLogExclusions.Contains(target))
        {
            var access = new SysAccessLog
            {
                OperationId = target,
                Protocol = Clip(origin?.Protocol, 16),
                Method = Clip(method, 16),
                Path = Clip(path, 512),
                Result = ResultOf(outcome),
                StatusCode = origin?.Protocol == InvocationProtocols.Http ? StatusCodeOf(operation, endpoint, outcome) : null,
                ErrorCode = outcome.Error?.Code,
                ErrorMessage = MessageOf(context, outcome),
                DurationMs = durationMs,
                Browser = agent.Browser,
                Os = agent.Os,
                Device = agent.Device,
                Referer = Clip(origin?.Referer, 1024),
            };
            Enqueue(access, caller, origin, context, occurredAt);
        }

        if (thrown is not null and not OperationCanceledException)
        {
            var failure = new SysExceptionLog
            {
                OperationId = target,
                Protocol = Clip(origin?.Protocol, 16),
                Method = Clip(method, 16),
                Path = Clip(path, 512),
                ExceptionType = Clip(thrown.GetType().FullName ?? thrown.GetType().Name, 256)!,
                Message = thrown.Message,
                StackTrace = thrown.ToString(),
                Source = Clip(thrown.Source, 256),
                Location = Clip(LocationOf(thrown), 512),
                MachineName = Clip(Environment.MachineName, 128),
                EnvironmentName = Clip(_environmentName, 64),
                AppVersion = AppInfo.Version,
            };
            Enqueue(failure, caller, origin, context, occurredAt);
        }

        foreach (var entry in context.GetAuditLogs())
        {
            switch (entry)
            {
                case LoginLogEntry login:
                    // 登录类事件的主体是账号本身（登录时调用方还是匿名）：用户、会话取登记的值，没有再取调用方
                    var log = new SysLoginLog
                    {
                        Event = login.Event,
                        Succeeded = outcome.IsSuccess,
                        ErrorCode = outcome.Error?.Code,
                        ErrorMessage = MessageOf(context, outcome),
                        AccountTenantId = Clip(login.AccountTenantId, 64),
                        ImpersonatorUserId = login.ImpersonatorUserId,
                        Detail = Clip(login.Detail, 512),
                        Browser = agent.Browser,
                        Os = agent.Os,
                        Device = agent.Device,
                    };
                    Enqueue(log, caller with
                    {
                        TenantId = login.TenantId ?? caller.TenantId,
                        UserId = login.UserId ?? caller.UserId,
                        UserName = login.UserName ?? caller.UserName,
                        SessionId = login.SessionId ?? caller.SessionId,
                    }, origin, context, occurredAt);
                    break;
                case PermissionChangeLogEntry change when outcome.IsSuccess:
                    var changed = new SysPermissionChangeLog
                    {
                        ChangeType = change.ChangeType,
                        RoleId = change.RoleId,
                        RoleName = Clip(change.RoleName, 256),
                        TargetUserId = change.TargetUserId,
                        TargetUserName = Clip(change.TargetUserName, 256),
                        EditionId = change.EditionId,
                        EditionCode = Clip(change.EditionCode, 64),
                        TargetTenantId = Clip(change.TargetTenantId, 64),
                        PermissionCodes = change.PermissionCodes.Count == 0 ? null : string.Join(',', change.PermissionCodes),
                        Detail = Clip(change.Detail, 512),
                    };
                    Enqueue(changed, caller with { TenantId = change.TenantId ?? caller.TenantId }, origin, context, occurredAt);
                    break;
                case ConditionHitLogEntry hit:
                    // 命中与调用成败无关：拦下的调用本身就是失败
                    var hitRecord = new SysConditionHit
                    {
                        Source = hit.Source,
                        PolicyId = hit.PolicyId,
                        PolicyCode = Clip(hit.PolicyCode, 128)!,
                        PolicyName = Clip(hit.PolicyName, 256),
                        Action = hit.Action,
                        Phase = hit.Phase,
                        IsBlocked = hit.IsBlocked,
                        OperationId = Clip(target, 128),
                        Method = Clip(method, 16),
                        Path = Clip(path, 512),
                        PermissionCodes = hit.PermissionCodes.Count == 0 ? null : Clip(string.Join(',', hit.PermissionCodes), 1024),
                    };
                    Enqueue(hitRecord, caller with { TenantId = hit.TenantId ?? caller.TenantId }, origin, context, occurredAt);
                    break;
                case OpenApiLogEntry open:
                    // 签名调用的调用方是匿名的：用户取凭证的归属人，日志落在归属人的注册地
                    var openRecord = new SysOpenApiLog
                    {
                        ClientId = Clip(open.ClientId, 128),
                        AppName = Clip(open.AppName, 256),
                        OperationId = Clip(target, 128),
                        Method = Clip(method, 16) ?? string.Empty,
                        Path = Clip(path, 512) ?? string.Empty,
                        SignatureAlgorithm = Clip(open.SignatureAlgorithm, 32),
                        SignatureValid = open.SignatureValid,
                        StatusCode = StatusCodeOf(operation, endpoint, outcome),
                        Succeeded = outcome.IsSuccess,
                        ErrorCode = outcome.Error?.Code,
                        ErrorMessage = MessageOf(context, outcome),
                        DurationMs = durationMs,
                        RequestSize = open.RequestSize,
                        ResponseSize = open.ResponseSize,
                        RequestBody = Clip(open.RequestBody, 4096),
                        ResponseBody = Clip(open.ResponseBody, 4096),
                    };
                    Enqueue(openRecord, caller with
                    {
                        TenantId = open.TenantId ?? caller.TenantId,
                        UserId = open.UserId ?? caller.UserId,
                        UserName = open.UserName ?? caller.UserName,
                    }, origin, context, occurredAt);
                    break;
            }
        }

        // 数据变更：只记操作与登记过的普通端点的调用、且调用成功的（失败的已回滚）；作业与种子的写入不记。缓冲每次都取空，不留给同一作用域的下一次调用
        if (context.Services.GetService<DiffLogBuffer>() is { } buffer)
        {
            var changes = buffer.Drain();
            if (target is not null && outcome.IsSuccess)
            {
                foreach (var diff in changes.SelectMany(model => DiffLogRows.From(model, target)))
                {
                    Enqueue(diff, caller, origin, context, occurredAt);
                }
            }
        }
    }

    private void Enqueue(AuditLogRecord record, AuditCaller caller, InvocationOrigin? origin, InvocationContext context, DateTime occurredAt)
    {
        caller.Stamp(record);
        record.ClientIp = Clip(origin?.RemoteAddress, 64);
        record.UserAgent = Clip(origin?.UserAgent, 512);
        record.Id = ids.NewId();
        record.OccurredAt = occurredAt;
        record.TraceId = context.TraceId;
        queue.Enqueue(record);
    }

    /// <summary>
    /// 客户端地址的归属地。
    /// </summary>
    /// <remarks>
    /// 经 <see cref="IIpRegionResolver"/>，宿主没装、内网地址与查不到都为空。
    /// </remarks>
    private static string? LocationOf(string? address, InvocationContext context)
        => context.Services.GetService<IIpRegionResolver>() is { } regions && IPAddress.TryParse(address, out var ip) && regions.Resolve(ip).TryGetValue(out var region) && region.Location.Length > 0
            ? Clip(region.Location, 128)
            : null;

    /// <summary>
    /// 取调用结果。
    /// </summary>
    /// <remarks>
    /// 管道里已写入的优先；抛出了异常而没写结果，按内核的转换记（取消是取消，其余是未处理异常）；正常返回却没写结果视为成功。
    /// </remarks>
    private static Result OutcomeOf(InvocationContext context, Exception? thrown)
    {
        if (context.GetOutcome().TryGetValue(out var written))
        {
            return written;
        }

        return thrown switch
        {
            null => Result.Success(),
            OperationCanceledException => new Error(KernelErrors.Cancelled, KernelMessages.Cancelled),
            _ => Error.Unexpected(KernelErrors.Unexpected, KernelMessages.UnhandledException),
        };
    }

    /// <summary>
    /// 访问结果按错误类别归并。
    /// </summary>
    /// <remarks>
    /// 认证失败与越权分开，找不到单列，未处理异常与依赖不可用是服务端故障，其余是业务失败。
    /// </remarks>
    private static AccessResult ResultOf(Result outcome)
    {
        if (outcome.IsSuccess)
        {
            return AccessResult.Success;
        }

        var error = outcome.Error!;
        return error.Kind switch
        {
            "DENIED" when error.Code is SecurityErrors.NotAuthenticated or SecurityErrors.InvalidCredential => AccessResult.Unauthorized,
            "DENIED" => AccessResult.Forbidden,
            "NOTFOUND" => AccessResult.NotFound,
            "VALIDATION" or "CONFLICT" or "TIMEOUT" or "CANCELLED" => AccessResult.Failed,
            _ => AccessResult.ServerError,
        };
    }

    /// <summary>
    /// 请求目标拆成方法与路径。
    /// </summary>
    /// <remarks>
    /// HTTP 的目标是「方法 路径」，其他协议（gRPC 方法全名、MCP 工具名、作业名…）整段当路径。
    /// </remarks>
    private static (string? Method, string? Path) TargetOf(InvocationOrigin? origin)
    {
        if (origin is null)
        {
            return (null, null);
        }

        if (origin.Protocol == InvocationProtocols.Http && origin.Target.IndexOf(' ', StringComparison.Ordinal) is var space and > 0)
        {
            return (origin.Target[..space], origin.Target[(space + 1)..]);
        }

        return (null, origin.Target);
    }

    /// <summary>
    /// 取 HTTP 状态码。
    /// </summary>
    /// <remarks>
    /// 普通端点已经开始写响应的取它实际写出的状态码（端点自己写出的失败响应也是，如范围不可满足的 416）；
    /// 其余按框架 HTTP 适配器的同一套映射推出（成功取操作的成功状态，失败按错误类别），不另写一份规则。
    /// </remarks>
    private static int StatusCodeOf(OperationDescriptor? operation, AuditedEndpoint? endpoint, Result outcome)
        => endpoint?.StatusCode
            ?? (outcome.IsFailure
                ? ProblemResponse.StatusCodeFor(outcome.Error!)
                : operation is not null ? HttpRouteConvention.SuccessStatusCode(operation.Contract) : StatusCodes.Status200OK);

    private static string? MessageOf(InvocationContext context, Result outcome)
        => outcome.IsFailure ? Clip(context.Localize(outcome.Error!), 1024) : null;

    /// <summary>
    /// 抛出点：堆栈第一帧的「类型.方法」。
    /// </summary>
    private static string? LocationOf(Exception exception)
    {
        var frame = exception.StackTrace?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (frame is null)
        {
            return null;
        }

        frame = frame.StartsWith("at ", StringComparison.Ordinal) ? frame[3..] : frame;
        var parameters = frame.IndexOf('(', StringComparison.Ordinal);
        return parameters > 0 ? frame[..parameters] : frame;
    }

    private static string? Clip(string? value, int max)
        => value is null || value.Length <= max ? value : value[..max];
}
