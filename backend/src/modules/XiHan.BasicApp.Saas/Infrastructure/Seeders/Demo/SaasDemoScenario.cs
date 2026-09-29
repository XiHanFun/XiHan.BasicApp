// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using P = XiHan.BasicApp.Saas.Domain.Permissions.SaasPermissionCodes;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 角色的授权：从权限目录里挑哪些权限
/// </summary>
/// <remarks>
/// SaaS 的权限码必须存在（写错直接报错）；其它模块的权限码是可选的，装了对应模块才授予。
/// 租户角色只落租户能生效的权限，平台侧的自动略过。
/// </remarks>
public sealed class DemoGrant
{
    private DemoGrant(string description, IReadOnlyList<string> requiredCodes, Func<SysPermission, bool>? filter, IReadOnlyList<string> optionalCodes)
    {
        Description = description;
        RequiredCodes = requiredCodes;
        Filter = filter;
        OptionalCodes = optionalCodes;
    }

    /// <summary>
    /// 全部权限（各模块）
    /// </summary>
    public static DemoGrant All { get; } = new("全部权限", [], static _ => true, []);

    /// <summary>
    /// 全部查看权限（各模块的 read）
    /// </summary>
    public static DemoGrant AllRead { get; } = new("全部查看权限", [], static permission => permission.PermissionCode.EndsWith(":read", StringComparison.Ordinal), []);

    /// <summary>
    /// 说明
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 必须存在的权限码
    /// </summary>
    public IReadOnlyList<string> RequiredCodes { get; }

    /// <summary>
    /// 按条件挑选
    /// </summary>
    public Func<SysPermission, bool>? Filter { get; }

    /// <summary>
    /// 可选的其它模块权限码
    /// </summary>
    public IReadOnlyList<string> OptionalCodes { get; }

    /// <summary>
    /// 指定的 SaaS 权限码
    /// </summary>
    public static DemoGrant Codes(string description, params string[] codes)
    {
        return new DemoGrant(description, codes, null, []);
    }

    /// <summary>
    /// 追加其它模块的权限码（装了对应模块才授予）
    /// </summary>
    public DemoGrant AndModules(params string[] codes)
    {
        return new DemoGrant(Description, RequiredCodes, Filter, [.. OptionalCodes, .. codes]);
    }
}

/// <summary>
/// 演示角色
/// </summary>
/// <param name="Code">角色编码</param>
/// <param name="Name">角色名称</param>
/// <param name="Scenario">演示的情况（写入角色说明）</param>
/// <param name="DataScope">数据范围</param>
/// <param name="Grant">授权</param>
/// <param name="Sort">排序</param>
/// <param name="MaxMembers">成员上限（0 不限）</param>
/// <param name="Status">启停</param>
/// <param name="Inherits">继承的角色编码</param>
/// <param name="CustomDepartments">自定义数据范围的部门编码（含下级）</param>
public sealed record DemoRole(
    string Code,
    string Name,
    string Scenario,
    DataPermissionScope DataScope,
    DemoGrant Grant,
    int Sort,
    int MaxMembers = 0,
    EnableStatus Status = EnableStatus.Enabled,
    string? Inherits = null,
    IReadOnlyList<string>? CustomDepartments = null);

/// <summary>
/// 演示账号（注册在平台或某个租户）
/// </summary>
/// <param name="UserName">用户名</param>
/// <param name="RealName">姓名</param>
/// <param name="Email">邮箱（全平台唯一，也是租户账号的登录名）</param>
/// <param name="Scenario">演示的情况（写入账号备注）</param>
/// <param name="Status">账号启停</param>
/// <param name="Locked">是否锁定（一天后自动解锁）</param>
/// <param name="Roles">平台账号持有的全局角色</param>
public sealed record DemoAccount(
    string UserName,
    string RealName,
    string Email,
    string Scenario,
    EnableStatus Status = EnableStatus.Enabled,
    bool Locked = false,
    IReadOnlyList<string>? Roles = null);

/// <summary>
/// 成员持有的角色；时间窗以播种时刻为基准的天数（负数表示已过去）
/// </summary>
/// <param name="RoleCode">角色编码（本租户角色或全局角色）</param>
/// <param name="EffectiveInDays">几天后生效（预约）</param>
/// <param name="ExpiresInDays">几天后到期</param>
public sealed record DemoRoleBinding(string RoleCode, int? EffectiveInDays = null, int? ExpiresInDays = null)
{
    /// <summary>
    /// 长期有效的角色
    /// </summary>
    public static implicit operator DemoRoleBinding(string roleCode)
    {
        return new DemoRoleBinding(roleCode);
    }
}

/// <summary>
/// 成员的直授权限
/// </summary>
/// <param name="PermissionCode">权限码</param>
/// <param name="Action">授予或禁止</param>
/// <param name="IsModuleCode">是否其它模块的权限码（装了对应模块才写）</param>
public sealed record DemoUserPermission(string PermissionCode, PermissionAction Action, bool IsModuleCode = false);

/// <summary>
/// 租户成员
/// </summary>
/// <param name="Email">账号邮箱（本租户的账号，或注册在别处的账号）</param>
/// <param name="Type">成员类型</param>
/// <param name="Scenario">演示的情况（写入成员备注）</param>
/// <param name="Roles">角色</param>
/// <param name="Department">主部门编码</param>
/// <param name="Position">岗位编码</param>
/// <param name="IsLeader">是否主部门负责人</param>
/// <param name="InviteStatus">邀请状态</param>
/// <param name="ExpiresInDays">成员身份几天后到期</param>
/// <param name="DataScopeOverride">成员级数据范围覆盖</param>
/// <param name="Permissions">直授权限</param>
public sealed record DemoMember(
    string Email,
    TenantMemberType Type,
    string Scenario,
    IReadOnlyList<DemoRoleBinding>? Roles = null,
    string? Department = null,
    string? Position = null,
    bool IsLeader = false,
    TenantMemberInviteStatus InviteStatus = TenantMemberInviteStatus.Accepted,
    int? ExpiresInDays = null,
    DataPermissionScope? DataScopeOverride = null,
    IReadOnlyList<DemoUserPermission>? Permissions = null);

/// <summary>
/// 部门（父级须排在子级之前）
/// </summary>
/// <param name="Code">部门编码</param>
/// <param name="Name">部门名称</param>
/// <param name="Type">部门类型</param>
/// <param name="Parent">父部门编码</param>
/// <param name="Status">启停</param>
public sealed record DemoDepartment(string Code, string Name, DepartmentType Type, string? Parent = null, EnableStatus Status = EnableStatus.Enabled);

/// <summary>
/// 岗位
/// </summary>
/// <param name="Code">岗位编码</param>
/// <param name="Name">岗位名称</param>
/// <param name="Status">启停</param>
public sealed record DemoPosition(string Code, string Name, EnableStatus Status = EnableStatus.Enabled);

/// <summary>
/// 演示租户
/// </summary>
/// <param name="Code">租户编码</param>
/// <param name="Name">租户名称（带上它演示的情况）</param>
/// <param name="Edition">套餐编码</param>
/// <param name="Scenario">演示的情况（写入租户备注）</param>
/// <param name="Status">租户状态</param>
/// <param name="ExpiresInDays">几天后到期（负数表示已到期）</param>
/// <param name="UserLimit">席位上限覆盖</param>
/// <param name="Isolation">隔离方式</param>
/// <param name="ConfigStatus">配置状态</param>
/// <param name="Departments">部门</param>
/// <param name="Positions">岗位</param>
/// <param name="Roles">租户角色</param>
/// <param name="Accounts">注册在本租户的账号</param>
/// <param name="Members">成员（本租户账号与外来账号）</param>
public sealed record DemoTenant(
    string Code,
    string Name,
    string Edition,
    string Scenario,
    TenantStatus Status = TenantStatus.Normal,
    int? ExpiresInDays = null,
    int? UserLimit = null,
    TenantIsolationMode Isolation = TenantIsolationMode.Field,
    TenantConfigStatus ConfigStatus = TenantConfigStatus.Configured,
    IReadOnlyList<DemoDepartment>? Departments = null,
    IReadOnlyList<DemoPosition>? Positions = null,
    IReadOnlyList<DemoRole>? Roles = null,
    IReadOnlyList<DemoAccount>? Accounts = null,
    IReadOnlyList<DemoMember>? Members = null);

/// <summary>
/// 演示场景：每个租户、角色、账号、成员演示一种情况，名称和备注就是说明
/// </summary>
/// <remarks>
/// 平台账号用用户名登录（如 operator），租户账号用邮箱登录（如 owner@enterprise.demo），密码都是 <see cref="Password"/>。
/// 企业版租户覆盖成员类型、数据范围、角色继承与名额、授权时间窗、直授与禁止、账号与成员的各种状态；
/// 其它租户各演示一种套餐或生命周期状态。
/// </remarks>
public static class SaasDemoScenario
{
    /// <summary>
    /// 全部演示账号的密码
    /// </summary>
    public const string Password = "Demo@123";

    /// <summary>
    /// 普通员工的授权：工作台、消息、通知，装了聊天与工作流就能聊天、发起流程
    /// </summary>
    private static readonly DemoGrant EmployeeGrant = DemoGrant
        .Codes("工作台、消息、通知", P.UserStatistics.Read, P.Message.Read, P.Notification.Read)
        .AndModules("chat:read", "chat:send", "workflow:read", "workflow:execute");

    /// <summary>
    /// 租户管理员的角色
    /// </summary>
    private static readonly DemoRole TenantAdminRole = new(
        "tenant_admin", "租户管理员", "租户能生效的全部权限（仍受套餐白名单限制）", DataPermissionScope.All, DemoGrant.All, 10);

    /// <summary>
    /// 普通员工的角色
    /// </summary>
    private static readonly DemoRole EmployeeRole = new(
        "employee", "普通员工", "只看本人数据：工作台、消息、通知、聊天、发起流程", DataPermissionScope.SelfOnly, EmployeeGrant, 20);

    /// <summary>
    /// 全局角色模板（TenantId = 0）：平台账号在平台持有；租户可以把它分配给自己的成员，但改不了
    /// </summary>
    public static IReadOnlyList<DemoRole> GlobalRoles { get; } =
    [
        new("platform_operator", "平台运营",
            "管理租户与套餐、以支持人员入驻、跨租户模仿（平台侧权限，分配到租户里不生效）",
            DataPermissionScope.All,
            DemoGrant.Codes("租户与套餐管理、模仿登录",
                P.Tenant.Read, P.Tenant.Create, P.Tenant.Update, P.Tenant.Status, P.Tenant.InitDb, P.Tenant.SupportMember, P.Tenant.TransferOwner,
                P.TenantEdition.Read, P.TenantEditionPermission.Read, P.Impersonation.Start, P.Impersonation.CrossTenant, P.UserStatistics.Read),
            100),
        new("global_auditor", "审计员（全局模板）",
            "各类日志只读；平台与租户都能用，租户里只能分配、改不了",
            DataPermissionScope.All,
            DemoGrant.Codes("日志只读",
                P.UserStatistics.Read, P.AccessLog.Read, P.ApiLog.Read, P.OperationLog.Read, P.LoginLog.Read, P.ExceptionLog.Read,
                P.DiffLog.Read, P.PermissionChangeLog.Read, P.LogTrace.Read, P.ReviewLog.Read),
            110),
    ];

    /// <summary>
    /// 平台账号（TenantId = 0，用用户名登录）
    /// </summary>
    public static IReadOnlyList<DemoAccount> PlatformAccounts { get; } =
    [
        new("operator", "平台运营", "operator@platform.demo", "平台运营：管理租户与套餐、跨租户模仿", Roles: ["platform_operator"]),
        new("support", "平台客服", "support@platform.demo", "平台客服：以支持人员入驻租户（企业版在支持窗口内，专业版的窗口已结束）", Roles: ["platform_operator"]),
        new("auditor", "平台审计", "auditor@platform.demo", "平台审计：持有全局审计角色，看平台日志", Roles: ["global_auditor"]),
        new("frozen", "已停用的平台账号", "frozen@platform.demo", "账号已停用：登录被拒绝", Status: EnableStatus.Disabled),
    ];

    /// <summary>
    /// 演示租户
    /// </summary>
    public static IReadOnlyList<DemoTenant> Tenants { get; } =
    [
        new("demo-enterprise", "示例企业（企业版）", SaasEditionSeeder.Codes.Enterprise,
            "企业版正常租户：完整组织，覆盖全部成员类型、各档数据范围、角色继承与名额、授权时间窗、直授与禁止、账号与成员的各种状态",
            Departments:
            [
                new("hq", "示例企业", DepartmentType.Company),
                new("rd", "研发中心", DepartmentType.Center, "hq"),
                new("rd-fe", "前端组", DepartmentType.Team, "rd"),
                new("rd-be", "后端组", DepartmentType.Team, "rd"),
                new("mkt", "市场部", DepartmentType.Department, "hq"),
                new("hr", "人事部", DepartmentType.Department, "hq"),
                new("legacy", "已撤并的部门", DepartmentType.Department, "hq", EnableStatus.Disabled),
            ],
            Positions:
            [
                new("gm", "总经理"),
                new("manager", "部门经理"),
                new("engineer", "工程师"),
                new("specialist", "专员"),
                new("intern", "实习生（已停用岗位）", EnableStatus.Disabled),
            ],
            Roles:
            [
                TenantAdminRole,
                EmployeeRole,
                new("dept_manager", "部门主管", "继承普通员工，数据范围为本部门及下级",
                    DataPermissionScope.DepartmentAndChildren,
                    DemoGrant.Codes("成员、部门、审批查看", P.User.Read, P.Department.Read, P.UserDepartment.Read, P.UserSession.Read, P.Review.Read, P.Review.Update),
                    30, Inherits: "employee"),
                new("hr_specialist", "人事专员", "自定义数据范围：研发中心（含下级）与市场部",
                    DataPermissionScope.Custom,
                    DemoGrant.Codes("人事日常：成员与部门归属",
                        P.User.Read, P.User.Create, P.User.Update, P.User.Status, P.Department.Read, P.Position.Read,
                        P.UserDepartment.Read, P.UserDepartment.Grant, P.UserDepartment.Update, P.TenantMember.Read),
                    40, CustomDepartments: ["rd", "mkt"]),
                new("viewer", "只读查看", "各模块的查看权限，全部数据", DataPermissionScope.All, DemoGrant.AllRead, 50),
                new("project_lead", "项目负责人", "成员上限 1 且已占满：再分配给别人会被拒绝",
                    DataPermissionScope.DepartmentOnly,
                    DemoGrant.Codes("工作台", P.UserStatistics.Read).AndModules("workflow:read", "workflow:create", "workflow:update", "workflow:execute"),
                    60, MaxMembers: 1),
                new("archived", "已停用的角色", "角色已停用：持有它的成员拿不到它的任何权限",
                    DataPermissionScope.SelfOnly, DemoGrant.Codes("成员查看", P.User.Read), 90, Status: EnableStatus.Disabled),
            ],
            Accounts:
            [
                new("owner", "张总", "owner@enterprise.demo", "租户所有者"),
                new("admin", "李管理", "admin@enterprise.demo", "租户管理员"),
                new("manager", "王主管", "manager@enterprise.demo", "研发中心主管"),
                new("engineer", "赵工程师", "engineer@enterprise.demo", "前端工程师"),
                new("hr", "钱人事", "hr@enterprise.demo", "人事专员"),
                new("auditor", "孙审计", "auditor@enterprise.demo", "审计员"),
                new("lead", "周负责人", "lead@enterprise.demo", "项目负责人"),
                new("scoped", "吴专员", "scoped@enterprise.demo", "成员级数据范围覆盖"),
                new("direct", "郑专员", "direct@enterprise.demo", "直授与禁止权限"),
                new("temp", "冯临时", "temp@enterprise.demo", "角色授权的时间窗"),
                new("intern", "陈实习", "intern@enterprise.demo", "访客"),
                new("left", "褚离职", "left@enterprise.demo", "成员身份已到期"),
                new("disabled", "卫停用", "disabled@enterprise.demo", "账号已停用：登录被拒绝", Status: EnableStatus.Disabled),
                new("locked", "蒋锁定", "locked@enterprise.demo", "账号已锁定：一天内登录被拒绝", Locked: true),
                new("removed", "沈移出", "removed@enterprise.demo", "已被移出本租户"),
                new("holder", "韩停角", "holder@enterprise.demo", "只持有已停用的角色"),
            ],
            Members:
            [
                new("owner@enterprise.demo", TenantMemberType.Owner, "所有者：套餐范围内的全部权限，所有权由平台转移", Department: "hq", Position: "gm", IsLeader: true),
                new("admin@enterprise.demo", TenantMemberType.Admin, "管理员成员，持有租户管理员角色", ["tenant_admin"], "hq", "manager"),
                new("manager@enterprise.demo", TenantMemberType.Member, "部门主管：继承普通员工，看研发中心及下级的数据", ["dept_manager"], "rd", "manager", IsLeader: true),
                new("engineer@enterprise.demo", TenantMemberType.Member, "普通员工：只看本人数据", ["employee"], "rd-fe", "engineer"),
                new("hr@enterprise.demo", TenantMemberType.Member, "自定义数据范围：研发中心（含下级）与市场部", ["hr_specialist"], "hr", "specialist"),
                new("auditor@enterprise.demo", TenantMemberType.Member, "持有全局角色模板：日志只读", ["global_auditor"], "hq", "specialist"),
                new("lead@enterprise.demo", TenantMemberType.Member, "占满了项目负责人的唯一名额", ["project_lead"], "rd-be", "engineer"),
                new("scoped@enterprise.demo", TenantMemberType.Member, "成员级数据范围覆盖为本部门（优先于角色的本人数据）", ["employee"], "mkt", "specialist",
                    DataScopeOverride: DataPermissionScope.DepartmentOnly),
                new("direct@enterprise.demo", TenantMemberType.Member, "直授成员查看、禁止聊天发送（禁止优先于角色授予）", ["employee"], "mkt", "specialist",
                    Permissions: [new(P.User.Read, PermissionAction.Grant), new("chat:send", PermissionAction.Deny, IsModuleCode: true)]),
                new("temp@enterprise.demo", TenantMemberType.Member, "只读查看昨天已到期；部门主管 7 天后才生效", [new("viewer", ExpiresInDays: -1), new("dept_manager", EffectiveInDays: 7)], "rd", "engineer"),
                new("intern@enterprise.demo", TenantMemberType.Guest, "访客：成员身份 30 天后到期", ["viewer"], "rd", "intern", ExpiresInDays: 30),
                new("left@enterprise.demo", TenantMemberType.Member, "成员身份昨天已到期：进不了本租户", ["employee"], "mkt", ExpiresInDays: -1),
                new("disabled@enterprise.demo", TenantMemberType.Member, "账号已停用", ["employee"], "hr"),
                new("locked@enterprise.demo", TenantMemberType.Member, "账号已锁定", ["employee"], "hr"),
                new("removed@enterprise.demo", TenantMemberType.Member, "已移出本租户：成员关系已撤销", ["employee"], "mkt", InviteStatus: TenantMemberInviteStatus.Revoked),
                new("holder@enterprise.demo", TenantMemberType.Member, "只持有已停用的角色：没有任何权限", ["archived"], "hr"),
                new("partner@pro.demo", TenantMemberType.External, "外部协作者：账号注册在专业版租户，同时是这里的成员（一个账号多个租户）", ["viewer"]),
                new("consultant@basic.demo", TenantMemberType.Consultant, "顾问：账号注册在基础版租户，持有全局审计角色，90 天后到期", ["global_auditor"], ExpiresInDays: 90),
                new("invitee@free.demo", TenantMemberType.Member, "待接受的邀请：接受前进不了本租户", ["employee"], InviteStatus: TenantMemberInviteStatus.Pending),
                new("decliner@free.demo", TenantMemberType.Member, "已拒绝的邀请", InviteStatus: TenantMemberInviteStatus.Rejected),
                new("member@basic.demo", TenantMemberType.Member, "邀请已过期：没有在期限内接受", InviteStatus: TenantMemberInviteStatus.Expired),
                new("support@platform.demo", TenantMemberType.PlatformAdmin, "平台支持人员入驻：7 天支持窗口，不占席位", ["viewer"], ExpiresInDays: 7),
            ]),
        new("demo-pro", "示例公司（专业版）", SaasEditionSeeder.Codes.Pro,
            "专业版正常租户：高级授权与审计可用，各模块功能不在白名单里",
            Roles: [TenantAdminRole, EmployeeRole],
            Accounts:
            [
                new("owner", "专业版所有者", "owner@pro.demo", "租户所有者"),
                new("admin", "专业版管理员", "admin@pro.demo", "租户管理员"),
                new("member", "专业版员工", "member@pro.demo", "普通员工"),
                new("partner", "专业版合作方", "partner@pro.demo", "同时是企业版租户的外部协作者"),
            ],
            Members:
            [
                new("owner@pro.demo", TenantMemberType.Owner, "所有者"),
                new("admin@pro.demo", TenantMemberType.Admin, "管理员成员", ["tenant_admin"]),
                new("member@pro.demo", TenantMemberType.Member, "普通员工", ["employee"]),
                new("partner@pro.demo", TenantMemberType.Member, "本租户员工，也加入了企业版租户", ["employee"]),
                new("support@platform.demo", TenantMemberType.PlatformAdmin, "平台支持人员：支持窗口昨天已结束", ["employee"], ExpiresInDays: -1),
            ]),
        new("demo-basic", "示例工作室（基础版）", SaasEditionSeeder.Codes.Basic,
            "基础版正常租户：组织、用户、角色的日常管理",
            Roles: [TenantAdminRole, EmployeeRole],
            Accounts:
            [
                new("owner", "基础版所有者", "owner@basic.demo", "租户所有者"),
                new("member", "基础版员工", "member@basic.demo", "普通员工"),
                new("consultant", "基础版顾问", "consultant@basic.demo", "同时是企业版租户的顾问"),
            ],
            Members:
            [
                new("owner@basic.demo", TenantMemberType.Owner, "所有者"),
                new("member@basic.demo", TenantMemberType.Member, "普通员工", ["employee"]),
                new("consultant@basic.demo", TenantMemberType.Member, "本租户员工，也以顾问身份加入了企业版租户", ["employee"]),
            ]),
        new("demo-free", "示例团队（免费版）", SaasEditionSeeder.Codes.Free,
            "免费版正常租户：成员与组织以查看为主",
            Roles: [TenantAdminRole, EmployeeRole],
            Accounts:
            [
                new("owner", "免费版所有者", "owner@free.demo", "租户所有者"),
                new("member", "免费版员工", "member@free.demo", "普通员工"),
                new("invitee", "被邀请人", "invitee@free.demo", "收到了企业版租户的邀请，还没接受"),
                new("decliner", "拒绝邀请的人", "decliner@free.demo", "拒绝了企业版租户的邀请"),
            ],
            Members:
            [
                new("owner@free.demo", TenantMemberType.Owner, "所有者"),
                new("member@free.demo", TenantMemberType.Member, "普通员工", ["employee"]),
                new("invitee@free.demo", TenantMemberType.Member, "普通员工", ["employee"]),
                new("decliner@free.demo", TenantMemberType.Member, "普通员工", ["employee"]),
            ]),
        new("demo-seats-full", "席位已满（限 2 席）", SaasEditionSeeder.Codes.Free,
            "席位上限覆盖为 2 且已占满：再添加成员会被拒绝",
            UserLimit: 2,
            Roles: [EmployeeRole],
            Accounts:
            [
                new("owner", "满席租户所有者", "owner@seats-full.demo", "租户所有者"),
                new("member", "满席租户员工", "member@seats-full.demo", "占了第二个席位"),
            ],
            Members:
            [
                new("owner@seats-full.demo", TenantMemberType.Owner, "所有者，占一个席位"),
                new("member@seats-full.demo", TenantMemberType.Member, "占了第二个席位", ["employee"]),
            ]),
        new("demo-suspended", "已暂停的租户", SaasEditionSeeder.Codes.Basic,
            "租户已暂停：成员登录后进不了，平台可以恢复",
            Status: TenantStatus.Suspended,
            Accounts: [new("owner", "暂停租户所有者", "owner@suspended.demo", "租户所有者")],
            Members: [new("owner@suspended.demo", TenantMemberType.Owner, "所有者")]),
        new("demo-expired", "已到期的租户", SaasEditionSeeder.Codes.Pro,
            "租户昨天已到期（状态为过期）：成员登录后进不了，平台续期后恢复",
            Status: TenantStatus.Expired,
            ExpiresInDays: -1,
            Accounts: [new("owner", "到期租户所有者", "owner@expired.demo", "租户所有者")],
            Members: [new("owner@expired.demo", TenantMemberType.Owner, "所有者")]),
        new("demo-disabled", "已停用的租户", SaasEditionSeeder.Codes.Free,
            "租户已停用：成员登录后进不了，平台可以删除",
            Status: TenantStatus.Disabled,
            Accounts: [new("owner", "停用租户所有者", "owner@disabled.demo", "租户所有者")],
            Members: [new("owner@disabled.demo", TenantMemberType.Owner, "所有者")]),
        new("demo-awaiting-admin", "待开通管理员", SaasEditionSeeder.Codes.Free,
            "字段隔离租户已建好、还没有所有者：在租户详情里开通管理员"),
        new("demo-dedicated-db", "独立库·待初始化", SaasEditionSeeder.Codes.Free,
            "库隔离租户：先初始化数据库，再开通管理员",
            Isolation: TenantIsolationMode.Database,
            ConfigStatus: TenantConfigStatus.Pending),
    ];
}
