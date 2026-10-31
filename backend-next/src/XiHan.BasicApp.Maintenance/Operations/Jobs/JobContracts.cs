// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;
using XiHan.Framework.Scheduling.Abstractions.Schedules;
using XiHan.Framework.Scheduling.Abstractions.Storage;

namespace XiHan.BasicApp.Maintenance.Operations.Jobs;

/// <summary>
/// 按名称指定一个作业的请求。
/// </summary>
/// <param name="Id">作业名（如 <c>auditing.retention</c>）。</param>
public sealed record JobIdRequest([Required, StringLength(JobName.MaxLength, MinimumLength = 1)] string Id);

/// <summary>
/// 按名称指定一批作业的请求。
/// </summary>
/// <param name="Ids">作业名。</param>
public sealed record JobIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<string> Ids);

/// <summary>
/// 一个作业。
/// </summary>
/// <param name="Name">作业名。</param>
/// <param name="Title">显示名（登记作业的包按区域给出，没给即作业名）。</param>
/// <param name="Description">说明；没有为 <see langword="null"/>。</param>
/// <param name="JobType">作业类型的登记键。</param>
/// <param name="Source">来源：代码登记或运行时登记。</param>
/// <param name="Paused">是否暂停。</param>
/// <param name="RunningCount">正在运行的次数。</param>
/// <param name="AllowConcurrent">是否允许并发运行。</param>
/// <param name="RunOnEveryInstance">是否每个节点都运行。</param>
/// <param name="Retry">失败后的重试策略；不重试为 <see langword="null"/>。</param>
/// <param name="TimeoutSeconds">单次执行的时限（秒）；不限为 <see langword="null"/>。</param>
/// <param name="NextRunAt">下次运行时间；暂停或没有计划时为 <see langword="null"/>。</param>
/// <param name="Triggers">触发器。</param>
/// <param name="Last">上一次运行的结果；没运行过为 <see langword="null"/>。</param>
/// <param name="UpdatedAt">登记最后更新的时间。</param>
public sealed record JobResponse(
    string Name,
    string Title,
    string? Description,
    string JobType,
    JobSource Source,
    bool Paused,
    int RunningCount,
    bool AllowConcurrent,
    bool RunOnEveryInstance,
    JobRetryResponse? Retry,
    long? TimeoutSeconds,
    DateTimeOffset? NextRunAt,
    IReadOnlyList<JobTriggerResponse> Triggers,
    JobOutcomeResponse? Last,
    DateTimeOffset UpdatedAt);

/// <summary>
/// 作业失败后的重试策略。
/// </summary>
/// <remarks>
/// 只有失败的运行重试（返回失败、抛出异常或超过执行时限），被取消与失联的不重试；每次重试是一次新运行。
/// </remarks>
/// <param name="MaxRetries">最多重试几次。</param>
/// <param name="DelaySeconds">第一次重试前等几秒。</param>
/// <param name="Backoff">等待方式：每次相同或指数退避（每次翻倍）。</param>
/// <param name="MaxDelaySeconds">指数退避的等待上限（秒）；没有为 <see langword="null"/>。</param>
public sealed record JobRetryResponse(int MaxRetries, long DelaySeconds, JobRetryBackoff Backoff, long? MaxDelaySeconds);

/// <summary>
/// 作业的一个触发器。
/// </summary>
/// <param name="Name">触发器名。</param>
/// <param name="Schedule">计划的文本写法（如 <c>cron 0 0 3 * * ?</c>、<c>every 01:00:00</c>）。</param>
/// <param name="Kind">计划的种类。</param>
/// <param name="Cron">Cron 表达式；不是 Cron 计划时为 <see langword="null"/>。</param>
/// <param name="IntervalSeconds">间隔秒数；不是间隔计划时为 <see langword="null"/>。</param>
/// <param name="TimeZone">Cron 计划的时区；缺省 UTC 时为 <see langword="null"/>。</param>
/// <param name="State">触发器状态。</param>
/// <param name="NextFireAt">下次触发时间。</param>
/// <param name="PreviousFireAt">上次触发时间。</param>
/// <param name="FireCount">已触发的次数。</param>
/// <param name="Error">触发器出错的原因；没出错为 <see langword="null"/>。</param>
public sealed record JobTriggerResponse(
    string Name,
    string Schedule,
    JobScheduleKind Kind,
    string? Cron,
    long? IntervalSeconds,
    string? TimeZone,
    TriggerState State,
    DateTimeOffset? NextFireAt,
    DateTimeOffset? PreviousFireAt,
    long FireCount,
    string? Error);

/// <summary>
/// 作业一次运行的结果。
/// </summary>
/// <param name="RunId">运行 ID。</param>
/// <param name="Trigger">怎么触发的：计划、手动、恢复或重试。</param>
/// <param name="Attempt">第几次尝试（从 1 起，重试的运行大于 1）。</param>
/// <param name="StartedAt">开始时间。</param>
/// <param name="CompletedAt">结束时间。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
/// <param name="Succeeded">是否成功。</param>
/// <param name="ErrorCode">失败的错误码；成功为 <see langword="null"/>。</param>
/// <param name="ErrorMessage">失败的原因；成功为 <see langword="null"/>。</param>
public sealed record JobOutcomeResponse(
    string RunId,
    JobTrigger Trigger,
    int Attempt,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    long DurationMs,
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage);

/// <summary>
/// 按作业名的批量结果。
/// </summary>
/// <param name="Succeeded">成功的作业名。</param>
/// <param name="Failed">失败的作业与原因。</param>
public sealed record JobBatchResult(IReadOnlyList<string> Succeeded, IReadOnlyList<JobBatchFailure> Failed);

/// <summary>
/// 批量里没成功的一个作业。
/// </summary>
/// <param name="Id">作业名。</param>
/// <param name="Code">错误码。</param>
public sealed record JobBatchFailure(string Id, string Code);

/// <summary>
/// 立即执行作业的结果。
/// </summary>
/// <param name="Trigger">这次排进去的一次性触发器名（运行历史里的触发器名与它相同）。</param>
public sealed record JobQueuedResponse(string Trigger);

/// <summary>
/// 校验计划的请求。
/// </summary>
/// <param name="Expression">计划：Cron 表达式（5 到 7 段），或计划的文本写法（<c>cron …</c>、<c>every …</c>、<c>once …</c>）。</param>
/// <param name="TimeZone">Cron 表达式的时区（如 <c>Asia/Shanghai</c>）；不填按 UTC。</param>
/// <param name="Count">要列出的下次运行时间个数。</param>
public sealed record ValidateScheduleRequest(
    [Required, StringLength(256, MinimumLength = 1)] string Expression,
    [StringLength(64)] string? TimeZone = null,
    [Range(1, 20)] int Count = 5);

/// <summary>
/// 校验计划的结果。
/// </summary>
/// <param name="Schedule">规范的文本写法。</param>
/// <param name="Kind">计划的种类。</param>
/// <param name="NextRuns">接下来的运行时间（UTC，按请求的个数，计划已结束则更少）。</param>
public sealed record ScheduleCheckResponse(string Schedule, JobScheduleKind Kind, IReadOnlyList<DateTimeOffset> NextRuns);
