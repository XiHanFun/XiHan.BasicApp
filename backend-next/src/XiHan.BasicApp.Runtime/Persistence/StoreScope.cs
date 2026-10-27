// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 在管道之外、独立作用域里开一个工作单元跑仓储：供 Authenticate 阶段的单例（会话门禁）、后台作业，以及必须在调用失败时也落库的写入（登录失败计数）使用。
/// 不经 <c>App.InvokeAsync</c>：嵌套调用会再过一遍 Tenant / Authorize 阶段，而且会随外层调用一起回滚。平台级数据不带租户。
/// </summary>
public static class StoreScope
{
    /// <summary>
    /// 只读工作单元：读完即提交（只读工作单元没有事务）。
    /// </summary>
    public static async Task<T> ReadAsync<T>(IServiceScopeFactory scopes, Func<IServiceProvider, CancellationToken, Task<T>> read, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(read);
        await using var scope = scopes.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions(IsReadOnly: true), cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await read(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
            (await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
            return result;
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

    /// <summary>
    /// 读写工作单元：委托返回成功即提交，返回失败或抛出即回滚；提交失败作为结果返回。
    /// </summary>
    public static async Task<Result> WriteAsync(IServiceScopeFactory scopes, Func<IServiceProvider, CancellationToken, Task<Result>> write, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(write);
        await using var scope = scopes.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(UnitOfWorkOptions.Default, cancellationToken).ConfigureAwait(false);
        try
        {
            var outcome = await write(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
            if (outcome.IsFailure)
            {
                await unitOfWork.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return outcome;
            }

            return await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
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
}
