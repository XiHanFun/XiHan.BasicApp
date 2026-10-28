// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Auditing.Abstractions.Pipeline;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Auditing.Middleware;

/// <summary>
/// Observe 阶段的审计日志中间件，排在框架 <c>AuditMiddleware</c> 里面：
/// 调用开始前对标了审计的操作显式声明审计动作（操作日志）；调用结束后补上耗时、登录名与会话等细节（框架随即把操作审计落库），
/// 再按最终结果写访问日志、异常日志，以及调用里登记的登录日志与权限变更日志（只在成功时记）。日志只进有界队列，不影响调用结果。
/// </summary>
/// <param name="queue">日志队列。</param>
/// <param name="options">日志选项。</param>
/// <param name="audited">进操作日志的操作。</param>
/// <param name="ids">日志主键生成器。</param>
/// <param name="clock">时间源。</param>
/// <param name="services">根服务（取宿主环境名）。</param>
public sealed class AuditLogMiddleware(AuditLogWriter queue, AuditLogOptions options, AuditedOperations audited, IIdGenerator<Guid> ids, TimeProvider clock, IServiceProvider services) : IPipelineMiddleware
{
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

        // 操作审计的细节：框架的审计中间件在本中间件返回之后才写事件
        if (context.GetAuditAction().HasValue)
        {
            context.AddAuditDetail("durationMs", durationMs);
            context.AddAuditDetail("userName", caller.UserName);
            context.AddAuditDetail("sessionId", caller.SessionId?.ToString("D"));
        }

        if (operation is not null && options.AccessLogEnabled && !options.AccessLogExclusions.Contains(operation.Id))
        {
            var access = new SysAccessLog
            {
                OperationId = operation.Id,
                Result = ResultOf(outcome),
                ErrorCode = outcome.Error?.Code,
                ErrorMessage = MessageOf(context, outcome),
                DurationMs = durationMs,
            };
            Enqueue(access, caller, context, occurredAt);
        }

        if (thrown is not null and not OperationCanceledException)
        {
            var failure = new SysExceptionLog
            {
                OperationId = operation?.Id,
                ExceptionType = Clip(thrown.GetType().FullName ?? thrown.GetType().Name, 256)!,
                Message = thrown.Message,
                StackTrace = thrown.ToString(),
                Source = Clip(thrown.Source, 256),
                Location = Clip(LocationOf(thrown), 512),
                MachineName = Clip(Environment.MachineName, 128),
                EnvironmentName = Clip(_environmentName, 64),
                AppVersion = AppInfo.Version,
            };
            Enqueue(failure, caller, context, occurredAt);
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
                    };
                    Enqueue(log, caller with
                    {
                        TenantId = login.TenantId ?? caller.TenantId,
                        UserId = login.UserId ?? caller.UserId,
                        UserName = login.UserName ?? caller.UserName,
                        SessionId = login.SessionId ?? caller.SessionId,
                    }, context, occurredAt);
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
                    Enqueue(changed, caller with { TenantId = change.TenantId ?? caller.TenantId }, context, occurredAt);
                    break;
            }
        }
    }

    private void Enqueue(AuditLogRecord record, AuditCaller caller, InvocationContext context, DateTime occurredAt)
    {
        caller.Stamp(record);
        record.Id = ids.NewId();
        record.OccurredAt = occurredAt;
        record.TraceId = context.TraceId;
        queue.Enqueue(record);
    }

    /// <summary>
    /// 调用结果：管道里已写入的优先；抛出了异常而没写结果，按内核的转换记（取消是取消，其余是未处理异常）；正常返回却没写结果视为成功。
    /// </summary>
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
    /// 访问结果按错误类别归并：认证失败与越权分开，找不到单列，未处理异常与依赖不可用是服务端故障，其余是业务失败。
    /// </summary>
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
