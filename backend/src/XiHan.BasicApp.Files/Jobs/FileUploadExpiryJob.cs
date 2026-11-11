// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Files.Jobs;

/// <summary>
/// 分片上传的过期处理
/// </summary>
/// <remarks>
/// 内建作业，每小时一次：平台与目录里的每个租户逐个作用域、逐批（一批一个工作单元）把过了时限的分片上传记为过期，放弃存储里的分片
/// 一个作用域失败只记日志、计入失败，不影响其他。操作里遇到过时限的上传也按过期回答，不等这个作业
/// </remarks>
/// <param name="sides">要处理的各侧</param>
/// <param name="scopes">作用域工厂</param>
/// <param name="logger">日志</param>
public sealed partial class FileUploadExpiryJob(TenantSides sides, IServiceScopeFactory scopes, ILogger<FileUploadExpiryJob> logger) : IJob
{
    /// <summary>
    /// 作业名
    /// </summary>
    public const string Name = "files.uploads.expire";

    /// <summary>
    /// 计划：每小时第 15 分钟
    /// </summary>
    public static JobSchedule Schedule { get; } = JobSchedule.FromCron("15 * * * *");

    /// <inheritdoc />
    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var cancellationToken = context.CancellationToken;
        var expired = 0;
        var failures = 0;
        var listedSides = await sides.ListAsync(cancellationToken).ConfigureAwait(false);
        if (!listedSides.TryGetValue(out var sideIds))
        {
            return listedSides.Error!;
        }

        foreach (var side in sideIds)
        {
            try
            {
                int batch;
                do
                {
                    batch = await InScopeAsync(side, cancellationToken).ConfigureAwait(false);
                    expired += batch;
                }
                while (batch == FileUploadService.ExpireBatchSize);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures++;
                LogExpireFailed(logger, exception, side);
            }
        }

        LogExpired(logger, expired, failures);
        return TenantSides.Outcome(Name, failures);
    }

    /// <summary>
    /// 在独立作用域、带租户的工作单元里处理一批
    /// </summary>
    /// <remarks>
    /// 成功即提交，抛出即回滚
    /// </remarks>
    private async Task<int> InScopeAsync(string side, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        // 独立库的租户：先按租户目录绑定分片，处理的是它自己的库
        (await scope.ServiceProvider.GetRequiredService<TenantShardScope>().BindTenantAsync(side, cancellationToken).ConfigureAwait(false)).ToResult().ThrowIfFailure();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions { TenantId = side }, cancellationToken).ConfigureAwait(false);
        try
        {
            var count = await scope.ServiceProvider.GetRequiredService<FileUploadService>().ExpireAsync(side, cancellationToken).ConfigureAwait(false);
            (await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
            return count;
        }
        catch
        {
            if (unitOfWork.State == UnitOfWorkState.Active)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "File upload expiry marked {Expired} uploads expired with {Failures} failures.")]
    private static partial void LogExpired(ILogger logger, int expired, int failures);

    [LoggerMessage(Level = LogLevel.Error, Message = "File upload expiry failed for side '{Side}'.")]
    private static partial void LogExpireFailed(ILogger logger, Exception exception, string side);
}
