// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Seeds;

/// <summary>
/// 演示租户种子（号段 900 + 模块偏移 10，<c>BasicApp:Seed:EnableDemoData</c> 打开才写）：租户 <c>demo</c>、它的管理员 <c>admin</c>（密码 <c>Demo@123456</c>，首次登录须改密），
/// 以及平台超级管理员在 <c>demo</c> 的成员身份（演示切换租户与模仿）。新建时绑定企业版套餐（套餐种子在前一阶段写入；没有企业版即不绑定）。运营数据只首写。
/// </summary>
/// <param name="tenants">租户仓储。</param>
/// <param name="members">成员仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="initializers">租户初始化扩展（Access 实现：内建角色与指派）。</param>
/// <param name="service">租户域服务。</param>
/// <param name="editions">套餐域服务。</param>
/// <param name="options">种子配置。</param>
/// <param name="ids">标识生成器。</param>
public sealed class DemoTenantSeeder(
    IRepository<SysTenant, Guid> tenants,
    IRepository<SysTenantMember, Guid> members,
    IRepository<SysUser, Guid> users,
    UserAccountService accounts,
    IEnumerable<ITenantInitializer> initializers,
    TenantService service,
    EditionService editions,
    SeedOptions options,
    IIdGenerator<Guid> ids) : ISeeder
{
    /// <summary>
    /// 演示租户 ID。
    /// </summary>
    public const string DemoTenantId = "demo";

    /// <summary>
    /// 演示租户管理员登录名。
    /// </summary>
    public const string DemoAdminUserName = "admin";

    /// <summary>
    /// 演示租户管理员初始密码。
    /// </summary>
    public const string DemoAdminPassword = "Demo@123456";

    /// <inheritdoc />
    public string Name => "tenancy.demo";

    /// <inheritdoc />
    public int Order => SeedOrders.Demo + SeedOrders.ModuleOffset;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!options.EnableDemoData)
        {
            return Result.Success();
        }

        SysTenant tenant;
        if ((await service.FindByTenantIdAsync(DemoTenantId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var found))
        {
            tenant = found;
        }
        else
        {
            var enterprise = await editions.FindByCodeAsync(EditionSeeder.Enterprise, context.CancellationToken).ConfigureAwait(false);
            tenant = new SysTenant
            {
                Id = ids.NewId(),
                TenantId = DemoTenantId,
                Name = "Demo Tenant",
                Description = "Seeded when BasicApp:Seed:EnableDemoData is on.",
                Order = 900,
                EditionId = enterprise.TryGetValue(out var edition) ? edition.Id : null,
            };
            var added = await tenants.AddAsync(tenant, context.CancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added;
            }

            var synced = await service.SyncDirectoryAsync(tenant, context.CancellationToken).ConfigureAwait(false);
            if (synced.IsFailure)
            {
                return synced;
            }
        }

        // 平台超级管理员是演示租户的成员：切换租户、在其中发起模仿都要它；只首写
        if ((await accounts.FindByNameAsync(TenantIds.Platform, SeedOptions.SuperAdminUserName, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var superAdmin))
        {
            var superAdminId = superAdmin.Id;
            var existing = await members.CountAsync(EntityQuery.All<SysTenantMember>().Where(new Specification<SysTenantMember>(m => m.TenantId == DemoTenantId && m.UserId == superAdminId)), context.CancellationToken).ConfigureAwait(false);
            if (existing == 0)
            {
                var joined = await members.AddAsync(new SysTenantMember { Id = ids.NewId(), TenantId = DemoTenantId, UserId = superAdminId, MemberType = TenantMemberType.Owner }, context.CancellationToken).ConfigureAwait(false);
                if (joined.IsFailure)
                {
                    return joined;
                }
            }
        }

        if (tenant.AdminUserId is not null)
        {
            return Result.Success();
        }

        var admin = new SysUser { Id = ids.NewId(), UserName = DemoAdminUserName, DisplayName = "Demo Admin", HomeTenantId = DemoTenantId, Status = UserStatus.Active };
        var userAdded = await users.AddAsync(admin, context.CancellationToken).ConfigureAwait(false);
        if (userAdded.IsFailure)
        {
            return userAdded;
        }

        var securityResult = await accounts.SecurityOfAsync(admin.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.ToResult();
        }

        var set = await accounts.SetPasswordAsync(security, DemoAdminPassword, mustChange: true, context.CancellationToken).ConfigureAwait(false);
        if (set.IsFailure)
        {
            return set;
        }

        var ran = false;
        foreach (var initializer in initializers)
        {
            ran = true;
            var initialized = await initializer.InitializeAsync(DemoTenantId, admin.Id, context).ConfigureAwait(false);
            if (initialized.IsFailure)
            {
                return initialized;
            }
        }

        if (!ran)
        {
            return Error.Unavailable(TenancyErrors.NoInitializer, TenancyMessages.NoInitializer);
        }

        tenant.AdminUserId = admin.Id;
        return await tenants.UpdateAsync(tenant, context.CancellationToken).ConfigureAwait(false);
    }
}
