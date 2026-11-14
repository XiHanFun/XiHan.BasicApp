// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Organization;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Tenancy;
using XiHan.BasicApp.Tenancy.Seeds;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Seeds;

/// <summary>
/// 演示角色种子
/// </summary>
/// <remarks>
/// 号段 900 + 模块偏移 20 + 5（排在演示组织之后），<c>BasicApp:Seed:EnableDemoData</c> 打开才写。在演示租户 <c>demo</c> 里建两个角色并指派：
/// 普通员工 <c>employee</c>（只看本人数据，工作流的查看与发起、聊天的查看与发送）给 <c>manager</c>、<c>lead</c>、<c>hr</c>；
/// 人事专员 <c>hr_specialist</c>（自定义数据范围：研发部与市场部，成员与部门归属的日常维护）给 <c>hr</c>
/// 可选模块的权限码按字符串写，只授出权限目录里已登记、租户一侧可用的那些（模块关掉即跳过）。角色按编码只首写，删掉的不补
/// </remarks>
/// <param name="roles">角色仓储</param>
/// <param name="members">指派仓储</param>
/// <param name="scopes">自定义数据范围仓储</param>
/// <param name="users">用户仓储</param>
/// <param name="grants">角色授予</param>
/// <param name="catalog">权限目录</param>
/// <param name="runner">按租户发起调用的运行器（读演示部门）</param>
/// <param name="options">种子配置</param>
/// <param name="ids">标识生成器</param>
public sealed class DemoAccessSeeder(
    IRepository<SysRole, Guid> roles,
    IRepository<SysUserRole, Guid> members,
    IRepository<SysRoleDataScope, Guid> scopes,
    IRepository<SysUser, Guid> users,
    RoleGrants grants,
    PermissionCatalog catalog,
    ITenantScopeRunner runner,
    SeedOptions options,
    IIdGenerator<Guid> ids) : ISeeder
{
    /// <summary>
    /// 普通员工的角色编码
    /// </summary>
    public const string EmployeeRoleCode = "employee";

    /// <summary>
    /// 人事专员的角色编码
    /// </summary>
    public const string HrSpecialistRoleCode = "hr_specialist";

    /// <inheritdoc />
    public string Name => "access.demo";

    /// <inheritdoc />
    public int Order => SeedOrders.Demo + (SeedOrders.ModuleOffset * 2) + 5;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!options.EnableDemoData)
        {
            return Result.Success();
        }

        const string tenant = DemoTenantSeeder.DemoTenantId;
        string[] staff = ["manager", "lead", "hr"];
        var found = (await users.ListAsync(EntityQuery.All<SysUser>().Where(new Specification<SysUser>(u => u.HomeTenantId == tenant && staff.Contains(u.UserName))), context.CancellationToken).ConfigureAwait(false))
            .ToDictionary(static u => u.UserName, static u => u.Id, StringComparer.Ordinal);

        var employee = await EnsureRoleAsync(
            new SysRole { Id = ids.NewId(), OwnerTenantId = tenant, Code = EmployeeRoleCode, Name = "普通员工", Description = "只看本人数据；可查看与发起流程、使用聊天。", DataScope = DataScope.SelfOnly, Order = 20 },
            ["workflow.workflow-definitions.read", "workflow.workflow-instances.read", "workflow.workflow-instances.execute", "chat.chat-conversations.read", "chat.chat-messages.read", "chat.chat-messages.send", "chat.chat-calls.read", "chat.chat-calls.call"],
            [.. staff.Where(found.ContainsKey).Select(name => found[name])],
            [],
            context).ConfigureAwait(false);
        if (employee.IsFailure)
        {
            return employee;
        }

        var departments = await DepartmentsAsync(["rd", "mkt"], context).ConfigureAwait(false);
        if (!departments.TryGetValue(out var custom))
        {
            return departments.ToResult();
        }

        return await EnsureRoleAsync(
            new SysRole { Id = ids.NewId(), OwnerTenantId = tenant, Code = HrSpecialistRoleCode, Name = "人事专员", Description = "自定义数据范围：研发中心与市场部；维护成员与部门归属。", DataScope = DataScope.Custom, Order = 40 },
            [IdentityPermissions.UsersRead, IdentityPermissions.UsersCreate, IdentityPermissions.UsersUpdate, OrganizationPermissions.DepartmentsRead, OrganizationPermissions.PositionsRead, OrganizationPermissions.MembershipsRead, OrganizationPermissions.MembershipsCreate, OrganizationPermissions.MembershipsUpdate, TenancyPermissions.TenantMembersRead],
            found.TryGetValue("hr", out var hr) ? [hr] : [],
            custom,
            context).ConfigureAwait(false);
    }

    private async Task<Result> EnsureRoleAsync(SysRole role, IReadOnlyList<string> permissions, IReadOnlyList<Guid> holders, IReadOnlyList<Guid> customDepartments, InvocationContext context)
    {
        var existing = EntityQuery.All<SysRole>().Where(new Specification<SysRole>(r => r.OwnerTenantId == role.OwnerTenantId && r.Code == role.Code)) with { IncludeDeleted = true };
        if (await roles.CountAsync(existing, context.CancellationToken).ConfigureAwait(false) > 0)
        {
            return Result.Success();
        }

        var added = await roles.AddAsync(role, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added;
        }

        var available = permissions.Where(code => catalog.Find(code) is { } descriptor && PermissionCatalog.AllowsSide(descriptor, platformSide: false)).ToList();
        if (available.Count > 0)
        {
            foreach (var outcome in await grants.GrantAsync(role, available, context.CancellationToken).ConfigureAwait(false))
            {
                if (outcome.Outcome.IsFailure)
                {
                    return outcome.Outcome;
                }
            }
        }

        var rows = new List<SysRoleDataScope>();
        foreach (var department in customDepartments)
        {
            rows.Add(new SysRoleDataScope { Id = ids.NewId(), OwnerTenantId = role.OwnerTenantId, RoleId = role.Id, DepartmentId = department, IncludeChildren = true });
        }

        foreach (var outcome in rows.Count == 0 ? [] : await scopes.AddRangeAsync(rows, context.CancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        var assignments = holders.Select(user => new SysUserRole { Id = ids.NewId(), OwnerTenantId = role.OwnerTenantId, UserId = user, RoleId = role.Id }).ToList();
        foreach (var outcome in assignments.Count == 0 ? [] : await members.AddRangeAsync(assignments, context.CancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }

    private async Task<Result<IReadOnlyList<Guid>>> DepartmentsAsync(IReadOnlyList<string> codes, InvocationContext context)
    {
        IReadOnlyList<Guid> found = [];
        var ran = await runner.RunAsync(DemoTenantSeeder.DemoTenantId, async ctx =>
        {
            found = await TenantScopedQuery.For<SysDepartment>(ctx).Where(d => codes.Contains(d.Code)).Select(static d => d.Id).ToListAsync(ctx.CancellationToken).ConfigureAwait(false);
            return Result.Success();
        }, context.CancellationToken).ConfigureAwait(false);
        return ran.IsFailure ? ran.Error! : Result.Success(found);
    }
}
