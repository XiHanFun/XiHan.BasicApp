// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Editions;

/// <summary>
/// 新建套餐：<c>POST /api/editions</c>（平台侧）。编码唯一（<c>TENANCY_CONFLICT_006</c>）；白名单只能放租户能生效的权限（<c>TENANCY_VALIDATION_002</c>）；设为缺省时清掉其余套餐的缺省标记。
/// </summary>
/// <param name="editions">套餐仓储。</param>
/// <param name="service">套餐域服务。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.EditionsCreate)]
public sealed class CreateEdition(IRepository<SysEdition, Guid> editions, EditionService service, IIdGenerator<Guid> ids) : IOperation<CreateEditionRequest, EditionDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "editions.create";

    /// <inheritdoc />
    public async ValueTask<Result<EditionDetailResponse>> HandleAsync(CreateEditionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var code = request.Code.Trim();
        if ((await service.FindByCodeAsync(code, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Conflict(TenancyErrors.EditionCodeTaken, TenancyMessages.EditionCodeTaken, MessageArguments.Of(("code", code)));
        }

        var normalized = service.Normalize(request.Permissions ?? []);
        if (!normalized.TryGetValue(out var permissions))
        {
            return normalized.Error!;
        }

        var edition = new SysEdition
        {
            Id = ids.NewId(),
            Code = code,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsDefault = request.IsDefault,
            Order = request.Order,
        };
        var added = await editions.AddAsync(edition, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var replaced = await service.ReplacePermissionsAsync(edition.Id, permissions, context.CancellationToken).ConfigureAwait(false);
        if (replaced.IsFailure)
        {
            return replaced.Error!;
        }

        if (edition.IsDefault)
        {
            var cleared = await service.ClearOtherDefaultsAsync(edition.Id, context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        return await EditionDetailResponse.LoadAsync(service, edition, context.CancellationToken).ConfigureAwait(false);
    }
}
