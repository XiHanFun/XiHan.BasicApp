#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasDemoUserSeeder
// Guid:a8f3c2e1-4b5d-4a9e-9f0c-1d2e3f4a5b6c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/20 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 演示用户种子数据（对齐用户管理设计稿样本，便于联调列表/筛选/安全标记）
/// </summary>
public sealed class SaasDemoUserSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasDemoUserSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant,
    IPasswordHasher passwordHasher)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private const long DefaultTenantId = 1;
    private const string DefaultTenantCode = "default";
    private const string DemoPassword = "Demo@123";
    private const string SeedMarkerUserName = "zhang.san";

    private static readonly Dictionary<string, string> DeptLabelToCode = new(StringComparer.Ordinal)
    {
        ["技术部"] = "tech",
        ["产品部"] = "product",
        ["运营部"] = "ops",
        ["市场部"] = "marketing",
        ["销售部"] = "sales",
        ["人事部"] = "hr",
        ["财务部"] = "finance",
        ["前端组"] = "frontend",
        ["后端组"] = "backend",
        ["测试组"] = "qa",
    };

    private static readonly DemoRoleSeed[] DemoRoles =
    [
        new("tenant_admin", "租户管理员", DataPermissionScope.All, 1),
        new("member", "普通用户", DataPermissionScope.SelfOnly, 10),
        new("data_analyst", "数据分析师", DataPermissionScope.DepartmentOnly, 20),
        new("ops_specialist", "运营专员", DataPermissionScope.DepartmentOnly, 30),
        new("finance_specialist", "财务专员", DataPermissionScope.DepartmentOnly, 40),
        new("guest", "访客", DataPermissionScope.SelfOnly, 50),
    ];

    private static readonly DemoUserSeed[] DemoUsers =
    [
        new("zhang.san", "张三", "小张", "zhang@example.com", "13900239001", UserGender.Male, EnableStatus.Enabled, true,
            ["tenant_admin"], ["技术部"], false, false, TwoFactorMethod.Email, 0, false, true, true, 3, "2026-05-19T09:15:00+08:00", "10.0.0.15"),
        new("li.si", "李四", null, "lisi@example.com", "13700347002", UserGender.Male, EnableStatus.Enabled, true,
            ["member", "data_analyst"], ["产品部"], false, false, TwoFactorMethod.None, 0, false, true, true, 1, "2026-05-18T17:45:00+08:00", "10.0.0.22"),
        new("wang.wu", "王五", "阿五", "wangwu@example.com", "13600456003", UserGender.Male, EnableStatus.Enabled, true,
            ["ops_specialist"], ["运营部"], false, true, TwoFactorMethod.Totp | TwoFactorMethod.Email | TwoFactorMethod.Phone, 5, false, true, true, 0, "2026-05-19T08:00:00+08:00", "10.0.1.5"),
        new("zhao.liu", "赵六", "小赵", "zhaoliu@example.com", "13500565004", UserGender.Female, EnableStatus.Enabled, true,
            ["member"], ["市场部"], false, false, TwoFactorMethod.None, 0, false, true, false, 2, "2026-05-19T11:20:00+08:00", "10.0.0.88"),
        new("chen.qi", "陈七", null, "chenqi@example.com", "13400674005", UserGender.Female, EnableStatus.Disabled, true,
            ["finance_specialist"], ["财务部"], false, false, TwoFactorMethod.None, 0, false, false, true, 1, "2026-05-10T14:30:00+08:00", "10.0.0.45"),
        new("liu.ba", "刘八", "老刘", "liuba@example.com", "13300783006", UserGender.Male, EnableStatus.Enabled, false,
            ["guest"], ["人事部"], false, false, TwoFactorMethod.None, 2, false, false, false, 0, "2026-04-28T09:00:00+08:00", "10.0.2.11"),
        new("sun.jiu", "孙九", "小孙", "sunjiu@example.com", "13200892007", UserGender.Female, EnableStatus.Enabled, true,
            ["data_analyst"], ["技术部", "产品部"], false, false, TwoFactorMethod.Totp, 0, false, true, true, 5, "2026-05-19T07:30:00+08:00", "10.0.0.99"),
        new("zhou.shi", "周十", null, "zhoushi@example.com", "13100901008", UserGender.Male, EnableStatus.Enabled, true,
            ["member"], ["技术部"], false, false, TwoFactorMethod.Email | TwoFactorMethod.Phone, 1, false, true, false, 2, "2026-05-16T16:00:00+08:00", "10.0.1.77", "ja-JP", "JP", "Asia/Tokyo"),
        new("wu.shi1", "吴十一", "小吴", "wushi1@example.com", "13001010009", UserGender.Female, EnableStatus.Enabled, true,
            ["ops_specialist"], ["运营部", "市场部"], false, false, TwoFactorMethod.None, 10, false, true, true, 0, "2026-05-19T12:00:00+08:00", "10.0.0.200"),
        new("zheng.shi2", "郑十二", null, "zhengshi2@example.com", "18911120010", UserGender.Male, EnableStatus.Disabled, true,
            ["member"], ["财务部"], false, false, TwoFactorMethod.None, 0, false, true, false, 1, "2026-03-15T10:00:00+08:00", "10.0.3.55"),
        new("wei.shi3", "魏十三", "阿伟", "weishi3@example.com", "18812130011", UserGender.Male, EnableStatus.Enabled, true,
            ["tenant_admin", "data_analyst"], ["技术部"], false, false, TwoFactorMethod.Email, 0, false, true, true, 3, "2026-05-18T20:00:00+08:00", "10.0.0.150"),
        new("xie.shi4", "谢十四", null, "xieshi4@example.com", "18713140012", UserGender.Female, EnableStatus.Enabled, true,
            ["member"], ["人事部"], false, false, TwoFactorMethod.None, 0, false, false, true, 0, "2026-05-17T14:45:00+08:00", "10.0.0.175"),
        new("han.shi5", "韩十五", "大韩", "hanshi5@example.com", "18614150013", UserGender.Male, EnableStatus.Enabled, true,
            ["finance_specialist", "tenant_admin"], ["财务部"], false, true, TwoFactorMethod.Totp | TwoFactorMethod.Email | TwoFactorMethod.Phone, 3, false, true, true, 2, "2026-05-19T09:50:00+08:00", "10.0.1.200"),
        new("tang.shi6", "唐十六", "小唐", "tangshi6@example.com", "18515160014", UserGender.Female, EnableStatus.Enabled, false,
            ["guest"], ["市场部"], false, false, TwoFactorMethod.None, 0, false, false, false, 0, "2026-05-01T11:00:00+08:00", "10.0.0.50"),
        new("feng.qian", "冯前端", "小冯", "fengqian@example.com", "18416170015", UserGender.Male, EnableStatus.Enabled, true,
            ["member"], ["前端组"], false, false, TwoFactorMethod.Email, 0, false, true, true, 2, "2026-05-19T10:05:00+08:00", "10.0.0.31"),
        new("yang.hou", "杨后端", null, "yanghou@example.com", "18317180016", UserGender.Male, EnableStatus.Enabled, true,
            ["member"], ["后端组"], false, false, TwoFactorMethod.Totp, 0, false, true, true, 0, "2026-05-19T10:10:00+08:00", "10.0.0.32"),
        new("lin.ce", "林测试", "小林", "lince@example.com", "18218190017", UserGender.Female, EnableStatus.Enabled, true,
            ["member"], ["测试组"], false, false, TwoFactorMethod.None, 0, false, true, false, 1, "2026-05-18T16:20:00+08:00", "10.0.0.33"),
        new("xu.xiao", "徐销售", null, "xuxiao@example.com", "18119200018", UserGender.Male, EnableStatus.Enabled, true,
            ["ops_specialist"], ["销售部"], false, false, TwoFactorMethod.Phone, 0, false, false, true, 0, "2026-05-17T09:00:00+08:00", "10.0.0.40"),
        new("he.hr", "何人事", null, "hehr@example.com", "18020210019", UserGender.Female, EnableStatus.Enabled, true,
            ["member"], ["人事部"], false, false, TwoFactorMethod.Email, 0, false, true, true, 0, "2026-05-16T11:30:00+08:00", "10.0.0.41"),
    ];

    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public override int Order => 35;

    public override string Name => "[SaaS]演示用户种子数据";

    protected override async Task SeedInternalAsync()
    {
        var tenantId = await ResolveTenantIdAsync();
        if (tenantId == 0)
        {
            Logger.LogWarning("未找到默认租户，跳过演示用户种子数据");
            return;
        }

        using var tenantScope = _currentTenant.Change(tenantId, tenantId.ToString());
        var client = DbClient;

        if (await client.Queryable<SysUser>()
            .AnyAsync(user => user.TenantId == tenantId && user.UserName == SeedMarkerUserName))
        {
            Logger.LogInformation("演示用户已存在（标记用户 {UserName}），跳过种子数据", SeedMarkerUserName);
            return;
        }

        var roleMap = await EnsureDemoRolesAsync(client, tenantId);
        var deptMap = await LoadDepartmentMapAsync(client, tenantId);
        var passwordHash = _passwordHasher.HashPassword(DemoPassword);
        var created = 0;

        foreach (var seed in DemoUsers)
        {
            if (await client.Queryable<SysUser>()
                .AnyAsync(user => user.TenantId == tenantId && user.UserName == seed.UserName))
            {
                continue;
            }

            var user = BuildUser(seed, tenantId);
            var savedUser = await client.Insertable(user).ExecuteReturnEntityAsync();

            await InsertSecurityAsync(client, tenantId, savedUser.BasicId, seed, passwordHash);
            await InsertMembershipAsync(client, tenantId, savedUser.BasicId, seed);
            await InsertUserRolesAsync(client, tenantId, savedUser.BasicId, seed.RoleCodes, roleMap);
            await InsertUserDepartmentsAsync(client, tenantId, savedUser.BasicId, seed.DepartmentLabels, deptMap);

            created++;
        }

        Logger.LogInformation(
            "成功初始化 {Count} 个演示用户（默认密码 {Password}，标记用户 {Marker}）",
            created,
            DemoPassword,
            SeedMarkerUserName);
    }

    private static SysUser BuildUser(DemoUserSeed seed, long tenantId)
    {
        var lastLogin = DateTimeOffset.TryParse(seed.LastLoginTime, out var loginAt) ? loginAt : (DateTimeOffset?)null;
        return new SysUser
        {
            TenantId = tenantId,
            UserName = seed.UserName,
            RealName = seed.RealName,
            NickName = seed.NickName,
            Email = seed.Email,
            Phone = seed.Phone,
            Gender = seed.Gender,
            Status = seed.Status,
            IsActive = seed.IsActive,
            IsSystemAccount = false,
            Language = seed.Language,
            Country = seed.Country,
            TimeZone = seed.TimeZone,
            LastLoginTime = lastLogin,
            LastLoginIp = seed.LastLoginIp,
            Remark = seed.Remark,
        };
    }

    private static async Task InsertSecurityAsync(
        ISqlSugarClient client,
        long tenantId,
        long userId,
        DemoUserSeed seed,
        string passwordHash)
    {
        var now = DateTimeOffset.UtcNow;
        var security = new SysUserSecurity
        {
            TenantId = tenantId,
            UserId = userId,
            Password = passwordHash,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            EmailVerified = seed.EmailVerified,
            PhoneVerified = seed.PhoneVerified,
            IsLocked = seed.IsLocked,
            // 配置了 MFA 方式时同步打开开关，便于前端安全列展示
            TwoFactorEnabled = seed.TwoFactorEnabled || seed.TwoFactorMethod != TwoFactorMethod.None,
            TwoFactorMethod = seed.TwoFactorMethod,
            FailedLoginAttempts = seed.FailedLoginAttempts,
            AllowMultiLogin = seed.AllowMultiLogin,
            MaxLoginDevices = seed.MaxLoginDevices,
            LastPasswordChangeTime = now,
            LastSecurityCheckTime = now,
            Remark = "演示用户种子数据",
        };

        if (seed.IsLocked)
        {
            security.LockoutTime = now;
            security.LockoutEndTime = now.AddYears(10);
        }

        _ = await client.Insertable(security).ExecuteReturnEntityAsync();
    }

    private static async Task InsertMembershipAsync(
        ISqlSugarClient client,
        long tenantId,
        long userId,
        DemoUserSeed seed)
    {
        var now = DateTimeOffset.UtcNow;
        var isAdmin = seed.RoleCodes.Contains("tenant_admin", StringComparer.Ordinal);
        var member = new SysTenantUser
        {
            TenantId = tenantId,
            UserId = userId,
            MemberType = isAdmin ? TenantMemberType.Admin : TenantMemberType.Member,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            DisplayName = seed.RealName,
            Status = ValidityStatus.Valid,
            InvitedTime = now.AddDays(-30),
            RespondedTime = now.AddDays(-29),
            LastActiveTime = now,
            Remark = "演示用户种子数据",
        };

        _ = await client.Insertable(member).ExecuteReturnEntityAsync();
    }

    private static async Task InsertUserRolesAsync(
        ISqlSugarClient client,
        long tenantId,
        long userId,
        IReadOnlyList<string> roleCodes,
        IReadOnlyDictionary<string, long> roleMap)
    {
        foreach (var roleCode in roleCodes.Distinct(StringComparer.Ordinal))
        {
            if (!roleMap.TryGetValue(roleCode, out var roleId))
            {
                continue;
            }

            var exists = await client.Queryable<SysUserRole>()
                .AnyAsync(ur => ur.TenantId == tenantId && ur.UserId == userId && ur.RoleId == roleId);
            if (exists)
            {
                continue;
            }

            var userRole = new SysUserRole
            {
                TenantId = tenantId,
                UserId = userId,
                RoleId = roleId,
                Status = ValidityStatus.Valid,
                GrantReason = "演示用户种子数据",
                Remark = "演示用户种子数据",
            };

            _ = await client.Insertable(userRole).ExecuteReturnEntityAsync();
        }
    }

    private static async Task InsertUserDepartmentsAsync(
        ISqlSugarClient client,
        long tenantId,
        long userId,
        IReadOnlyList<string> departmentLabels,
        IReadOnlyDictionary<string, long> deptMap)
    {
        var isFirst = true;
        foreach (var label in departmentLabels)
        {
            if (!DeptLabelToCode.TryGetValue(label, out var code) || !deptMap.TryGetValue(code, out var deptId))
            {
                continue;
            }

            var exists = await client.Queryable<SysUserDepartment>()
                .AnyAsync(ud => ud.TenantId == tenantId && ud.UserId == userId && ud.DepartmentId == deptId);
            if (exists)
            {
                continue;
            }

            var userDept = new SysUserDepartment
            {
                TenantId = tenantId,
                UserId = userId,
                DepartmentId = deptId,
                IsMain = isFirst,
                Status = ValidityStatus.Valid,
                Remark = "演示用户种子数据",
            };

            _ = await client.Insertable(userDept).ExecuteReturnEntityAsync();
            isFirst = false;
        }
    }

    private async Task<Dictionary<string, long>> EnsureDemoRolesAsync(ISqlSugarClient client, long tenantId)
    {
        var map = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var seed in DemoRoles)
        {
            var existing = await client.Queryable<SysRole>()
                .FirstAsync(role => role.TenantId == tenantId && role.RoleCode == seed.RoleCode);
            if (existing is not null)
            {
                map[seed.RoleCode] = existing.BasicId;
                continue;
            }

            var role = new SysRole
            {
                TenantId = tenantId,
                RoleCode = seed.RoleCode,
                RoleName = seed.RoleName,
                RoleDescription = $"演示角色：{seed.RoleName}",
                RoleType = RoleType.Custom,
                DataScope = seed.DataScope,
                MaxMembers = 0,
                Status = EnableStatus.Enabled,
                Sort = seed.Sort,
                Remark = "演示用户种子数据",
            };

            var saved = await client.Insertable(role).ExecuteReturnEntityAsync();
            map[seed.RoleCode] = saved.BasicId;
        }

        return map;
    }

    private static async Task<Dictionary<string, long>> LoadDepartmentMapAsync(ISqlSugarClient client, long tenantId)
    {
        var departments = await client.Queryable<SysDepartment>()
            .Where(dept => dept.TenantId == tenantId && !dept.IsDeleted)
            .Select(dept => new { dept.BasicId, dept.DepartmentCode })
            .ToListAsync();

        return departments
            .Where(dept => !string.IsNullOrWhiteSpace(dept.DepartmentCode))
            .ToDictionary(dept => dept.DepartmentCode!, dept => dept.BasicId, StringComparer.Ordinal);
    }

    private async Task<long> ResolveTenantIdAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var tenant = await client.Queryable<SysTenant>()
            .FirstAsync(t => t.TenantCode == DefaultTenantCode);
        return tenant?.BasicId ?? DefaultTenantId;
    }

    private sealed record DemoRoleSeed(string RoleCode, string RoleName, DataPermissionScope DataScope, int Sort);

    private sealed record DemoUserSeed(
        string UserName,
        string RealName,
        string? NickName,
        string Email,
        string Phone,
        UserGender Gender,
        EnableStatus Status,
        bool IsActive,
        string[] RoleCodes,
        string[] DepartmentLabels,
        bool IsLocked,
        bool TwoFactorEnabled,
        TwoFactorMethod TwoFactorMethod,
        int FailedLoginAttempts,
        bool EmailVerified,
        bool PhoneVerified,
        bool AllowMultiLogin,
        int MaxLoginDevices,
        string LastLoginTime,
        string LastLoginIp,
        string Language = "zh-CN",
        string Country = "CN",
        string TimeZone = "Asia/Shanghai",
        string? Remark = null);
}
