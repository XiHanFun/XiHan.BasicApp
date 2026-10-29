// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Dicts;

/// <summary>
/// 新建字典。
/// </summary>
/// <remarks>
/// <c>POST /api/dicts</c>。写在调用方这一侧；字典不覆盖，平台建字典要求全库没有同码，租户建字典要求平台与本租户都没有同码。
/// </remarks>
/// <param name="dicts">字典仓储。</param>
/// <param name="service">字典域服务。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.DictsCreate)]
public sealed class CreateDict(IRepository<SysDict, Guid> dicts, DictService service, IIdGenerator<Guid> ids) : IOperation<CreateDictRequest, DictResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "dicts.create";

    /// <inheritdoc />
    public async ValueTask<Result<DictResponse>> HandleAsync(CreateDictRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        var code = request.Code.Trim();
        if (await service.IsCodeTakenAsync(code, owner, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(SettingsErrors.DictCodeTaken, SettingsMessages.DictCodeTaken, MessageArguments.Of(("code", code)));
        }

        var dict = new SysDict
        {
            Id = ids.NewId(),
            OwnerTenantId = owner,
            Code = code,
            Name = request.Name.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Order = request.Order,
        };
        var added = await dicts.AddAsync(dict, context.CancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : DictResponse.From(dict);
    }
}
