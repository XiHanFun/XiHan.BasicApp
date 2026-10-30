// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 转移租户所有权。
/// </summary>
/// <remarks>
/// <c>POST /api/tenants/{id}:transfer-owner</c>（平台侧）。所有者即 <see cref="SysTenant.AdminUserId"/>，每租户唯一，转移即改写它（照 4.x 的所有权转移）：
/// 新所有者须是该租户注册地的用户或生效中的普通成员（运维支持成员不行），启用且未锁定；新所有者经 <see cref="ITenantInitializer"/> 拿到管理员身份，
/// 原所有者保留已有角色（降为管理员），两人的授权缓存随即失效。转移记一条权限变更日志（含原 / 新所有者）。
/// </remarks>
/// <param name="tenants">租户仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="members">成员仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="initializers">租户初始化扩展（Access 实现：管理员角色与授权缓存）。</param>
/// <param name="service">租户域服务。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsTransferOwner)]
public sealed class TransferOwner(
    IRepository<SysTenant, Guid> tenants,
    IRepository<SysUser, Guid> users,
    IRepository<SysTenantMember, Guid> members,
    UserAccountService accounts,
    IEnumerable<ITenantInitializer> initializers,
    TenantService service,
    TimeProvider clock) : IOperation<TransferOwnerRequest, TenantResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.transfer-owner";

    /// <inheritdoc />
    public async ValueTask<Result<TenantResponse>> HandleAsync(TransferOwnerRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!(await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var tenant))
        {
            return Error.NotFound(TenancyErrors.TenantNotFound, TenancyMessages.TenantNotFound, MessageArguments.Of(("id", request.Id)));
        }

        if (tenant.AdminUserId is not { } previousId)
        {
            return Error.Conflict(TenancyErrors.OwnerMissing, TenancyMessages.OwnerMissing, MessageArguments.Of(("tenantId", tenant.TenantId)));
        }

        if (previousId == request.UserId)
        {
            return Error.Conflict(TenancyErrors.AlreadyOwner, TenancyMessages.AlreadyOwner, MessageArguments.Of(("tenantId", tenant.TenantId), ("userId", request.UserId)));
        }

        var eligible = await EligibleAsync(tenant.TenantId, request.UserId, context.CancellationToken).ConfigureAwait(false);
        if (!eligible.TryGetValue(out var owner))
        {
            return Error.Validation(TenancyErrors.OwnerIneligible, TenancyMessages.OwnerIneligible, MessageArguments.Of(("tenantId", tenant.TenantId), ("userId", request.UserId)));
        }

        tenant.AdminUserId = owner.Id;
        var updated = await tenants.UpdateAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var ran = false;
        foreach (var initializer in initializers)
        {
            ran = true;
            var transferred = await initializer.TransferOwnerAsync(tenant.TenantId, previousId, owner.Id, context).ConfigureAwait(false);
            if (transferred.IsFailure)
            {
                return transferred.Error!;
            }
        }

        if (!ran)
        {
            return Error.Unavailable(TenancyErrors.NoInitializer, TenancyMessages.NoInitializer);
        }

        var previousName = (await users.FindAsync(previousId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var previous) ? previous.UserName : null;
        context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.TenantOwner)
        {
            TargetTenantId = tenant.TenantId,
            TargetUserId = owner.Id,
            TargetUserName = owner.UserName,
            Detail = $"from:{previousName ?? previousId.ToString("D")} ({previousId:D}) to:{owner.UserName} ({owner.Id:D})",
        });
        return TenantResponse.From(tenant);
    }

    /// <summary>
    /// 新所有者是否合格。
    /// </summary>
    /// <remarks>
    /// 注册地在该租户的用户，或生效中的普通成员；启用且没有处在锁定期。
    /// </remarks>
    private async Task<Maybe<SysUser>> EligibleAsync(string tenantId, Guid userId, CancellationToken cancellationToken)
    {
        if (!(await users.FindAsync(userId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var user) || user.Status != UserStatus.Active)
        {
            return Maybe.None<SysUser>();
        }

        var now = clock.GetUtcNow();
        if (!string.Equals(user.HomeTenantId, tenantId, StringComparison.Ordinal))
        {
            var rows = await members.ListAsync(EntityQuery.All<SysTenantMember>().Where(new Specification<SysTenantMember>(m => m.TenantId == tenantId && m.UserId == userId && m.MemberType == TenantMemberType.Member)), cancellationToken).ConfigureAwait(false);
            if (!rows.Any(m => m.IsInEffect(now)))
            {
                return Maybe.None<SysUser>();
            }
        }

        var security = await accounts.SecurityOfAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return security.TryGetValue(out var record) && record.LockoutEndAt is { } until && until > now ? Maybe.None<SysUser>() : Maybe.Some(user);
    }
}
