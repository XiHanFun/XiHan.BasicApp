// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 初始化租户管理员：<c>POST /api/tenants/{id}:initialize-admin</c>。在租户注册地建用户、设初始密码（首次登录须改密），
/// 并以租户范围的用户授予 <c>*</c> 给到该租户侧的全部权限（Tenancy 不能引用 Access，角色化的租户管理员随 Access 的租户种子加入）。每个租户只能初始化一次。
/// </summary>
/// <param name="tenants">租户仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="initializers">租户初始化扩展（Access 实现：内建角色与指派）。</param>
/// <param name="service">租户域服务。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsInitializeAdmin)]
public sealed class InitializeAdmin(
    IRepository<SysTenant, Guid> tenants,
    IRepository<SysUser, Guid> users,
    UserAccountService accounts,
    IEnumerable<ITenantInitializer> initializers,
    TenantService service,
    IIdGenerator<Guid> ids) : IOperation<InitializeAdminRequest, UserResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.initialize-admin";

    /// <inheritdoc />
    public async ValueTask<Result<UserResponse>> HandleAsync(InitializeAdminRequest request, InvocationContext context)
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

        if (tenant.AdminUserId is not null)
        {
            return Error.Conflict(TenancyErrors.AdminInitialized, TenancyMessages.AdminInitialized, MessageArguments.Of(("tenantId", tenant.TenantId)));
        }

        var userName = request.UserName.Trim();
        if ((await accounts.FindByNameAsync(tenant.TenantId, userName, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Conflict(TenancyErrors.AdminNameTaken, TenancyMessages.AdminNameTaken, MessageArguments.Of(("userName", userName)));
        }

        var admin = new SysUser
        {
            Id = ids.NewId(),
            UserName = userName,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? userName : request.DisplayName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            HomeTenantId = tenant.TenantId,
            Status = UserStatus.Active,
        };
        var added = await users.AddAsync(admin, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var securityResult = await accounts.SecurityOfAsync(admin.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        var set = await accounts.SetPasswordAsync(security, request.Password, mustChange: true, context.CancellationToken).ConfigureAwait(false);
        if (set.IsFailure)
        {
            return set.Error!;
        }

        // 管理员身份由上层包准备（Access：内建租户角色 admin 持 *，并指派）；没有任何实现时管理员会没有权限，直接失败
        var ran = false;
        foreach (var initializer in initializers)
        {
            ran = true;
            var initialized = await initializer.InitializeAsync(tenant.TenantId, admin.Id, context).ConfigureAwait(false);
            if (initialized.IsFailure)
            {
                return initialized.Error!;
            }
        }

        if (!ran)
        {
            return Error.Unavailable(TenancyErrors.NoInitializer, TenancyMessages.NoInitializer);
        }

        tenant.AdminUserId = admin.Id;
        var updated = await tenants.UpdateAsync(tenant, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : UserResponse.From(admin);
    }
}
