// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Tenancy.Seeds;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Seeds;

/// <summary>
/// 演示组织种子。
/// </summary>
/// <remarks>
/// 号段 900 + 模块偏移 20，<c>BasicApp:Seed:EnableDemoData</c> 打开才写。
/// 在演示租户 <c>demo</c> 里建「总部 → 研发部 / 市场部」、岗位「经理 / 工程师」，并把演示管理员放进总部（经理、主部门）。
/// 部门是租户库实体，种子调用本身没有租户，所以经 <see cref="ITenantScopeRunner"/> 以 <c>demo</c> 另起一次调用写入；租户里已有部门即跳过（只首写）。
/// </remarks>
/// <param name="runner">按租户发起调用的运行器。</param>
/// <param name="options">种子配置。</param>
public sealed class DemoOrganizationSeeder(ITenantScopeRunner runner, SeedOptions options) : ISeeder
{
    /// <inheritdoc />
    public string Name => "organization.demo";

    /// <inheritdoc />
    public int Order => SeedOrders.Demo + (SeedOrders.ModuleOffset * 2);

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!options.EnableDemoData)
        {
            return Result.Success();
        }

        return await runner.RunAsync(DemoTenantSeeder.DemoTenantId, SeedTenantAsync, context.CancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<Result> SeedTenantAsync(InvocationContext context)
    {
        var departments = context.Services.GetRequiredService<IRepository<SysDepartment, Guid>>();
        if (await departments.CountAsync(EntityQuery.All<SysDepartment>(), context.CancellationToken).ConfigureAwait(false) > 0)
        {
            return Result.Success();
        }

        var service = context.Services.GetRequiredService<DepartmentService>();
        var positions = context.Services.GetRequiredService<IRepository<SysPosition, Guid>>();
        var memberships = context.Services.GetRequiredService<IRepository<SysUserDepartment, Guid>>();
        var users = context.Services.GetRequiredService<IRepository<SysUser, Guid>>();
        var ids = context.Services.GetRequiredService<IIdGenerator<Guid>>();

        var headquarters = new SysDepartment { Id = ids.NewId(), Code = "hq", Name = "Headquarters", Order = 0, Description = "Seeded when BasicApp:Seed:EnableDemoData is on." };
        var engineering = new SysDepartment { Id = ids.NewId(), Code = "rd", Name = "Research & Development", ParentId = headquarters.Id, Order = 10 };
        var marketing = new SysDepartment { Id = ids.NewId(), Code = "mkt", Name = "Marketing", ParentId = headquarters.Id, Order = 20 };
        foreach (var department in new[] { headquarters, engineering, marketing })
        {
            var added = await departments.AddAsync(department, context.CancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added;
            }

            var attached = await service.AttachAsync(department, context.CancellationToken).ConfigureAwait(false);
            if (attached.IsFailure)
            {
                return attached;
            }
        }

        var manager = new SysPosition { Id = ids.NewId(), Code = "manager", Name = "Manager", Order = 0 };
        var engineer = new SysPosition { Id = ids.NewId(), Code = "engineer", Name = "Engineer", Order = 10 };
        foreach (var outcome in await positions.AddRangeAsync([manager, engineer], context.CancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        var admins = await users.ListAsync(EntityQuery.All<SysUser>().Where(new Specification<SysUser>(u => u.HomeTenantId == DemoTenantSeeder.DemoTenantId && u.UserName == DemoTenantSeeder.DemoAdminUserName)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
        if (admins.Count == 0)
        {
            return Result.Success();
        }

        return await memberships.AddAsync(new SysUserDepartment { Id = ids.NewId(), UserId = admins[0].Id, DepartmentId = headquarters.Id, PositionId = manager.Id, IsPrimary = true, EmployeeNumber = "0001" }, context.CancellationToken).ConfigureAwait(false);
    }
}
