// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Editions;

/// <summary>
/// 套餐详情：<c>GET /api/editions/{id}</c>，带白名单。
/// </summary>
/// <param name="service">套餐域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.EditionsRead)]
public sealed class GetEdition(EditionService service) : IOperation<GetEditionRequest, EditionDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "editions.get";

    /// <inheritdoc />
    public async ValueTask<Result<EditionDetailResponse>> HandleAsync(GetEditionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!(await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var edition))
        {
            return Error.NotFound(TenancyErrors.EditionNotFound, TenancyMessages.EditionNotFound, MessageArguments.Of(("id", request.Id)));
        }

        return await EditionDetailResponse.LoadAsync(service, edition, context.CancellationToken).ConfigureAwait(false);
    }
}
