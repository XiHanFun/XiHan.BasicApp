// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Workflow.Infrastructure.Stores;

/// <summary>
/// 引擎运行时行（实例、节点实例、书签）的写入
/// </summary>
/// <remarks>
/// 仓储的单实体更新带行版本乐观锁，要求实体携带库里当前的版本；运行时行每次都由引擎模型重新映射出来，
/// 版本是默认值，同一行从第二次更新起就会被当成并发冲突拒绝（办理、撤回、取消都要再更新挂起中的节点实例）。
/// 引擎对同一实例的操作在分布式锁内串行，存储只负责把引擎状态落库，所以这里取当前行的版本再更新。
/// </remarks>
internal static class WorkflowStoreWrites
{
    /// <summary>
    /// 按当前行的版本更新运行时行（行不存在时由仓储报错，不凭模型新建）
    /// </summary>
    /// <typeparam name="TEntity">运行时实体</typeparam>
    /// <param name="repository">仓储</param>
    /// <param name="entity">由引擎模型映射出的实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public static async Task UpdateRuntimeRowAsync<TEntity>(ISaasRepository<TEntity> repository, TEntity entity, CancellationToken cancellationToken)
        where TEntity : BasicAppEntity
    {
        var current = await repository.GetByIdAsync(entity.BasicId, cancellationToken);
        if (current is not null)
        {
            entity.RowVersion = current.RowVersion;
        }

        await repository.UpdateAsync(entity, cancellationToken);
    }
}
