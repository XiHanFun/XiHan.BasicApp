// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;

namespace XiHan.BasicApp.OpenPlatform.Jobs;

/// <summary>
/// 清理过期的授权码与令牌（每天一次；4.x 没有清理，记录越积越多）：授权码过期一天、令牌过期七天后物理删除（重放与吊销的判定只看没过期的）。
/// </summary>
/// <param name="clock">时间源。</param>
public sealed class OAuthCleanupJob(TimeProvider clock) : IJob
{
    /// <summary>
    /// 作业名。
    /// </summary>
    public const string Name = "openplatform.oauth-cleanup";

    /// <inheritdoc />
    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        var codesBefore = now.AddDays(-1);
        var tokensBefore = now.AddDays(-7);
        var unitOfWork = context.Services.GetRequiredService<SqlSugarUnitOfWork>();
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        await unitOfWork.ConnectionFor(typeof(SysOAuthCode)).Deleteable<SysOAuthCode>().Where(c => c.ExpiresAt < codesBefore).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        await unitOfWork.ConnectionFor(typeof(SysOAuthToken)).Deleteable<SysOAuthToken>().Where(t => t.ExpiresAt < tokensBefore).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
