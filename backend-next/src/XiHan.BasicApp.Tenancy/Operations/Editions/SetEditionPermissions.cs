// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.BasicApp.Tenancy.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Editions;

/// <summary>
/// 替换套餐白名单。
/// </summary>
/// <remarks>
/// <c>POST /api/editions/{id}:set-permissions</c>（平台侧，<c>tenancy.editions.update</c>）。
/// 只能放租户能生效的权限（<c>TENANCY_VALIDATION_002</c>）；写入后失效绑定该套餐的租户的门控缓存，其用户的有效权限随即收窄或放宽。
/// </remarks>
/// <param name="service">套餐域服务。</param>
/// <param name="gate">套餐门控。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.EditionsUpdate)]
public sealed class SetEditionPermissions(EditionService service, EditionGate gate) : IOperation<SetEditionPermissionsRequest, EditionDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "editions.set-permissions";

    /// <inheritdoc />
    public async ValueTask<Result<EditionDetailResponse>> HandleAsync(SetEditionPermissionsRequest request, InvocationContext context)
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

        var normalized = service.Normalize(request.Permissions);
        if (!normalized.TryGetValue(out var permissions))
        {
            return normalized.Error!;
        }

        var replaced = await service.ReplacePermissionsAsync(edition.Id, permissions, context.CancellationToken).ConfigureAwait(false);
        if (replaced.IsFailure)
        {
            return replaced.Error!;
        }

        await gate.InvalidateAsync(await service.TenantsUsingAsync(edition.Id, context.CancellationToken).ConfigureAwait(false), context.CancellationToken).ConfigureAwait(false);
        // 整体替换：日志记替换后的白名单
        context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.EditionPermissions) { EditionId = edition.Id, EditionCode = edition.Code, PermissionCodes = permissions.ToList() });
        return await EditionDetailResponse.LoadAsync(service, edition, context.CancellationToken).ConfigureAwait(false);
    }
}
