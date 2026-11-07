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
/// 演示组织种子
/// </summary>
/// <remarks>
/// 号段 900 + 模块偏移 20，<c>BasicApp:Seed:EnableDemoData</c> 打开才写
/// 在演示租户 <c>demo</c> 里建「总部 → 研发部 / 市场部 / 人事部」、岗位「总经理 / 经理 / 工程师 / 专员」与负责人，并给流程演示要用的成员排主部门任职：
/// 管理员 <c>admin</c> 是总经理、总部负责人；<c>manager</c> 是研发部经理、研发部负责人；<c>lead</c> 是研发部工程师；<c>hr</c> 是人事部专员
/// 部门是租户库实体，种子调用本身没有租户，所以经 <see cref="ITenantScopeRunner"/> 以 <c>demo</c> 另起一次调用写入；租户里已有部门即跳过（只首写）
/// </remarks>
/// <param name="runner">按租户发起调用的运行器</param>
/// <param name="options">种子配置</param>
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

        // 演示成员按登录名取（账号由演示租户种子在前一阶段写入；缺了的就不排任职、不当负责人）
        string[] names = [DemoTenantSeeder.DemoAdminUserName, .. DemoTenantSeeder.DemoStaff.Select(static s => s.UserName)];
        var found = (await users.ListAsync(EntityQuery.All<SysUser>().Where(new Specification<SysUser>(u => u.HomeTenantId == DemoTenantSeeder.DemoTenantId && names.Contains(u.UserName))), context.CancellationToken).ConfigureAwait(false))
            .ToDictionary(static u => u.UserName, static u => u.Id, StringComparer.Ordinal);
        Guid? UserOf(string userName) => found.TryGetValue(userName, out var id) ? id : null;

        var headquarters = new SysDepartment { Id = ids.NewId(), Code = "hq", Name = "Headquarters", DepartmentType = DepartmentType.Company, Order = 0, LeaderUserId = UserOf(DemoTenantSeeder.DemoAdminUserName), Description = "Seeded when BasicApp:Seed:EnableDemoData is on." };
        var engineering = new SysDepartment { Id = ids.NewId(), Code = "rd", Name = "Research & Development", DepartmentType = DepartmentType.Center, ParentId = headquarters.Id, Order = 10, LeaderUserId = UserOf("manager") };
        var marketing = new SysDepartment { Id = ids.NewId(), Code = "mkt", Name = "Marketing", ParentId = headquarters.Id, Order = 20 };
        var humanResources = new SysDepartment { Id = ids.NewId(), Code = "hr", Name = "Human Resources", ParentId = headquarters.Id, Order = 30 };
        foreach (var department in new[] { headquarters, engineering, marketing, humanResources })
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

        var generalManager = new SysPosition { Id = ids.NewId(), Code = "gm", Name = "General Manager", Order = 0 };
        var manager = new SysPosition { Id = ids.NewId(), Code = "manager", Name = "Manager", Order = 10 };
        var engineer = new SysPosition { Id = ids.NewId(), Code = "engineer", Name = "Engineer", Order = 20 };
        var specialist = new SysPosition { Id = ids.NewId(), Code = "specialist", Name = "Specialist", Order = 30 };
        foreach (var outcome in await positions.AddRangeAsync([generalManager, manager, engineer, specialist], context.CancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        (string UserName, SysDepartment Department, SysPosition Position, string Number)[] staff =
        [
            (DemoTenantSeeder.DemoAdminUserName, headquarters, generalManager, "0001"),
            ("manager", engineering, manager, "0002"),
            ("lead", engineering, engineer, "0003"),
            ("hr", humanResources, specialist, "0004"),
        ];
        var rows = staff
            .Where(s => found.ContainsKey(s.UserName))
            .Select(s => new SysUserDepartment { Id = ids.NewId(), UserId = found[s.UserName], DepartmentId = s.Department.Id, PositionId = s.Position.Id, IsPrimary = true, EmployeeNumber = s.Number })
            .ToList();
        if (rows.Count == 0)
        {
            return Result.Success();
        }

        foreach (var outcome in await memberships.AddRangeAsync(rows, context.CancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }
}
