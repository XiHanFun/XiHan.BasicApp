#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasBusinessTenantSeeder
// Guid:9d2f4c81-7a36-4e15-b8c9-3a47e0d52f6b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/22 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.Demo;

/// <summary>
/// SaaS 业务租户种子数据（新增 2 个演示业务租户 + 多租户身份 + 跨租户成员）
/// </summary>
/// <remarks>
/// 在默认租户演示身份 35 之后运行（Order=37）：跨租户成员步骤依赖默认租户样例用户（zhangsan/lisi）已存在。
/// 自包含完成每个业务租户的全套初始化：
/// - 平台态：建租户（TenantCode 幂等）+ 绑定默认版本（IsDefault=true）。
/// - 租户态：建部门（总公司 + 一级部门）并重建闭包表 → 播种差异化角色（沿用 6 角色口径）→
///   播种账号 + 安全记录 + 成员关系 + 用户角色 + 用户部门。
/// - 跨租户成员：把默认租户的若干用户以 External/Consultant 身份加入新租户，演示「一个用户分配多个租户」，
///   并在目标租户授予只读角色（guest）以便登录后切换租户可见可测。
/// 幂等：所有插入前按唯一键判存在；不触碰默认租户既有数据，不触碰 superadmin/super_admin。
/// </remarks>
public sealed class SaasBusinessTenantSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasBusinessTenantSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant,
    IPasswordHasher passwordHasher)
    : SaasDemoSeederBase(clientResolver, logger, serviceProvider)
{
    private const string DefaultTenantCode = "default";

    /// <summary>
    /// 平台专属权限（口径对齐 SaasTenantEditionSeeder 的 Enterprise 白名单排除项）：系统管理员授「全部权限减去这些」。
    /// </summary>
    private static readonly HashSet<string> PlatformOnlyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        SaasPermissionCodes.Tenant.Create,
        SaasPermissionCodes.Tenant.Update,
        SaasPermissionCodes.Tenant.Status,
        SaasPermissionCodes.TenantEdition.Read,
        SaasPermissionCodes.TenantEdition.Create,
        SaasPermissionCodes.TenantEdition.Update,
        SaasPermissionCodes.TenantEdition.Status,
        SaasPermissionCodes.TenantEdition.Default,
        SaasPermissionCodes.TenantEditionPermission.Read,
        SaasPermissionCodes.TenantEditionPermission.Grant,
        SaasPermissionCodes.TenantEditionPermission.Update,
        SaasPermissionCodes.TenantEditionPermission.Revoke,
        SaasPermissionCodes.Resource.Create,
        SaasPermissionCodes.Resource.Update,
        SaasPermissionCodes.Resource.Status,
        SaasPermissionCodes.Resource.Delete,
        SaasPermissionCodes.Operation.Create,
        SaasPermissionCodes.Operation.Update,
        SaasPermissionCodes.Operation.Status,
        SaasPermissionCodes.Operation.Delete,
        SaasPermissionCodes.Menu.Create,
        SaasPermissionCodes.Menu.Update,
        SaasPermissionCodes.Menu.Status,
        SaasPermissionCodes.Menu.Delete,
        SaasPermissionCodes.Cache.Read,
        SaasPermissionCodes.Cache.Clear,
        SaasPermissionCodes.Server.Read,
    };

    /// <summary>
    /// 开发工具权限（代码生成）：平台级开发功能，仅超级管理员可拥有，租户管理员的"全部权限"亦排除之。
    /// </summary>
    private static bool IsDevelopmentToolCode(string code)
    {
        return code.StartsWith("code_gen:", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("code_gen_api:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 差异化角色集（与默认租户演示角色口径一致，每个业务租户各播一套）。
    /// </summary>
    private static readonly IReadOnlyList<RoleSeed> RoleSeeds =
    [
        new("tenant_admin", "系统管理员", "租户内最高权限：管理用户/角色/部门/业务/日志等（平台/开发专属除外）",
            DataPermissionScope.All, 10,
            allCodes => allCodes.Where(code => !PlatformOnlyCodes.Contains(code) && !IsDevelopmentToolCode(code))),
        new("normal_user", "普通用户", "普通成员：工作台 + 消息阅读",
            DataPermissionScope.SelfOnly, 20,
            _ =>
            [
                SaasPermissionCodes.UserStatistics.Read,
                SaasPermissionCodes.Notification.Read,
                SaasPermissionCodes.Message.Read,
            ]),
        new("guest", "游客", "只读访客：仅工作台仪表盘",
            DataPermissionScope.SelfOnly, 30,
            _ => [SaasPermissionCodes.UserStatistics.Read]),
        new("auditor", "审计员", "审计与日志查看：全量日志 + 审批查看",
            DataPermissionScope.All, 40,
            _ =>
            [
                SaasPermissionCodes.UserStatistics.Read,
                SaasPermissionCodes.AccessLog.Read,
                SaasPermissionCodes.ApiLog.Read,
                SaasPermissionCodes.OperationLog.Read,
                SaasPermissionCodes.LoginLog.Read,
                SaasPermissionCodes.ExceptionLog.Read,
                SaasPermissionCodes.DiffLog.Read,
                SaasPermissionCodes.PermissionChangeLog.Read,
                SaasPermissionCodes.ReviewLog.Read,
                SaasPermissionCodes.TaskLog.Read,
                SaasPermissionCodes.Review.Read,
            ]),
        new("operator", "运营专员", "内容运营：消息/通知/模板/文件/审批",
            DataPermissionScope.All, 50,
            _ =>
            [
                SaasPermissionCodes.UserStatistics.Read,
                SaasPermissionCodes.Message.Read,
                SaasPermissionCodes.Message.Create,
                SaasPermissionCodes.Message.Update,
                SaasPermissionCodes.Message.Status,
                SaasPermissionCodes.Message.Publish,
                SaasPermissionCodes.Message.Delete,
                SaasPermissionCodes.Notification.Read,
                SaasPermissionCodes.Notification.Create,
                SaasPermissionCodes.Notification.Update,
                SaasPermissionCodes.Notification.Publish,
                SaasPermissionCodes.Notification.Delete,
                SaasPermissionCodes.MessageTemplate.Read,
                SaasPermissionCodes.MessageTemplate.Create,
                SaasPermissionCodes.MessageTemplate.Update,
                SaasPermissionCodes.MessageTemplate.Status,
                SaasPermissionCodes.MessageTemplate.Delete,
                SaasPermissionCodes.File.Read,
                SaasPermissionCodes.File.Create,
                SaasPermissionCodes.File.Update,
                SaasPermissionCodes.File.Status,
                SaasPermissionCodes.File.Delete,
                SaasPermissionCodes.Review.Read,
                SaasPermissionCodes.Review.Create,
                SaasPermissionCodes.Review.Update,
            ]),
        new("dept_manager", "部门主管", "部门负责人：本部门工作台 + 消息/通知 + 审批查看（本部门数据范围）",
            DataPermissionScope.DepartmentAndChildren, 15,
            _ =>
            [
                SaasPermissionCodes.UserStatistics.Read,
                SaasPermissionCodes.Notification.Read,
                SaasPermissionCodes.Message.Read,
                SaasPermissionCodes.Review.Read,
            ]),
    ];

    /// <summary>
    /// 业务租户清单：每个租户编码 + 名称 + 简称 + 部门集合 + 账号集合。
    /// </summary>
    private static readonly IReadOnlyList<TenantSeed> TenantSeeds =
    [
        new("acme", "演示甲企业", "甲企业", 10,
            Departments:
            [
                new("XiHan", "xihan", DepartmentType.Company, "总公司", null, 10),
                new("技术部", "tech", DepartmentType.Department, "技术研发中心", "xihan", 20),
                new("产品部", "product", DepartmentType.Department, "产品设计与管理", "xihan", 30),
                new("运营部", "ops", DepartmentType.Department, "运营与增长", "xihan", 40),
            ],
            Users:
            [
                new("acme_admin", "甲企业管理员", "甲管理员", "admin@acme.demo", "Admin@123", "tenant_admin", TenantMemberType.Owner, "tech"),
                new("acme_manager", "甲企业主管", "甲主管", "manager@acme.demo", "Manager@123", "dept_manager", TenantMemberType.Admin, "product"),
                new("acme_user", "甲企业用户", "甲用户", "user@acme.demo", "User@123", "normal_user", TenantMemberType.Member, "ops"),
                new("acme_auditor", "甲企业审计", "甲审计", "auditor@acme.demo", "Auditor@123", "auditor", TenantMemberType.Member, "tech"),
            ]),
        new("globex", "演示乙企业", "乙企业", 20,
            Departments:
            [
                new("XiHan", "xihan", DepartmentType.Company, "总公司", null, 10),
                new("技术部", "tech", DepartmentType.Department, "技术研发中心", "xihan", 20),
                new("市场部", "marketing", DepartmentType.Department, "市场与品牌", "xihan", 30),
                new("财务部", "finance", DepartmentType.Department, "财务管理与审计", "xihan", 40),
            ],
            Users:
            [
                new("globex_admin", "乙企业管理员", "乙管理员", "admin@globex.demo", "Admin@123", "tenant_admin", TenantMemberType.Owner, "tech"),
                new("globex_operator", "乙企业运营", "乙运营", "operator@globex.demo", "Operator@123", "operator", TenantMemberType.Admin, "marketing"),
                new("globex_user", "乙企业用户", "乙用户", "user@globex.demo", "User@123", "normal_user", TenantMemberType.Member, "marketing"),
                new("globex_auditor", "乙企业审计", "乙审计", "auditor@globex.demo", "Auditor@123", "auditor", TenantMemberType.Member, "finance"),
            ]),
    ];

    /// <summary>
    /// 跨租户成员演示：把默认租户的用户加入新业务租户（External/Consultant），并授目标租户只读角色 guest。
    /// </summary>
    private static readonly IReadOnlyList<CrossTenantSeed> CrossTenantSeeds =
    [
        new("zhangsan", "acme", TenantMemberType.External, "guest", "张三（默认租户）跨租户协作至甲企业"),
        new("zhangsan", "globex", TenantMemberType.Consultant, "guest", "张三（默认租户）跨租户顾问至乙企业"),
        new("lisi", "acme", TenantMemberType.Consultant, "guest", "李四（默认租户）跨租户顾问至甲企业"),
    ];

    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    /// <summary>
    /// 种子数据优先级（在默认租户演示身份 35 之后）。
    /// </summary>
    /// <remarks>
    /// 必须晚于 <see cref="SaasSampleIdentitySeeder"/>（Order=35）：跨租户成员步骤依赖默认租户的样例用户
    /// （zhangsan/lisi）已存在；若早于 35 运行，源用户尚未播种，跨租户成员会被静默跳过，
    /// 导致「一个用户分配多个租户」演示数据缺失。业务租户/部门/角色/账号本身只依赖权限(20)/版本(21)，
    /// 故整体后移到 37 不影响这些前置依赖。
    /// </remarks>
    public override int Order => 37;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]业务租户种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (TrySkipWhenDemoDisabled())
        {
            return;
        }

        Dictionary<string, long> permissionIdByCode;
        long? defaultEditionId;

        // 平台态：解析全局权限码→ID 映射（权限均为 TenantId=0 全局定义）与默认版本 ID
        {
            using var platformScope = _currentTenant.Change(null);
            var platformClient = DbClient;
            var permissions = await platformClient.Queryable<SysPermission>()
                .Where(permission => permission.TenantId == 0 && permission.Status == EnableStatus.Enabled)
                .ToListAsync();
            permissionIdByCode = permissions
                .GroupBy(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().BasicId, StringComparer.OrdinalIgnoreCase);

            defaultEditionId = await ResolveDefaultEditionIdAsync(platformClient);
        }

        if (permissionIdByCode.Count == 0)
        {
            Logger.LogWarning("平台全局权限不存在，跳过业务租户种子数据");
            return;
        }

        var allCodes = permissionIdByCode.Keys.ToArray();

        // 业务租户编码→ID（供跨租户成员引用）
        var tenantIdByCode = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var tenantCreated = 0;

        foreach (var tenantSeed in TenantSeeds)
        {
            long tenantId;

            // 平台态：建租户 + 绑定默认版本
            {
                using var platformScope = _currentTenant.Change(null);
                var (id, created) = await EnsureBusinessTenantAsync(DbClient, tenantSeed, defaultEditionId);
                tenantId = id;
                tenantCreated += created ? 1 : 0;
            }

            tenantIdByCode[tenantSeed.TenantCode] = tenantId;

            // 租户态：部门 + 角色 + 账号 + 成员 + 用户角色 + 用户部门
            await SeedTenantIdentityAsync(tenantId, tenantSeed, allCodes, permissionIdByCode);
        }

        // 跨租户成员：把默认租户用户加入新业务租户
        await SeedCrossTenantMembershipsAsync(tenantIdByCode);

        Logger.LogInformation(
            "业务租户种子已就绪（业务租户 {TenantCount} 个本次新增 {TenantCreated}，跨租户成员关系 {CrossCount} 条）",
            TenantSeeds.Count, tenantCreated, CrossTenantSeeds.Count);
    }

    private static async Task<long?> ResolveDefaultEditionIdAsync(ISqlSugarClient client)
    {
        var edition = (await client.Queryable<SysTenantEdition>()
                .Where(item => item.IsDefault && item.Status == EnableStatus.Enabled)
                .ToListAsync())
            .OrderBy(item => item.Sort)
            .ThenBy(item => item.BasicId)
            .FirstOrDefault();
        return edition?.BasicId;
    }

    private static async Task<(long TenantId, bool Created)> EnsureBusinessTenantAsync(
        ISqlSugarClient client, TenantSeed seed, long? defaultEditionId)
    {
        var existing = await client.Queryable<SysTenant>()
            .FirstAsync(tenant => tenant.TenantCode == seed.TenantCode);
        if (existing is not null)
        {
            return (existing.BasicId, false);
        }

        var tenant = new SysTenant
        {
            TenantId = 0,
            TenantCode = seed.TenantCode,
            TenantName = seed.TenantName,
            TenantShortName = seed.ShortName,
            EditionId = defaultEditionId,
            ConfigStatus = TenantConfigStatus.Configured,
            TenantStatus = TenantStatus.Normal,
            IsolationMode = TenantIsolationMode.Field,
            Sort = seed.Sort,
            Remark = "系统初始化业务租户",
        };
        var saved = await client.Insertable(tenant).ExecuteReturnEntityAsync();
        return (saved.BasicId, true);
    }

    private async Task SeedTenantIdentityAsync(
        long tenantId,
        TenantSeed tenantSeed,
        string[] allCodes,
        IReadOnlyDictionary<string, long> permissionIdByCode)
    {
        using var tenantScope = _currentTenant.Change(tenantId, tenantId.ToString());
        var client = DbClient;

        // 部门（含闭包表重建）
        var departmentIdByCode = await EnsureDepartmentsAsync(client, tenantId, tenantSeed.Departments);

        // 角色 + 角色权限
        var roleIdByCode = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleSeed in RoleSeeds)
        {
            var (roleId, _) = await EnsureRoleAsync(client, tenantId, roleSeed);
            roleIdByCode[roleSeed.Code] = roleId;

            var permissionIds = roleSeed.ResolveCodes(allCodes)
                .Select(code => permissionIdByCode.TryGetValue(code, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            await BindRolePermissionsAsync(client, tenantId, roleId, permissionIds);
        }

        // 账号 + 安全 + 成员 + 用户角色 + 用户部门
        foreach (var userSeed in tenantSeed.Users)
        {
            var (userId, _) = await EnsureUserAsync(client, tenantId, userSeed);
            await EnsureUserSecurityAsync(client, tenantId, userId, userSeed.Password);
            await EnsureMembershipAsync(client, tenantId, userId, userSeed.MemberType, null, "系统初始化业务租户成员关系");
            if (roleIdByCode.TryGetValue(userSeed.RoleCode, out var roleId))
            {
                await EnsureUserRoleAsync(client, tenantId, userId, roleId);
            }

            if (departmentIdByCode.TryGetValue(userSeed.DepartmentCode, out var departmentId))
            {
                await EnsureUserDepartmentAsync(client, tenantId, userId, departmentId);
            }
        }
    }

    private async Task SeedCrossTenantMembershipsAsync(IReadOnlyDictionary<string, long> tenantIdByCode)
    {
        // 平台态：解析默认租户 ID + 源用户（默认租户内）UserName→UserId
        long defaultTenantId;
        Dictionary<string, long> defaultUserIdByName;
        {
            using var platformScope = _currentTenant.Change(null);
            var platformClient = DbClient;
            var defaultTenant = await platformClient.Queryable<SysTenant>()
                .FirstAsync(tenant => tenant.TenantCode == DefaultTenantCode);
            if (defaultTenant is null)
            {
                Logger.LogWarning("未找到默认租户，跳过跨租户成员种子数据");
                return;
            }

            defaultTenantId = defaultTenant.BasicId;
            var sourceNames = CrossTenantSeeds.Select(seed => seed.UserName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var users = await platformClient.Queryable<SysUser>()
                .Where(user => user.TenantId == defaultTenantId && sourceNames.Contains(user.UserName))
                .ToListAsync();
            defaultUserIdByName = users
                .GroupBy(user => user.UserName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().BasicId, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var seed in CrossTenantSeeds)
        {
            if (!defaultUserIdByName.TryGetValue(seed.UserName, out var userId))
            {
                Logger.LogWarning("跨租户成员源用户 {UserName} 不存在（默认租户），跳过", seed.UserName);
                continue;
            }

            if (!tenantIdByCode.TryGetValue(seed.TargetTenantCode, out var targetTenantId))
            {
                Logger.LogWarning("跨租户成员目标租户 {TenantCode} 不存在，跳过", seed.TargetTenantCode);
                continue;
            }

            // 目标租户态：建成员关系 + 授只读角色（角色 ID 取目标租户内同名角色）
            using var tenantScope = _currentTenant.Change(targetTenantId, targetTenantId.ToString());
            var client = DbClient;

            await EnsureMembershipAsync(client, targetTenantId, userId, seed.MemberType, seed.Remark, seed.Remark);

            var role = await client.Queryable<SysRole>()
                .FirstAsync(item => item.TenantId == targetTenantId && item.RoleCode == seed.RoleCode);
            if (role is not null)
            {
                await EnsureUserRoleAsync(client, targetTenantId, userId, role.BasicId);
            }
        }
    }

    private static async Task<Dictionary<string, long>> EnsureDepartmentsAsync(
        ISqlSugarClient client, long tenantId, IReadOnlyList<DepartmentSeed> departmentSeeds)
    {
        var idByCode = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        // 父级须先于子级建立：种子已按层级排序（根在前），ParentCode 解析依赖前序已建
        foreach (var seed in departmentSeeds)
        {
            long? parentId = null;
            if (seed.ParentCode is not null && idByCode.TryGetValue(seed.ParentCode, out var resolvedParentId))
            {
                parentId = resolvedParentId;
            }

            var (department, _) = await EnsureDepartmentAsync(client, tenantId, parentId, seed.Name, seed.Code, seed.Type, seed.Description, seed.Sort);
            idByCode[seed.Code] = department.BasicId;
        }

        await RebuildDepartmentHierarchyAsync(client, tenantId);
        return idByCode;
    }

    private static async Task<(SysDepartment Department, bool Created)> EnsureDepartmentAsync(
        ISqlSugarClient client,
        long tenantId,
        long? parentId,
        string name,
        string code,
        DepartmentType type,
        string description,
        int sort)
    {
        var existing = await client.Queryable<SysDepartment>()
            .FirstAsync(dept => dept.TenantId == tenantId && dept.DepartmentCode == code && !dept.IsDeleted);
        if (existing is not null)
        {
            return (existing, false);
        }

        var dept = new SysDepartment
        {
            TenantId = tenantId,
            ParentId = parentId,
            DepartmentName = name,
            DepartmentCode = code,
            DepartmentType = type,
            Status = EnableStatus.Enabled,
            Sort = sort,
            Remark = description,
        };

        var saved = await client.Insertable(dept).ExecuteReturnEntityAsync();
        return (saved, true);
    }

    private static async Task RebuildDepartmentHierarchyAsync(ISqlSugarClient client, long tenantId)
    {
        _ = await client.Deleteable<SysDepartmentHierarchy>()
            .Where(row => row.TenantId == tenantId)
            .ExecuteCommandAsync();

        var departments = await client.Queryable<SysDepartment>()
            .Where(dept => dept.TenantId == tenantId && !dept.IsDeleted)
            .ToListAsync();
        if (departments.Count == 0)
        {
            return;
        }

        var rows = BuildHierarchyRows(departments);
        foreach (var row in rows)
        {
            row.TenantId = tenantId;
        }

        if (rows.Count > 0)
        {
            _ = await client.Insertable(rows).ExecuteCommandAsync();
        }
    }

    private static List<SysDepartmentHierarchy> BuildHierarchyRows(IReadOnlyList<SysDepartment> departments)
    {
        var departmentMap = departments.ToDictionary(department => department.BasicId);
        var rows = new List<SysDepartmentHierarchy>();

        foreach (var department in departments
                     .OrderBy(department => department.ParentId ?? 0)
                     .ThenBy(department => department.Sort)
                     .ThenBy(department => department.DepartmentCode, StringComparer.Ordinal))
        {
            var chain = BuildAncestorChain(department, departmentMap);
            for (var depth = 0; depth < chain.Count; depth++)
            {
                var ancestor = chain[depth];
                var pathNodes = chain.Take(depth + 1).Reverse().ToArray();
                rows.Add(new SysDepartmentHierarchy
                {
                    AncestorId = ancestor.BasicId,
                    DescendantId = department.BasicId,
                    Depth = depth,
                    Path = string.Join("/", pathNodes.Select(node => node.BasicId)),
                    PathName = string.Join("/", pathNodes.Select(node => node.DepartmentName)),
                });
            }
        }

        return rows;
    }

    private static List<SysDepartment> BuildAncestorChain(
        SysDepartment department,
        IReadOnlyDictionary<long, SysDepartment> departmentMap)
    {
        var chain = new List<SysDepartment>();
        var visited = new HashSet<long>();
        var cursor = department;

        while (true)
        {
            if (!visited.Add(cursor.BasicId))
            {
                throw new InvalidOperationException("部门层级存在环路，不能重建闭包表。");
            }

            chain.Add(cursor);
            if (!cursor.ParentId.HasValue)
            {
                return chain;
            }

            if (!departmentMap.TryGetValue(cursor.ParentId.Value, out var parent))
            {
                throw new InvalidOperationException($"部门 {cursor.DepartmentCode} 的父级不存在，无法重建闭包表。");
            }

            cursor = parent;
        }
    }

    private static async Task<(long RoleId, bool Created)> EnsureRoleAsync(ISqlSugarClient client, long tenantId, RoleSeed seed)
    {
        var existing = await client.Queryable<SysRole>()
            .FirstAsync(role => role.TenantId == tenantId && role.RoleCode == seed.Code);
        if (existing is not null)
        {
            return (existing.BasicId, false);
        }

        var role = new SysRole
        {
            TenantId = tenantId,
            RoleCode = seed.Code,
            RoleName = seed.Name,
            RoleDescription = seed.Description,
            RoleType = RoleType.Business,
            DataScope = seed.DataScope,
            MaxMembers = 0,
            Status = EnableStatus.Enabled,
            Sort = seed.Sort,
            Remark = "系统初始化业务租户角色",
        };
        var saved = await client.Insertable(role).ExecuteReturnEntityAsync();
        return (saved.BasicId, true);
    }

    private static async Task BindRolePermissionsAsync(ISqlSugarClient client, long tenantId, long roleId, IReadOnlyCollection<long> permissionIds)
    {
        if (permissionIds.Count == 0)
        {
            return;
        }

        var existingIds = (await client.Queryable<SysRolePermission>()
                .Where(binding => binding.TenantId == tenantId && binding.RoleId == roleId)
                .ToListAsync())
            .Select(binding => binding.PermissionId)
            .ToHashSet();

        var addList = permissionIds
            .Where(permissionId => !existingIds.Contains(permissionId))
            .Select(permissionId => new SysRolePermission
            {
                TenantId = tenantId,
                RoleId = roleId,
                PermissionId = permissionId,
                PermissionAction = PermissionAction.Grant,
                Status = ValidityStatus.Valid,
                GrantReason = "系统初始化业务租户角色权限",
                Remark = "系统初始化业务租户角色权限",
            })
            .ToList();

        if (addList.Count > 0)
        {
            _ = await client.Insertable(addList).ExecuteCommandAsync();
        }
    }

    private static async Task<(long UserId, bool Created)> EnsureUserAsync(ISqlSugarClient client, long tenantId, UserSeed seed)
    {
        var existing = await client.Queryable<SysUser>()
            .FirstAsync(user => user.TenantId == tenantId && user.UserName == seed.UserName);
        if (existing is not null)
        {
            return (existing.BasicId, false);
        }

        var user = new SysUser
        {
            TenantId = tenantId,
            UserName = seed.UserName,
            RealName = seed.RealName,
            NickName = seed.NickName,
            Email = seed.Email,
            Status = EnableStatus.Enabled,
            IsActive = true,
            Language = "zh-CN",
            IsSystemAccount = false,
            Remark = "系统初始化业务租户账号",
        };
        var saved = await client.Insertable(user).ExecuteReturnEntityAsync();
        return (saved.BasicId, true);
    }

    private static async Task EnsureMembershipAsync(
        ISqlSugarClient client, long tenantId, long userId, TenantMemberType memberType, string? displayName, string remark)
    {
        var exists = await client.Queryable<SysTenantUser>()
            .FirstAsync(member => member.TenantId == tenantId && member.UserId == userId);
        if (exists is not null)
        {
            return;
        }

        var member = new SysTenantUser
        {
            TenantId = tenantId,
            UserId = userId,
            MemberType = memberType,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            RespondedTime = DateTimeOffset.UtcNow,
            Status = ValidityStatus.Valid,
            DisplayName = displayName,
            Remark = remark,
        };
        _ = await client.Insertable(member).ExecuteReturnEntityAsync();
    }

    private static async Task EnsureUserRoleAsync(ISqlSugarClient client, long tenantId, long userId, long roleId)
    {
        var exists = await client.Queryable<SysUserRole>()
            .FirstAsync(userRole => userRole.TenantId == tenantId && userRole.UserId == userId && userRole.RoleId == roleId);
        if (exists is not null)
        {
            return;
        }

        var userRole = new SysUserRole
        {
            TenantId = tenantId,
            UserId = userId,
            RoleId = roleId,
            Status = ValidityStatus.Valid,
            GrantReason = "系统初始化业务租户角色绑定",
            Remark = "系统初始化业务租户角色绑定",
        };
        _ = await client.Insertable(userRole).ExecuteReturnEntityAsync();
    }

    private static async Task EnsureUserDepartmentAsync(ISqlSugarClient client, long tenantId, long userId, long departmentId)
    {
        var exists = await client.Queryable<SysUserDepartment>()
            .FirstAsync(relation => relation.TenantId == tenantId && relation.UserId == userId && relation.DepartmentId == departmentId);
        if (exists is not null)
        {
            return;
        }

        var relation = new SysUserDepartment
        {
            TenantId = tenantId,
            UserId = userId,
            DepartmentId = departmentId,
            IsMain = true,
            Status = ValidityStatus.Valid,
            Remark = "系统初始化业务租户用户部门归属",
        };
        _ = await client.Insertable(relation).ExecuteReturnEntityAsync();
    }

    private async Task EnsureUserSecurityAsync(ISqlSugarClient client, long tenantId, long userId, string password)
    {
        var exists = await client.Queryable<SysUserSecurity>()
            .FirstAsync(security => security.UserId == userId);
        if (exists is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var security = new SysUserSecurity
        {
            TenantId = tenantId,
            UserId = userId,
            Password = _passwordHasher.HashPassword(password),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            EmailVerified = true,
            AllowMultiLogin = true,
            MaxLoginDevices = 0,
            LastPasswordChangeTime = now,
            LastSecurityCheckTime = now,
            Remark = "系统初始化业务租户账号安全记录",
        };
        _ = await client.Insertable(security).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 角色种子定义
    /// </summary>
    private sealed record RoleSeed(
        string Code,
        string Name,
        string Description,
        DataPermissionScope DataScope,
        int Sort,
        Func<IReadOnlyCollection<string>, IEnumerable<string>> ResolveCodes);

    /// <summary>
    /// 账号种子定义
    /// </summary>
    private sealed record UserSeed(
        string UserName,
        string RealName,
        string NickName,
        string Email,
        string Password,
        string RoleCode,
        TenantMemberType MemberType,
        string DepartmentCode);

    /// <summary>
    /// 部门种子定义（ParentCode 为 null 表示根部门）
    /// </summary>
    private sealed record DepartmentSeed(
        string Name,
        string Code,
        DepartmentType Type,
        string Description,
        string? ParentCode,
        int Sort);

    /// <summary>
    /// 业务租户种子定义
    /// </summary>
    private sealed record TenantSeed(
        string TenantCode,
        string TenantName,
        string ShortName,
        int Sort,
        IReadOnlyList<DepartmentSeed> Departments,
        IReadOnlyList<UserSeed> Users);

    /// <summary>
    /// 跨租户成员种子定义（把默认租户用户加入目标业务租户）
    /// </summary>
    private sealed record CrossTenantSeed(
        string UserName,
        string TargetTenantCode,
        TenantMemberType MemberType,
        string RoleCode,
        string Remark);
}
