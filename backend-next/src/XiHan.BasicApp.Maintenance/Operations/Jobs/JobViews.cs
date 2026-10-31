// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Maintenance.Operations.Jobs;

/// <summary>
/// 作业状态到响应的投影与公共检查。
/// </summary>
/// <remarks>
/// 作业的显示名与说明取登记它的包按区域给出的 <c>jobs.&lt;作业名&gt;.title</c> / <c>.description</c>（连字符换下划线），没有即作业名与登记时的说明。
/// </remarks>
internal static class JobViews
{
    /// <summary>
    /// 重试等待与执行时限的秒数上限。
    /// </summary>
    /// <remarks>
    /// 30 天，与框架的 <see cref="JobRetryPolicy.DelayLimit"/>、<see cref="JobDefinition.TimeoutLimit"/> 一致。
    /// </remarks>
    public const int MaxSeconds = 2_592_000;

    /// <summary>
    /// 不在平台一侧即拒绝。
    /// </summary>
    public static Error? RequirePlatform(InvocationContext context)
        => context.GetSecurity().Tenant is null ? null : Error.Denied(MaintenanceErrors.PlatformOnly, MaintenanceMessages.PlatformOnly);

    /// <summary>
    /// 作业不存在的错误。
    /// </summary>
    public static Error NotFound(string name)
        => Error.NotFound(MaintenanceErrors.JobNotFound, MaintenanceMessages.JobNotFound, MessageArguments.Of(("name", name)));

    /// <summary>
    /// 把作业状态投影成响应。
    /// </summary>
    public static JobResponse From(JobStatus status, InvocationContext context)
    {
        var job = status.Job;
        var localizer = context.Services.GetService<IMessageLocalizer>();
        var locale = localizer?.Options.Resolve(context);
        var key = "jobs." + job.Name.Value.Replace('-', '_');
        var title = Find(localizer, locale, key + ".title") ?? job.Description ?? job.Name.Value;
        var description = Find(localizer, locale, key + ".description") ?? (job.Description == title ? null : job.Description);
        var triggers = status.Triggers.Select(static t => new JobTriggerResponse(
            t.Name,
            t.Schedule.ToString(),
            t.Schedule.Kind,
            t.Schedule.Cron?.ToString(),
            t.Schedule.Kind == JobScheduleKind.Interval ? (long)t.Schedule.Interval.TotalSeconds : null,
            t.Schedule.TimeZoneId,
            t.State,
            t.NextFireAt,
            t.PreviousFireAt,
            t.FireCount,
            t.Error)).ToList();
        var last = status.Last is { } outcome
            ? new JobOutcomeResponse(
                outcome.Run.Id,
                outcome.Run.Trigger,
                outcome.Run.Attempt,
                outcome.StartedAt,
                outcome.CompletedAt,
                (long)(outcome.CompletedAt - outcome.StartedAt).TotalMilliseconds,
                outcome.Result.IsSuccess,
                outcome.Result.Error?.Code,
                outcome.Result.Error is { } error ? context.Localize(error) : null)
            : null;
        var retry = job.Retry is { } policy ? new JobRetryResponse(policy.MaxRetries, SecondsOf(policy.Delay), policy.Backoff, policy.MaxDelay is { } max ? SecondsOf(max) : null) : null;
        var timeout = job.Timeout is { } limit ? SecondsOf(limit) : (long?)null;
        return new JobResponse(job.Name.Value, title, description, job.JobTypeName, job.Source, job.Paused, status.RunningCount, job.AllowConcurrent, job.RunOnEveryInstance, retry, timeout, status.NextRun, triggers, last, job.UpdatedAt);
    }

    /// <summary>
    /// 由请求的几项拼出重试策略。
    /// </summary>
    /// <remarks>
    /// 没给重试次数即不重试，等待与退避随之不用；秒数的范围由请求的注解校验。
    /// </remarks>
    public static JobRetryPolicy? RetryOf(int? maxRetries, long delaySeconds, JobRetryBackoff backoff, long? maxDelaySeconds)
        => maxRetries is { } retries
            ? new JobRetryPolicy(retries, TimeSpan.FromSeconds(delaySeconds)) { Backoff = backoff, MaxDelay = maxDelaySeconds is { } max ? TimeSpan.FromSeconds(max) : null }
            : null;

    /// <summary>
    /// 由请求的秒数拼出执行时限。
    /// </summary>
    public static TimeSpan? TimeoutOf(long? seconds) => seconds is { } value ? TimeSpan.FromSeconds(value) : null;

    /// <summary>
    /// 计划的文本写法：Cron 表达式补上 <c>cron</c> 前缀与时区。
    /// </summary>
    public static string TextOf(string expression, string? timeZone)
    {
        var text = expression.Trim();
        var first = text.Split(' ', 2)[0].ToLowerInvariant();
        if (first is "cron" or "every" or "once" or "manual")
        {
            return text;
        }

        return "cron " + text + (string.IsNullOrWhiteSpace(timeZone) ? string.Empty : " tz=" + timeZone.Trim());
    }

    /// <summary>
    /// 解析计划写法。
    /// </summary>
    /// <remarks>
    /// 时区不认识也算解析失败。
    /// </remarks>
    public static bool TryParse(string expression, string? timeZone, [NotNullWhen(true)] out JobSchedule? schedule)
    {
        try
        {
            return JobSchedule.TryParse(TextOf(expression, timeZone), out schedule);
        }
        catch (TimeZoneNotFoundException)
        {
            schedule = null;
            return false;
        }
    }

    /// <summary>
    /// 解析逗号分隔的枚举名。
    /// </summary>
    /// <remarks>
    /// 有认不出的名字或一个都没有时为 <see langword="null"/>。
    /// </remarks>
    public static List<T>? EnumsOf<T>(string value)
        where T : struct, Enum
    {
        var result = new List<T>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<T>(part, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            {
                return null;
            }

            result.Add(parsed);
        }

        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// 时长的整秒数。
    /// </summary>
    /// <remarks>
    /// 不足一秒按一秒。
    /// </remarks>
    private static long SecondsOf(TimeSpan span) => (long)Math.Ceiling(span.TotalSeconds);

    private static string? Find(IMessageLocalizer? localizer, string? locale, string key)
        => localizer is not null && locale is not null && localizer.Resolve(key, locale, null).TryGetValue(out var text) ? text.Text : null;
}
