// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
                outcome.StartedAt,
                outcome.CompletedAt,
                (long)(outcome.CompletedAt - outcome.StartedAt).TotalMilliseconds,
                outcome.Result.IsSuccess,
                outcome.Result.Error?.Code,
                outcome.Result.Error is { } error ? context.Localize(error) : null)
            : null;
        return new JobResponse(job.Name.Value, title, description, job.JobTypeName, job.Source, job.Paused, status.RunningCount, job.AllowConcurrent, job.RunOnEveryInstance, status.NextRun, triggers, last, job.UpdatedAt);
    }

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

    private static string? Find(IMessageLocalizer? localizer, string? locale, string key)
        => localizer is not null && locale is not null && localizer.Resolve(key, locale, null).TryGetValue(out var text) ? text.Text : null;
}
