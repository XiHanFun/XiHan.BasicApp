// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 演示租户与账号：按 <see cref="SaasDemoScenario"/> 写入全局角色、平台账号与各演示租户
/// </summary>
/// <remarks>
/// 只写库里还没有的：全局角色按编码、平台账号按用户名、租户按编码判断（连同删掉的一起算），
/// 已有的整个跳过，改过、删过的都不覆盖或补回。一个租户第一次写入时连同它的部门、岗位、角色、账号、成员一起写；
/// 成员可以是别的租户或平台的账号（按邮箱找），所以先把这一轮要建的租户和账号全部建好，再逐个租户写成员。
/// 演示账号的密码都由种子写入，标记为需要本人改密。
/// </remarks>
public sealed class SaasDemoSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasDemoSeeder> logger,
    IServiceProvider serviceProvider,
    IPasswordHasher passwordHasher)
    : DemoDataSeederBase(clientResolver, logger, serviceProvider)
{
    private const string SeededRemark = "演示数据";

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Demo;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]演示租户与账号";

    /// <summary>
    /// 写入演示数据
    /// </summary>
    protected override async Task SeedDemoAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var catalog = await DbClientFor<SysPermission>().Queryable<SysPermission>()
            .Where(permission => permission.TenantId == 0)
            .ToListAsync();
        var globalRoleIds = await EnsureGlobalRolesAsync(catalog);
        var platformAccounts = await EnsurePlatformAccountsAsync(globalRoleIds, now);

        var editionIds = (await DbClientFor<SysTenantEdition>().Queryable<SysTenantEdition>()
                .Where(edition => edition.TenantId == 0)
                .ToListAsync())
            .ToDictionary(edition => edition.EditionCode, edition => edition.BasicId, StringComparer.Ordinal);
        var tenantCodes = SaasDemoScenario.Tenants.Select(static tenant => tenant.Code).ToList();
        var existingTenantCodes = (await DbClientFor<SysTenant>().Queryable<SysTenant>()
                .IncludingDeleted()
                .Where(tenant => tenantCodes.Contains(tenant.TenantCode))
                .Select(tenant => tenant.TenantCode)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        var created = new List<TenantSeedContext>();
        foreach (var (demo, index) in SaasDemoScenario.Tenants.Select((tenant, index) => (tenant, index)))
        {
            if (existingTenantCodes.Contains(demo.Code))
            {
                continue;
            }

            var editionId = editionIds.TryGetValue(demo.Edition, out var id)
                ? id
                : throw new InvalidOperationException($"{Name}：演示租户 {demo.Code} 的套餐 {demo.Edition} 不存在。");
            created.Add(await CreateTenantAsync(demo, editionId, (index + 1) * 10, catalog, now));
        }

        foreach (var context in created)
        {
            await AddMembersAsync(context, globalRoleIds, catalog, now);
        }

        Logger.LogInformation(
            "{Seeder}：新增演示租户 {TenantCount} 个、平台账号 {AccountCount} 个；已有的不覆盖。演示账号密码 {Password}",
            Name, created.Count, platformAccounts, SaasDemoScenario.Password);
    }

    private async Task<Dictionary<string, long>> EnsureGlobalRolesAsync(IReadOnlyList<SysPermission> catalog)
    {
        var codes = SaasDemoScenario.GlobalRoles.Select(static role => role.Code).ToList();
        var roleIds = (await DbClientFor<SysRole>().Queryable<SysRole>()
                .IncludingDeleted()
                .Where(role => role.TenantId == 0 && codes.Contains(role.RoleCode))
                .ToListAsync())
            .ToDictionary(role => role.RoleCode, role => role.BasicId, StringComparer.Ordinal);

        foreach (var demo in SaasDemoScenario.GlobalRoles.Where(role => !roleIds.ContainsKey(role.Code)))
        {
            var role = await InsertRoleAsync(0, demo, catalog, tenantRole: false);
            roleIds[demo.Code] = role.BasicId;
        }

        return roleIds;
    }

    private async Task<int> EnsurePlatformAccountsAsync(IReadOnlyDictionary<string, long> globalRoleIds, DateTimeOffset now)
    {
        var userNames = SaasDemoScenario.PlatformAccounts.Select(static account => account.UserName).ToList();
        var existing = (await DbClientFor<SysUser>().Queryable<SysUser>()
                .IncludingDeleted()
                .Where(user => user.TenantId == 0 && userNames.Contains(user.UserName))
                .Select(user => user.UserName)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        var added = 0;
        foreach (var account in SaasDemoScenario.PlatformAccounts.Where(account => !existing.Contains(account.UserName)))
        {
            var user = await InsertAccountAsync(0, account, isSystemAccount: false, now);
            foreach (var roleCode in account.Roles ?? [])
            {
                var roleId = globalRoleIds.TryGetValue(roleCode, out var id)
                    ? id
                    : throw new InvalidOperationException($"{Name}：平台账号 {account.UserName} 的角色 {roleCode} 不是全局角色。");
                await InsertUserRoleAsync(0, user.BasicId, roleId, new DemoRoleBinding(roleCode), now);
            }

            added++;
        }

        return added;
    }

    /// <summary>
    /// 建一个演示租户及它的部门、岗位、角色、账号
    /// </summary>
    private async Task<TenantSeedContext> CreateTenantAsync(DemoTenant demo, long editionId, int sort, IReadOnlyList<SysPermission> catalog, DateTimeOffset now)
    {
        var hasContent = demo.Departments is { Count: > 0 } || demo.Roles is { Count: > 0 } || demo.Accounts is { Count: > 0 } || demo.Members is { Count: > 0 };
        if (hasContent && demo.Isolation != TenantIsolationMode.Field)
        {
            throw new InvalidOperationException($"{Name}：演示租户 {demo.Code} 不是字段隔离，库还没初始化，不能写租户数据。");
        }

        var tenant = await DbClientFor<SysTenant>().Insertable(new SysTenant
        {
            TenantId = 0,
            TenantCode = demo.Code,
            TenantName = demo.Name,
            EditionId = editionId,
            IsolationMode = demo.Isolation,
            ConfigStatus = demo.ConfigStatus,
            TenantStatus = demo.Status,
            ExpirationTime = demo.ExpiresInDays is { } days ? now.AddDays(days) : null,
            UserLimit = demo.UserLimit,
            Sort = sort,
            Remark = demo.Scenario
        }).ExecuteReturnEntityAsync();

        var context = new TenantSeedContext(demo, tenant);
        using var tenantScope = CurrentTenant.Change(tenant.BasicId, tenant.TenantName);

        await InsertDepartmentsAsync(context);
        await InsertPositionsAsync(context);
        await InsertTenantRolesAsync(context, catalog);

        var ownerEmails = (demo.Members ?? [])
            .Where(static member => member.Type == TenantMemberType.Owner)
            .Select(static member => member.Email)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var account in demo.Accounts ?? [])
        {
            // 所有者账号与平台开通的管理员同一口径：租户里的系统账号
            _ = await InsertAccountAsync(tenant.BasicId, account, ownerEmails.Contains(account.Email), now);
        }

        Logger.LogInformation("{Seeder}：新增演示租户 {Tenant}", Name, demo.Name);
        return context;
    }

    private async Task InsertDepartmentsAsync(TenantSeedContext context)
    {
        var tenantId = context.Tenant.BasicId;
        var departments = new List<SysDepartment>();
        foreach (var (demo, index) in (context.Demo.Departments ?? []).Select((department, index) => (department, index)))
        {
            long? parentId = demo.Parent is null
                ? null
                : context.DepartmentIds.TryGetValue(demo.Parent, out var id)
                    ? id
                    : throw new InvalidOperationException($"{Name}：部门 {demo.Code} 的父部门 {demo.Parent} 须排在它前面。");
            var department = await DbClientFor<SysDepartment>().Insertable(new SysDepartment
            {
                TenantId = tenantId,
                ParentId = parentId,
                DepartmentCode = demo.Code,
                DepartmentName = demo.Name,
                DepartmentType = demo.Type,
                Status = demo.Status,
                Sort = (index + 1) * 10,
                Remark = SeededRemark
            }).ExecuteReturnEntityAsync();
            context.DepartmentIds[demo.Code] = department.BasicId;
            departments.Add(department);
        }

        if (departments.Count == 0)
        {
            return;
        }

        var closure = SysDepartmentHierarchy.BuildClosure(departments);
        foreach (var row in closure)
        {
            row.TenantId = tenantId;
        }

        await BulkInsertAsync([.. closure]);
    }

    private async Task InsertPositionsAsync(TenantSeedContext context)
    {
        var tenantId = context.Tenant.BasicId;
        foreach (var (demo, index) in (context.Demo.Positions ?? []).Select((position, index) => (position, index)))
        {
            var position = await DbClientFor<SysPosition>().Insertable(new SysPosition
            {
                TenantId = tenantId,
                PositionCode = demo.Code,
                PositionName = demo.Name,
                Status = demo.Status,
                Sort = (index + 1) * 10,
                Remark = SeededRemark
            }).ExecuteReturnEntityAsync();
            context.PositionIds[demo.Code] = position.BasicId;
        }
    }

    private async Task InsertTenantRolesAsync(TenantSeedContext context, IReadOnlyList<SysPermission> catalog)
    {
        var tenantId = context.Tenant.BasicId;
        if ((context.Demo.Members ?? []).Any(static member => member.Type == TenantMemberType.Owner))
        {
            var ownerRole = SysRole.CreateTenantOwnerRole();
            ownerRole.TenantId = tenantId;
            ownerRole = await DbClientFor<SysRole>().Insertable(ownerRole).ExecuteReturnEntityAsync();
            context.RoleIds[SaasRoleCodes.TenantOwner] = ownerRole.BasicId;
        }

        foreach (var demo in context.Demo.Roles ?? [])
        {
            var role = await InsertRoleAsync(tenantId, demo, catalog, tenantRole: true);
            context.RoleIds[demo.Code] = role.BasicId;
        }

        foreach (var demo in context.Demo.Roles ?? [])
        {
            if (demo.Inherits is not null)
            {
                await InsertRoleInheritanceAsync(tenantId, context.RoleIds[demo.Inherits], context.RoleIds[demo.Code]);
            }

            foreach (var departmentCode in demo.CustomDepartments ?? [])
            {
                _ = await DbClientFor<SysRoleDataScope>().Insertable(new SysRoleDataScope
                {
                    TenantId = tenantId,
                    RoleId = context.RoleIds[demo.Code],
                    DepartmentId = context.DepartmentIds[departmentCode],
                    IncludeChildren = true,
                    Status = ValidityStatus.Valid,
                    Remark = SeededRemark
                }).ExecuteCommandAsync();
            }
        }
    }

    /// <summary>
    /// 写一个角色及它的授权
    /// </summary>
    private async Task<SysRole> InsertRoleAsync(long tenantId, DemoRole demo, IReadOnlyList<SysPermission> catalog, bool tenantRole)
    {
        var role = await DbClientFor<SysRole>().Insertable(new SysRole
        {
            TenantId = tenantId,
            RoleCode = demo.Code,
            RoleName = demo.Name,
            RoleDescription = demo.Scenario,
            RoleType = RoleType.Business,
            DataScope = demo.DataScope,
            MaxMembers = demo.MaxMembers,
            Status = demo.Status,
            Sort = demo.Sort,
            Remark = SeededRemark
        }).ExecuteReturnEntityAsync();

        var permissionIds = ResolveGrant(demo, catalog, tenantRole);
        await BulkInsertAsync([.. permissionIds.Select(permissionId => new SysRolePermission
        {
            TenantId = tenantId,
            RoleId = role.BasicId,
            PermissionId = permissionId,
            PermissionAction = PermissionAction.Grant,
            Status = ValidityStatus.Valid,
            GrantReason = demo.Grant.Description,
            Remark = SeededRemark
        })]);
        return role;
    }

    /// <summary>
    /// 解析角色授权：指定的权限码必须存在，租户角色的指定权限码还必须能在租户生效；按条件挑选的，租户角色只取租户能生效的
    /// </summary>
    private IReadOnlyList<long> ResolveGrant(DemoRole demo, IReadOnlyList<SysPermission> catalog, bool tenantRole)
    {
        var byCode = catalog.ToDictionary(permission => permission.PermissionCode, StringComparer.Ordinal);
        var missing = demo.Grant.RequiredCodes.Where(code => !byCode.ContainsKey(code)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"{Name}：角色 {demo.Code} 的权限 {string.Join("、", missing)} 不存在。");
        }

        var explicitPermissions = demo.Grant.RequiredCodes
            .Concat(demo.Grant.OptionalCodes.Where(byCode.ContainsKey))
            .Select(code => byCode[code])
            .ToList();
        var platformOnly = explicitPermissions.Where(permission => tenantRole && !permission.Side.IsTenantEffective()).Select(static permission => permission.PermissionCode).ToList();
        if (platformOnly.Count > 0)
        {
            throw new InvalidOperationException($"{Name}：租户角色 {demo.Code} 不能授予平台侧权限 {string.Join("、", platformOnly)}。");
        }

        var filtered = demo.Grant.Filter is null
            ? []
            : catalog.Where(demo.Grant.Filter).Where(permission => !tenantRole || permission.Side.IsTenantEffective());
        return [.. explicitPermissions.Concat(filtered).Select(static permission => permission.BasicId).Distinct()];
    }

    /// <summary>
    /// 角色继承：后代拿到祖先的授权；闭包含两个角色的自身行与这条直接继承边
    /// </summary>
    private async Task InsertRoleInheritanceAsync(long tenantId, long ancestorId, long descendantId)
    {
        await BulkInsertAsync(
        [
            new SysRoleHierarchy { TenantId = tenantId, AncestorId = ancestorId, DescendantId = ancestorId, Depth = 0, Path = $"{ancestorId}" },
            new SysRoleHierarchy { TenantId = tenantId, AncestorId = descendantId, DescendantId = descendantId, Depth = 0, Path = $"{descendantId}" },
            new SysRoleHierarchy { TenantId = tenantId, AncestorId = ancestorId, DescendantId = descendantId, Depth = 1, Path = $"{ancestorId}/{descendantId}" }
        ]);
    }

    /// <summary>
    /// 写一个账号与它的安全记录（注册在指定租户，平台账号为 0）
    /// </summary>
    private async Task<SysUser> InsertAccountAsync(long tenantId, DemoAccount account, bool isSystemAccount, DateTimeOffset now)
    {
        var user = await DbClientFor<SysUser>().Insertable(new SysUser
        {
            TenantId = tenantId,
            UserName = account.UserName,
            RealName = account.RealName,
            NickName = account.RealName,
            Email = account.Email,
            Status = account.Status,
            IsActive = true,
            IsSystemAccount = isSystemAccount,
            Remark = account.Scenario
        }).ExecuteReturnEntityAsync();

        _ = await DbClientFor<SysUserSecurity>().Insertable(new SysUserSecurity
        {
            TenantId = tenantId,
            UserId = user.BasicId,
            Password = passwordHasher.HashPassword(SaasDemoScenario.Password),
            PasswordChangeRequired = true,
            LastPasswordChangeTime = now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            EmailVerified = true,
            AllowMultiLogin = true,
            MaxLoginDevices = 0,
            IsLocked = account.Locked,
            LockoutTime = account.Locked ? now : null,
            LockoutEndTime = account.Locked ? now.AddDays(1) : null,
            LastSecurityCheckTime = now,
            Remark = SeededRemark
        }).ExecuteCommandAsync();
        return user;
    }

    /// <summary>
    /// 写一个租户的成员：成员关系、角色、主部门与岗位、直授权限，并设部门负责人
    /// </summary>
    private async Task AddMembersAsync(TenantSeedContext context, IReadOnlyDictionary<string, long> globalRoleIds, IReadOnlyList<SysPermission> catalog, DateTimeOffset now)
    {
        var members = context.Demo.Members ?? [];
        if (members.Count == 0)
        {
            return;
        }

        List<string?> emails = [.. members.Select(static member => member.Email)];
        var userIds = (await DbClientFor<SysUser>().Queryable<SysUser>()
                .ClearTenantFilter()
                .Where(user => emails.Contains(user.Email))
                .ToListAsync())
            .ToDictionary(user => user.Email!, user => user.BasicId, StringComparer.Ordinal);
        var permissionIds = catalog.ToDictionary(permission => permission.PermissionCode, StringComparer.Ordinal);

        var tenantId = context.Tenant.BasicId;
        using var tenantScope = CurrentTenant.Change(tenantId, context.Tenant.TenantName);
        long? ownerId = members.FirstOrDefault(static member => member.Type == TenantMemberType.Owner) is { } owner ? userIds[owner.Email] : null;
        foreach (var member in members)
        {
            var userId = userIds.TryGetValue(member.Email, out var id)
                ? id
                : throw new InvalidOperationException($"{Name}：租户 {context.Demo.Code} 的成员账号 {member.Email} 不存在。");
            var isOwner = member.Type == TenantMemberType.Owner;
            _ = await DbClientFor<SysTenantUser>().Insertable(new SysTenantUser
            {
                TenantId = tenantId,
                UserId = userId,
                MemberType = member.Type,
                InviteStatus = member.InviteStatus,
                InvitedBy = isOwner ? null : ownerId,
                InvitedTime = isOwner ? null : now,
                RespondedTime = member.InviteStatus == TenantMemberInviteStatus.Pending ? null : now,
                ExpirationTime = member.ExpiresInDays is { } days ? now.AddDays(days) : null,
                DataScopeOverride = member.DataScopeOverride,
                Status = member.InviteStatus is TenantMemberInviteStatus.Revoked or TenantMemberInviteStatus.Expired ? ValidityStatus.Invalid : ValidityStatus.Valid,
                Remark = member.Scenario
            }).ExecuteCommandAsync();

            if (isOwner)
            {
                await InsertUserRoleAsync(tenantId, userId, context.RoleIds[SaasRoleCodes.TenantOwner], new DemoRoleBinding(SaasRoleCodes.TenantOwner), now);
            }

            foreach (var binding in member.Roles ?? [])
            {
                var roleId = context.RoleIds.TryGetValue(binding.RoleCode, out var tenantRoleId)
                    ? tenantRoleId
                    : globalRoleIds.TryGetValue(binding.RoleCode, out var globalRoleId)
                        ? globalRoleId
                        : throw new InvalidOperationException($"{Name}：成员 {member.Email} 的角色 {binding.RoleCode} 既不是租户 {context.Demo.Code} 的角色，也不是全局角色。");
                await InsertUserRoleAsync(tenantId, userId, roleId, binding, now);
            }

            if (member.Department is not null)
            {
                _ = await DbClientFor<SysUserDepartment>().Insertable(new SysUserDepartment
                {
                    TenantId = tenantId,
                    UserId = userId,
                    DepartmentId = context.DepartmentIds[member.Department],
                    PositionId = member.Position is null ? null : context.PositionIds[member.Position],
                    IsMain = true,
                    JoinTime = now,
                    Status = ValidityStatus.Valid,
                    Remark = SeededRemark
                }).ExecuteCommandAsync();

                if (member.IsLeader)
                {
                    var departmentId = context.DepartmentIds[member.Department];
                    _ = await DbClientFor<SysDepartment>().Updateable<SysDepartment>()
                        .SetColumns(department => department.LeaderId == userId)
                        .Where(department => department.BasicId == departmentId)
                        .ExecuteCommandAsync();
                }
            }

            foreach (var grant in member.Permissions ?? [])
            {
                if (!permissionIds.TryGetValue(grant.PermissionCode, out var permission))
                {
                    if (grant.IsModuleCode)
                    {
                        continue;
                    }

                    throw new InvalidOperationException($"{Name}：成员 {member.Email} 的直授权限 {grant.PermissionCode} 不存在。");
                }

                if (!permission.Side.IsTenantEffective())
                {
                    throw new InvalidOperationException($"{Name}：成员 {member.Email} 的直授权限 {grant.PermissionCode} 是平台侧的，在租户里不生效。");
                }

                _ = await DbClientFor<SysUserPermission>().Insertable(new SysUserPermission
                {
                    TenantId = tenantId,
                    UserId = userId,
                    PermissionId = permission.BasicId,
                    PermissionAction = grant.Action,
                    Status = ValidityStatus.Valid,
                    GrantReason = member.Scenario,
                    Remark = SeededRemark
                }).ExecuteCommandAsync();
            }
        }
    }

    private async Task InsertUserRoleAsync(long tenantId, long userId, long roleId, DemoRoleBinding binding, DateTimeOffset now)
    {
        _ = await DbClientFor<SysUserRole>().Insertable(new SysUserRole
        {
            TenantId = tenantId,
            UserId = userId,
            RoleId = roleId,
            EffectiveTime = binding.EffectiveInDays is { } effective ? now.AddDays(effective) : null,
            ExpirationTime = binding.ExpiresInDays is { } expires ? now.AddDays(expires) : null,
            Status = ValidityStatus.Valid,
            GrantReason = SeededRemark,
            Remark = SeededRemark
        }).ExecuteCommandAsync();
    }

    /// <summary>
    /// 一个正在写入的演示租户：编码到主键的映射
    /// </summary>
    private sealed class TenantSeedContext(DemoTenant demo, SysTenant tenant)
    {
        public DemoTenant Demo { get; } = demo;

        public SysTenant Tenant { get; } = tenant;

        public Dictionary<string, long> DepartmentIds { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, long> PositionIds { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, long> RoleIds { get; } = new(StringComparer.Ordinal);
    }
}
