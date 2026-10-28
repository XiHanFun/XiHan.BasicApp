// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.NumberingRules;

/// <summary>
/// 批量删除编号规则：<c>DELETE /api/numbering-rules?ids=...</c>。逐项：看不见的 <c>SETTINGS_NOTFOUND_004</c>，另一侧的 <c>SETTINGS_DENIED_001</c>，
/// 发过号的 <c>SETTINGS_CONFLICT_009</c>（发号记录是永久事实，停用即可）。
/// </summary>
/// <param name="rules">规则仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.NumberingRulesDelete)]
public sealed class DeleteNumberingRules(IRepository<SysNumberingRule, Guid> rules) : IOperation<NumberingRuleIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "numbering-rules.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(NumberingRuleIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        var found = (await rules.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var rule) || !NumberingRuleService.IsVisible(rule, owner))
            {
                failed.Add(new BatchFailure(id, SettingsErrors.NumberingRuleNotFound));
                continue;
            }

            if (!SettingsScope.IsOwnedBy(rule, owner))
            {
                failed.Add(new BatchFailure(id, SettingsErrors.OtherSide));
                continue;
            }

            if (rule.HasAllocated)
            {
                failed.Add(new BatchFailure(id, SettingsErrors.NumberingRuleFrozen));
                continue;
            }

            var removed = await rules.RemoveAsync(rule, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
