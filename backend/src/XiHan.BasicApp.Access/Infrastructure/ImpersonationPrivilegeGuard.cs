// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 模仿的权限上限
/// </summary>
/// <remarks>
/// 不能模仿同级或更高权限的账号
/// 平台侧持 <c>*</c> 的超级管理员不比较；别的发起人不能模仿持 <c>*</c> 的账号，目标在目标上下文持有的权限码都要在发起人手里
/// 平台账号的授予与租户侧的权限码对不上，所以平台上权限受限的账号模仿不了租户里持有租户侧权限的人
/// </remarks>
/// <param name="evaluator">授予全貌</param>
/// <param name="catalog">权限目录</param>
/// <param name="clock">时间源</param>
public sealed class ImpersonationPrivilegeGuard(GrantEvaluator evaluator, PermissionCatalog catalog, TimeProvider clock) : IImpersonationGuard
{
    /// <inheritdoc />
    public async Task<Result<bool>> AllowsAsync(SecurityContext actor, Guid targetId, string? tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var viewed = await evaluator.ViewAsync(actor, cancellationToken).ConfigureAwait(false);
        if (!viewed.TryGetValue(out var found))
        {
            return viewed.Error!;
        }

        if (!found.TryGetValue(out var mine))
        {
            return false;
        }

        if (actor.Tenant is null && mine.Allows(PermissionGrant.All))
        {
            return true;
        }

        var theirs = await evaluator.ViewOfAsync(tenantId ?? TenantIds.Platform, targetId, clock.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        if (!theirs.TryGetValue(out var target))
        {
            return theirs.Error!;
        }

        return !target.Allows(PermissionGrant.All) && catalog.ForSide(platformSide: tenantId is null).All(p => !target.Allows(p.Code) || mine.Allows(p.Code));
    }
}
