// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;

namespace XiHan.BasicApp.Maintenance.Operations.Jobs;

/// <summary>
/// <c>jobs.create</c> 的请求。
/// </summary>
/// <param name="Name">作业名（如 <c>reports.daily</c>，首段即分组）。</param>
/// <param name="JobType">作业类型的登记键（<c>jobs.types</c> 列出的）。</param>
/// <param name="Expression">计划：Cron 表达式（5 到 7 段），或计划的文本写法（<c>cron …</c>、<c>every …</c>、<c>once …</c>）。</param>
/// <param name="TimeZone">Cron 表达式的时区（如 <c>Asia/Shanghai</c>）；不填按 UTC。</param>
/// <param name="Data">作业参数（JSON 对象的文本）；不填即没有参数。</param>
/// <param name="AllowConcurrent">是否允许上一次没跑完时再跑。</param>
/// <param name="TimeoutSeconds">单次执行的时限（秒）；不填不限。超过即取消并记失败（<c>SCHEDULING_TIMEOUT_001</c>）。</param>
/// <param name="MaxRetries">失败后最多重试几次；不填不重试。</param>
/// <param name="RetryDelaySeconds">第一次重试前等几秒（设了重试次数才用）。</param>
/// <param name="RetryBackoff">重试的等待方式：每次相同或指数退避。</param>
/// <param name="RetryMaxDelaySeconds">指数退避的等待上限（秒）；不填只受 30 天约束。</param>
/// <param name="Description">说明。</param>
public sealed record CreateJobRequest(
    [Required, StringLength(JobName.MaxLength, MinimumLength = 1)] string Name,
    [Required, StringLength(256, MinimumLength = 1)] string JobType,
    [Required, StringLength(256, MinimumLength = 1)] string Expression,
    [StringLength(64)] string? TimeZone = null,
    [StringLength(65_536)] string? Data = null,
    bool AllowConcurrent = false,
    [Range(1, JobViews.MaxSeconds)] long? TimeoutSeconds = null,
    [Range(1, JobRetryPolicy.MaxRetriesLimit)] int? MaxRetries = null,
    [Range(1, JobViews.MaxSeconds)] long RetryDelaySeconds = 60,
    JobRetryBackoff RetryBackoff = JobRetryBackoff.Fixed,
    [Range(1, JobViews.MaxSeconds)] long? RetryMaxDelaySeconds = null,
    [StringLength(500)] string? Description = null);

/// <summary>
/// 新建运行时作业。
/// </summary>
/// <remarks>
/// <c>POST /api/jobs</c>（平台侧，照 4.x 的新建任务，替代 4.x 的 DynamicJobWorker）。经框架 <c>ScheduleAsync</c> 运行时登记：
/// 类型须在作业类型登记里（否则 <c>MAINTENANCE_CONFLICT_001</c>），同名作业已存在 <c>MAINTENANCE_CONFLICT_003</c>，
/// 作业名不合规 <c>MAINTENANCE_VALIDATION_004</c>，参数不是 JSON 对象 <c>MAINTENANCE_VALIDATION_005</c>，计划写得不对 <c>MAINTENANCE_VALIDATION_002</c>。
/// </remarks>
/// <param name="scheduler">框架调度。</param>
/// <param name="types">作业类型登记。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.JobsCreate)]
public sealed class CreateJob(IJobScheduler scheduler, JobTypeRegistry types) : IOperation<CreateJobRequest, JobResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.create";

    /// <inheritdoc />
    public async ValueTask<Result<JobResponse>> HandleAsync(CreateJobRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!JobName.IsValid(request.Name))
        {
            return Error.Validation(MaintenanceErrors.JobNameInvalid, MaintenanceMessages.JobNameInvalid, MessageArguments.Of(("name", request.Name)));
        }

        if (!JobViews.TryParse(request.Expression, request.TimeZone, out var schedule))
        {
            return Error.Validation(MaintenanceErrors.ScheduleInvalid, MaintenanceMessages.ScheduleInvalid, MessageArguments.Of(("expression", request.Expression)));
        }

        if (!types.Find(request.JobType).TryGetValue(out var jobType))
        {
            return Error.Conflict(MaintenanceErrors.JobTypeUnknown, MaintenanceMessages.JobTypeUnknown, MessageArguments.Of(("id", request.Name), ("type", request.JobType)));
        }

        JsonObject? data = null;
        if (!string.IsNullOrWhiteSpace(request.Data))
        {
            try
            {
                data = JsonNode.Parse(request.Data) as JsonObject;
            }
            catch (JsonException)
            {
                data = null;
            }

            if (data is null)
            {
                return Error.Validation(MaintenanceErrors.JobDataInvalid, MaintenanceMessages.JobDataInvalid);
            }
        }

        var name = new JobName(request.Name);
        var existing = await scheduler.StatusAsync(name, context.CancellationToken).ConfigureAwait(false);
        if (!existing.TryGetValue(out var maybe))
        {
            return existing.Error!;
        }

        if (maybe.HasValue)
        {
            return Error.Conflict(MaintenanceErrors.JobExists, MaintenanceMessages.JobExists, MessageArguments.Of(("id", request.Name)));
        }

        var definition = new JobDefinition(name, jobType)
        {
            Triggers = [new JobTriggerDefinition(JobTriggerDefinition.DefaultName, schedule)],
            Data = data,
            AllowConcurrent = request.AllowConcurrent,
            Retry = JobViews.RetryOf(request.MaxRetries, request.RetryDelaySeconds, request.RetryBackoff, request.RetryMaxDelaySeconds),
            Timeout = JobViews.TimeoutOf(request.TimeoutSeconds),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        };
        var scheduled = (await scheduler.ScheduleAsync([definition], context.CancellationToken).ConfigureAwait(false))[0];
        if (scheduled.IsFailure)
        {
            return scheduled.Error!;
        }

        var created = await scheduler.StatusAsync(name, context.CancellationToken).ConfigureAwait(false);
        return created.TryGetValue(out var after) && after.TryGetValue(out var status) ? JobViews.From(status, context) : JobViews.NotFound(request.Name);
    }
}

/// <summary>
/// 删除运行时作业。
/// </summary>
/// <remarks>
/// <c>DELETE /api/jobs</c>（平台侧）。只删运行时登记的作业（框架 <c>UnscheduleAsync</c>，连同触发器与等待中的运行）；
/// 代码登记的（含运行时改过计划的）逐项报 <c>MAINTENANCE_CONFLICT_004</c>，不存在的报 <c>MAINTENANCE_NOTFOUND_001</c>。
/// </remarks>
/// <param name="scheduler">框架调度。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.JobsDelete)]
public sealed class DeleteJobs(IJobScheduler scheduler) : IOperation<JobIdsRequest, JobBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "jobs.delete";

    /// <inheritdoc />
    public async ValueTask<Result<JobBatchResult>> HandleAsync(JobIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var succeeded = new List<string>();
        var failed = new List<JobBatchFailure>();
        var removable = new List<JobName>();
        foreach (var id in request.Ids.Distinct(StringComparer.Ordinal))
        {
            var found = JobName.IsValid(id) ? await scheduler.StatusAsync(new JobName(id), context.CancellationToken).ConfigureAwait(false) : Result.Success(Maybe.None<JobStatus>());
            if (!found.TryGetValue(out var maybe))
            {
                failed.Add(new JobBatchFailure(id, found.Error!.Code));
            }
            else if (!maybe.TryGetValue(out var status))
            {
                failed.Add(new JobBatchFailure(id, MaintenanceErrors.JobNotFound));
            }
            else if (status.Job.Source != JobSource.Runtime)
            {
                failed.Add(new JobBatchFailure(id, MaintenanceErrors.JobFromCode));
            }
            else
            {
                removable.Add(status.Job.Name);
            }
        }

        if (removable.Count > 0)
        {
            var outcomes = await scheduler.UnscheduleAsync(removable, context.CancellationToken).ConfigureAwait(false);
            for (var i = 0; i < removable.Count; i++)
            {
                if (outcomes[i].IsSuccess)
                {
                    succeeded.Add(removable[i].Value);
                }
                else
                {
                    failed.Add(new JobBatchFailure(removable[i].Value, outcomes[i].Error!.Code));
                }
            }
        }

        return new JobBatchResult(succeeded, failed);
    }
}
