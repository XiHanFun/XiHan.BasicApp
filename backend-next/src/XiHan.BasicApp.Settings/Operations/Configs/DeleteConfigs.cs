// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Configs;

/// <summary>
/// 批量删除参数：<c>DELETE /api/configs?ids=...</c>。逐项：看不见的 <c>SETTINGS_NOTFOUND_001</c>，另一侧的 <c>SETTINGS_DENIED_001</c>，内建的 <c>SETTINGS_CONFLICT_006</c>。
/// </summary>
/// <param name="configs">参数仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.ConfigsDelete)]
public sealed class DeleteConfigs(IRepository<SysConfig, Guid> configs) : IOperation<ConfigIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "configs.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(ConfigIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        var found = (await configs.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static c => c.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var config) || !SettingsScope.IsVisible(config, owner))
            {
                failed.Add(new BatchFailure(id, SettingsErrors.ConfigNotFound));
                continue;
            }

            if (!SettingsScope.IsOwnedBy(config, owner))
            {
                failed.Add(new BatchFailure(id, SettingsErrors.OtherSide));
                continue;
            }

            if (config.IsBuiltIn)
            {
                failed.Add(new BatchFailure(id, SettingsErrors.BuiltInProtected));
                continue;
            }

            var removed = await configs.RemoveAsync(config, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
