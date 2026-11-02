// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Contracts.Organization;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// Contracts 里给流程用的三组目录与待办投递。
/// </summary>
/// <remarks>
/// 在演示数据上核对：组织目录（部门成员、负责人、岗位任职、部门链）只读调用所在租户、只取启用的部门与岗位；
/// 角色成员目录只取当前一侧启用的角色与有效期内的指派；可见的在用账号不含运维支持成员、停用的账号与别的租户的人；
/// 待办投递在调用所在一侧给收件人各写一条「待办」站内信。也核对演示种子写出的人、组织与角色（照 4.x 示例企业里流程演示的那几位）。
/// </remarks>
public sealed class DirectoryContractsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Directories_ReadTheCallersSide_AndKeepOnlyActiveVisibleUsers()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        await server.RunSeedersAsync();
        var people = await UsersAsync(server, "demo", ["admin", "manager", "lead", "hr"]);
        var (admin, manager, lead, hr) = (people["admin"], people["manager"], people["lead"], people["hr"]);
        var superAdmin = (await UsersAsync(server, TenantIds.Platform, ["superadmin"]))["superadmin"];

        await SettingsTestHost.InvokeAsync(server, "demo", async ctx =>
        {
            var organization = ctx.Services.GetRequiredService<IOrganizationDirectory>();
            Assert.Equal(new HashSet<Guid> { manager, lead }, (await organization.DepartmentMembersAsync(ctx, ["rd"])).ToHashSet());
            Assert.Equal([hr], await organization.DepartmentMembersAsync(ctx, ["hr", "nowhere"]));
            Assert.Equal([admin, manager], await organization.DepartmentLeadersAsync(ctx, ["hq", "mkt", "rd"]));
            Assert.Equal([admin], await organization.PositionHoldersAsync(ctx, ["gm"]));
            Assert.Equal([manager, lead], await organization.PositionHoldersAsync(ctx, ["manager", "engineer"]));
            var path = await organization.LeaderPathAsync(ctx, lead);
            Assert.Equal([(true, manager), (true, admin)], path.Select(static s => (s.IsActive, s.LeaderUserId!.Value)).ToList());
            Assert.Empty(await organization.LeaderPathAsync(ctx, Guid.NewGuid()));
            Assert.Equal(["hq", "rd", "mkt", "hr"], (await organization.DepartmentsAsync(ctx)).Select(static d => d.Code).ToList());
            Assert.Equal(["gm", "manager", "engineer", "specialist"], (await organization.PositionsAsync(ctx)).Select(static p => p.Code).ToList());

            var roles = ctx.Services.GetRequiredService<IRoleMemberDirectory>();
            Assert.Equal(new HashSet<Guid> { manager, lead, hr }, (await roles.MembersAsync(ctx, ["employee"])).ToHashSet());
            Assert.Equal([hr, admin], await roles.MembersAsync(ctx, ["hr_specialist", "admin", "nobody"]));
            Assert.Equal(["admin", "employee", "hr_specialist"], (await roles.RolesAsync(ctx)).Select(static r => r.Code).Order(StringComparer.Ordinal).ToList());

            // 停用部门：成员与负责人不再取到，部门链上照样有这一级、标为停用
            var departments = ctx.Services.GetRequiredService<IRepository<SysDepartment, Guid>>();
            var rd = (await departments.ListAsync(EntityQuery.All<SysDepartment>().Where(new Specification<SysDepartment>(d => d.Code == "rd")), ctx.CancellationToken)).Single();
            rd.Status = OrganizationStatus.Disabled;
            (await departments.UpdateAsync(rd, ctx.CancellationToken)).ThrowIfFailure();
            Assert.Empty(await organization.DepartmentMembersAsync(ctx, ["rd"]));
            Assert.Equal([admin], await organization.DepartmentLeadersAsync(ctx, ["rd", "hq"]));
            Assert.Equal([false, true], (await organization.LeaderPathAsync(ctx, lead)).Select(static s => s.IsActive).ToList());
            return true;
        });

        // 指派过期即不算成员
        await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var members = ctx.Services.GetRequiredService<IRepository<SysUserRole, Guid>>();
            var hrEmployee = (await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.OwnerTenantId == "demo" && m.UserId == hr)), ctx.CancellationToken)).ToList();
            foreach (var row in hrEmployee)
            {
                row.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
                (await members.UpdateAsync(row, ctx.CancellationToken)).ThrowIfFailure();
            }

            return true;
        });
        await SettingsTestHost.InvokeAsync(server, "demo", async ctx =>
        {
            var roles = ctx.Services.GetRequiredService<IRoleMemberDirectory>();
            Assert.Equal(new HashSet<Guid> { manager, lead }, (await roles.MembersAsync(ctx, ["employee"])).ToHashSet());
            Assert.Empty(await roles.MembersAsync(ctx, ["hr_specialist"]));
            return true;
        });

        // 平台一侧没有组织；角色只看平台自己的
        await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            Assert.Empty(await ctx.Services.GetRequiredService<IOrganizationDirectory>().DepartmentsAsync(ctx));
            Assert.Empty(await ctx.Services.GetRequiredService<IOrganizationDirectory>().DepartmentMembersAsync(ctx, ["rd"]));
            Assert.DoesNotContain(await ctx.Services.GetRequiredService<IRoleMemberDirectory>().RolesAsync(ctx), static r => r.Code == "employee");
            return true;
        });

        // 可见的账号：运维支持成员（超管）不算，取在用的另外不算停用的；关键字不匹配邮箱；平台一侧只有平台账号
        await SettingsTestHost.InvokeAsync(server, "demo", async ctx =>
        {
            var users = ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>();
            var disabled = (await users.FindAsync(lead, ctx.CancellationToken)).ValueOrThrow();
            disabled.Status = UserStatus.Disabled;
            (await users.UpdateAsync(disabled, ctx.CancellationToken)).ThrowIfFailure();
            var withEmail = (await users.FindAsync(manager, ctx.CancellationToken)).ValueOrThrow();
            withEmail.Email = "boss@example.com";
            (await users.UpdateAsync(withEmail, ctx.CancellationToken)).ThrowIfFailure();
            return true;
        });
        await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var directory = ctx.Services.GetRequiredService<IUserDirectory>();
            Assert.Equal(new HashSet<Guid> { admin, manager, hr }, await directory.ActiveInSideAsync("demo", [admin, manager, lead, hr, superAdmin], ctx.CancellationToken));
            Assert.Empty(await directory.ActiveInSideAsync(TenantIds.Platform, [admin, manager], ctx.CancellationToken));
            Assert.Equal(new HashSet<Guid> { admin, manager, lead, hr }, await directory.InSideAsync("demo", [admin, manager, lead, hr, superAdmin], ctx.CancellationToken));
            Assert.Empty(await directory.InSideAsync(TenantIds.Platform, [admin, lead], ctx.CancellationToken));
            Assert.Empty(await directory.SearchActiveInSideAsync("demo", "example.com", 50, ctx.CancellationToken));
            Assert.Equal(["admin", "hr", "manager"], (await directory.SearchActiveInSideAsync("demo", null, 50, ctx.CancellationToken)).Select(static u => u.UserName).ToList());
            Assert.Equal(["manager"], (await directory.SearchActiveInSideAsync("demo", "Manager", 50, ctx.CancellationToken)).Select(static u => u.UserName).ToList());
            Assert.Contains(await directory.SearchActiveInSideAsync(TenantIds.Platform, "super", 50, ctx.CancellationToken), static u => u.UserName == "superadmin");
            return true;
        });
    }

    [Fact]
    public async Task InboxDelivery_WritesATodoForEachRecipient_OnTheCallersSide()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var people = await UsersAsync(server, "demo", ["manager", "lead"]);

        var delivered = await SettingsTestHost.InvokeAsync(server, "demo", async ctx =>
            (await ctx.Services.GetRequiredService<IInboxDelivery>().DeliverAsync(ctx, new InboxMessage([people["manager"], people["lead"], people["lead"]], "Pending approval", "Expense 12000", "/workflow/approvals?tab=todo", "workflow", "instance-1"))).ValueOrThrow());
        Assert.Equal(2, delivered);
        Assert.Equal(0, await SettingsTestHost.InvokeAsync(server, "demo", async ctx => (await ctx.Services.GetRequiredService<IInboxDelivery>().DeliverAsync(ctx, new InboxMessage([], "Nobody", null, null, "workflow", "instance-2"))).ValueOrThrow()));

        await SettingsTestHost.InvokeAsync(server, "demo", async ctx =>
        {
            var notification = Assert.Single(await TenantScopedQuery.For<SysNotification>(ctx).ToListAsync(ctx.CancellationToken));
            Assert.Equal((NotificationType.Todo, "/workflow/approvals?tab=todo", "workflow", "instance-1", true), (notification.NotificationType, notification.Link, notification.BusinessType, notification.BusinessId, notification.IsPublished));
            var inbox = await TenantScopedQuery.For<SysUserNotification>(ctx).Where(n => n.NotificationId == notification.Id).Select(static n => n.UserId).ToListAsync(ctx.CancellationToken);
            Assert.Equal(new HashSet<Guid> { people["manager"], people["lead"] }, inbox.ToHashSet());
            return true;
        });
    }

    private static async Task<Dictionary<string, Guid>> UsersAsync(BasicAppTestServer server, string side, IReadOnlyList<string> names)
        => await SettingsTestHost.InvokeAsync(server, null, async ctx =>
            (await ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>().ListAsync(EntityQuery.All<SysUser>().Where(new Specification<SysUser>(u => u.HomeTenantId == side && names.Contains(u.UserName))), ctx.CancellationToken))
                .ToDictionary(static u => u.UserName, static u => u.Id, StringComparer.Ordinal));
}
