// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.BasicApp.Settings.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Dicts;

/// <summary>
/// 修改字典
/// </summary>
/// <remarks>
/// <c>PUT /api/dicts/{id}</c>（名称、分类、说明、排序、状态；编码不可改）。只能改本侧的，另一侧 <c>SETTINGS_DENIED_001</c>
/// </remarks>
/// <param name="dicts">字典仓储</param>
/// <param name="service">字典域服务</param>
/// <param name="sets">字典选项缓存</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.DictsUpdate)]
public sealed class UpdateDict(IRepository<SysDict, Guid> dicts, DictService service, DictOptionSets sets) : IOperation<UpdateDictRequest, DictResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "dicts.update";

    /// <inheritdoc />
    public async ValueTask<Result<DictResponse>> HandleAsync(UpdateDictRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        if (!(await service.FindVisibleAsync(request.Id, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var dict))
        {
            return Error.NotFound(SettingsErrors.DictNotFound, SettingsMessages.DictNotFound, MessageArguments.Of(("id", request.Id)));
        }

        if (!SettingsScope.IsOwnedBy(dict, owner))
        {
            return Error.Denied(SettingsErrors.OtherSide, SettingsMessages.OtherSide, MessageArguments.Of(("id", request.Id), ("name", dict.Name)));
        }

        dict.Name = request.Name.Trim();
        dict.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        dict.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        dict.Order = request.Order;
        dict.Status = request.Status;
        var updated = await dicts.UpdateAsync(dict, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await sets.InvalidateAsync(dict.OwnerTenantId, dict.Code, context.CancellationToken).ConfigureAwait(false);
        return DictResponse.From(dict);
    }
}
