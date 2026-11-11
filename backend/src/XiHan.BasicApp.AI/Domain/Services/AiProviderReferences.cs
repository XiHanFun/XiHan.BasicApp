// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Domain.Services;

/// <summary>
/// 提供商被谁引用
/// </summary>
/// <remarks>
/// 被引用的提供商不能删除（方案 D5）：助手在平台库，知识文档是租户库实体（方案 A2），要逐个侧查：平台与目录里的每个租户，独立库的租户绑定分片后查它自己的库；
/// 删提供商是少见的平台操作，这个开销可以接受。任何一侧查不了即失败，不当作没有引用
/// </remarks>
/// <param name="assistants">助手仓储</param>
/// <param name="sides">要查的各侧</param>
/// <param name="scopes">作用域工厂</param>
/// <param name="logger">日志</param>
public sealed partial class AiProviderReferences(IRepository<SysAiAssistant, Guid> assistants, TenantSides sides, IServiceScopeFactory scopes, ILogger<AiProviderReferences> logger)
{
    /// <summary>
    /// 这些编码里被引用的那些
    /// </summary>
    /// <param name="codes">提供商的编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被引用的编码</returns>
    public async Task<Result<IReadOnlySet<string>>> FindAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codes);
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        if (codes.Count == 0)
        {
            return referenced;
        }

        var configured = await assistants.ListAsync(EntityQuery.All<SysAiAssistant>().Where(new Specification<SysAiAssistant>(a => codes.Contains(a.ProviderCode!) || codes.Contains(a.KnowledgeProviderCode!))), cancellationToken).ConfigureAwait(false);
        foreach (var assistant in configured)
        {
            referenced.UnionWith(new[] { assistant.ProviderCode, assistant.KnowledgeProviderCode }.OfType<string>().Where(codes.Contains));
        }

        var listedSides = await sides.ListAsync(cancellationToken).ConfigureAwait(false);
        if (!listedSides.TryGetValue(out var sideIds))
        {
            return listedSides.Error!;
        }

        foreach (var side in sideIds)
        {
            var found = await FindInAsync(side, codes, cancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var items))
            {
                return found.Error!;
            }

            referenced.UnionWith(items);
        }

        return referenced;
    }

    private async Task<Result<List<string>>> FindInAsync(string side, IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var bound = await scope.ServiceProvider.GetRequiredService<TenantShardScope>().BindTenantAsync(side, cancellationToken).ConfigureAwait(false);
        if (bound.IsFailure)
        {
            return bound.Error!;
        }

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions { IsReadOnly = true, TenantId = side }, cancellationToken).ConfigureAwait(false);
        try
        {
            var found = await scope.ServiceProvider.GetRequiredService<SideRows>().Query<SysKnowledgeDocument>(side)
                .Where(d => codes.Contains(d.EmbeddingProviderCode!))
                .Select(d => d.EmbeddingProviderCode!)
                .Distinct()
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            (await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
            return found;
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            await RollbackAsync(unitOfWork).ConfigureAwait(false);
            LogSideUnreadable(logger, exception, side);
            return Error.Unavailable(AIErrors.ProviderReferencesUnavailable, AIMessages.ProviderReferencesUnavailable, MessageArguments.Of(("side", side)));
        }
        catch
        {
            await RollbackAsync(unitOfWork).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task RollbackAsync(IUnitOfWork unitOfWork)
    {
        if (unitOfWork.State == UnitOfWorkState.Active)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Reading the provider references of side '{Side}' failed.")]
    private static partial void LogSideUnreadable(ILogger logger, Exception exception, string side);
}
