#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasPermissionDefinitions
// Guid:3e7b2f4a-91c5-4d68-b8e0-6f2a0c5d9e13
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Permissions;

/// <summary>
/// SaaS 权限项（组内单条权限：码 + 显示名 + 描述 + 是否需审计 + 排序）
/// </summary>
public sealed record SaasPermissionItem(
    string PermissionCode,
    string PermissionName,
    string PermissionDescription,
    bool IsRequireAudit,
    int Sort);

/// <summary>
/// SaaS 权限功能分组（组码 + 中文组名 + 该组下全部权限项）
/// </summary>
public sealed record SaasPermissionGroup(
    string GroupCode,
    string GroupName,
    IReadOnlyList<SaasPermissionItem> Permissions);

/// <summary>
/// SaaS 权限种子定义项（落库扁平结构，由 <see cref="SaasPermissionDefinitions.Groups"/> 派生）
/// </summary>
public sealed record SaasPermissionDefinition(
    string ModuleCode,
    string PermissionCode,
    string PermissionName,
    string PermissionDescription,
    string Tags,
    bool IsRequireAudit,
    int Priority,
    int Sort);

/// <summary>
/// SaaS 权限单一事实源：分组与权限定义在一起
/// </summary>
/// <remarks>
/// 唯一手写源是 <see cref="Groups"/>：每个资源块一个分组节点，组名与该组权限写在一起。
/// 落库扁平表 <see cref="All"/>、组码→组名 <see cref="GroupNames"/> 均由 <see cref="Groups"/> 派生，
/// 每条权限的 ModuleCode（恒为 saas）、Tags（[module, 组码](+export/import 段)）、Priority（恒等于 Sort）自动生成，无需手写。
/// 新增资源时只在 <see cref="Groups"/> 增一个分组节点或在已有节点增一条权限项即可。
/// </remarks>
public static class SaasPermissionDefinitions
{
    /// <summary>
    /// 全部权限分组（手写单一事实源）
    /// </summary>
    public static IReadOnlyList<SaasPermissionGroup> Groups { get; } =
    [
        new(SaasPermissionCodes.Tenant.Group, "租户",
        [
            new(SaasPermissionCodes.Tenant.Read, "租户查看", "查看当前用户可进入的租户列表", false, 100),
            new(SaasPermissionCodes.Tenant.Create, "租户创建", "创建租户基础资料", true, 110),
            new(SaasPermissionCodes.Tenant.Update, "租户更新", "更新租户基础资料", true, 120),
            new(SaasPermissionCodes.Tenant.Status, "租户状态", "更新租户生命周期状态", true, 130),
            new(SaasPermissionCodes.Tenant.Export, "租户导出", "导出当前数据范围内的租户列表数据", false, 2590),
        ]),
        new(SaasPermissionCodes.TenantMember.Group, "租户成员",
        [
            new(SaasPermissionCodes.TenantMember.Read, "租户成员查看", "查看当前租户成员列表和详情", false, 135),
            new(SaasPermissionCodes.TenantMember.Update, "租户成员更新", "更新当前租户成员资料和有效期", true, 136),
            new(SaasPermissionCodes.TenantMember.Status, "租户成员状态", "更新当前租户成员启停状态", true, 137),
            new(SaasPermissionCodes.TenantMember.InviteStatus, "租户成员邀请状态", "更新当前租户成员邀请生命周期状态", true, 138),
            new(SaasPermissionCodes.TenantMember.Revoke, "租户成员撤销", "撤销当前租户成员身份", true, 139),
        ]),
        new(SaasPermissionCodes.TenantEdition.Group, "租户版本",
        [
            new(SaasPermissionCodes.TenantEdition.Read, "租户版本查看", "查看租户版本套餐列表和详情", false, 140),
            new(SaasPermissionCodes.TenantEdition.Create, "租户版本创建", "创建租户版本套餐", true, 150),
            new(SaasPermissionCodes.TenantEdition.Update, "租户版本更新", "更新租户版本套餐", true, 160),
            new(SaasPermissionCodes.TenantEdition.Status, "租户版本状态", "更新租户版本上下架状态", true, 170),
            new(SaasPermissionCodes.TenantEdition.Default, "租户版本默认", "设置默认租户版本", true, 180),
            new(SaasPermissionCodes.TenantEdition.Export, "版本套餐导出", "导出当前数据范围内的租户版本套餐列表数据", false, 2720),
        ]),
        new(SaasPermissionCodes.TenantEditionPermission.Group, "租户版本权限",
        [
            new(SaasPermissionCodes.TenantEditionPermission.Read, "租户版本权限查看", "查看租户版本可用权限绑定", false, 190),
            new(SaasPermissionCodes.TenantEditionPermission.Grant, "租户版本权限授权", "授予租户版本可用权限", true, 200),
            new(SaasPermissionCodes.TenantEditionPermission.Update, "租户版本权限更新", "更新租户版本权限绑定状态", true, 210),
            new(SaasPermissionCodes.TenantEditionPermission.Revoke, "租户版本权限撤销", "撤销租户版本可用权限", true, 220),
        ]),
        new(SaasPermissionCodes.Permission.Group, "权限定义",
        [
            new(SaasPermissionCodes.Permission.Read, "权限定义查看", "查看权限定义列表、详情和全局权限选择项", false, 230),
            new(SaasPermissionCodes.Permission.Create, "权限定义创建", "创建当前租户权限定义", true, 231),
            new(SaasPermissionCodes.Permission.Update, "权限定义更新", "更新当前租户权限定义基础资料", true, 232),
            new(SaasPermissionCodes.Permission.Status, "权限定义状态", "更新当前租户权限定义状态", true, 233),
            new(SaasPermissionCodes.Permission.Delete, "权限定义删除", "删除当前租户权限定义", true, 234),
            new(SaasPermissionCodes.Permission.Export, "权限定义导出", "导出当前数据范围内的权限定义列表数据", false, 2560),
        ]),
        new(SaasPermissionCodes.Resource.Group, "资源定义",
        [
            new(SaasPermissionCodes.Resource.Read, "资源定义查看", "查看资源定义列表、详情和全局资源选择项", false, 240),
            new(SaasPermissionCodes.Resource.Create, "资源定义创建", "创建当前租户资源定义", true, 241),
            new(SaasPermissionCodes.Resource.Update, "资源定义更新", "更新当前租户资源定义", true, 242),
            new(SaasPermissionCodes.Resource.Status, "资源定义状态", "更新当前租户资源定义状态", true, 243),
            new(SaasPermissionCodes.Resource.Delete, "资源定义删除", "删除当前租户资源定义", true, 244),
        ]),
        new(SaasPermissionCodes.Operation.Group, "操作定义",
        [
            new(SaasPermissionCodes.Operation.Read, "操作定义查看", "查看操作定义列表、详情和全局操作选择项", false, 250),
            new(SaasPermissionCodes.Operation.Create, "操作定义创建", "创建当前租户操作定义", true, 251),
            new(SaasPermissionCodes.Operation.Update, "操作定义更新", "更新当前租户操作定义", true, 252),
            new(SaasPermissionCodes.Operation.Status, "操作定义状态", "更新当前租户操作定义状态", true, 253),
            new(SaasPermissionCodes.Operation.Delete, "操作定义删除", "删除当前租户操作定义", true, 254),
        ]),
        new(SaasPermissionCodes.Menu.Group, "菜单",
        [
            new(SaasPermissionCodes.Menu.Read, "菜单查看", "查看菜单列表、详情和树形结构", false, 255),
            new(SaasPermissionCodes.Menu.Create, "菜单创建", "创建当前租户菜单", true, 256),
            new(SaasPermissionCodes.Menu.Update, "菜单更新", "更新当前租户菜单", true, 257),
            new(SaasPermissionCodes.Menu.Status, "菜单状态", "更新当前租户菜单状态", true, 258),
            new(SaasPermissionCodes.Menu.Delete, "菜单删除", "删除当前租户菜单", true, 259),
            new(SaasPermissionCodes.Menu.Export, "菜单导出", "导出当前数据范围内的菜单列表数据", false, 2640),
        ]),
        new(SaasPermissionCodes.Role.Group, "角色",
        [
            new(SaasPermissionCodes.Role.Read, "角色定义查看", "查看角色定义列表、详情和已启用角色选择项", false, 260),
            new(SaasPermissionCodes.Role.Create, "角色定义创建", "创建当前租户角色定义", true, 262),
            new(SaasPermissionCodes.Role.Update, "角色定义更新", "更新当前租户角色基础资料", true, 264),
            new(SaasPermissionCodes.Role.Status, "角色定义状态", "更新当前租户角色启停状态", true, 266),
            new(SaasPermissionCodes.Role.Delete, "角色定义删除", "删除当前租户未被引用的角色定义", true, 268),
            new(SaasPermissionCodes.Role.Export, "角色导出", "导出当前数据范围内的角色列表数据", false, 2540),
        ]),
        new(SaasPermissionCodes.RoleHierarchy.Group, "角色继承",
        [
            new(SaasPermissionCodes.RoleHierarchy.Read, "角色继承查看", "查看角色继承祖先链、后代链和详情", false, 269),
            new(SaasPermissionCodes.RoleHierarchy.Create, "角色继承创建", "创建角色直接继承关系并补齐闭包记录", true, 270),
            new(SaasPermissionCodes.RoleHierarchy.Delete, "角色继承删除", "删除角色直接继承关系并清理派生闭包记录", true, 271),
        ]),
        new(SaasPermissionCodes.RoleDataScope.Group, "角色数据范围",
        [
            new(SaasPermissionCodes.RoleDataScope.Read, "角色数据范围查看", "查看角色自定义数据范围列表和详情", false, 272),
            new(SaasPermissionCodes.RoleDataScope.Grant, "角色数据范围授权", "授予角色自定义数据范围", true, 273),
            new(SaasPermissionCodes.RoleDataScope.Update, "角色数据范围更新", "更新角色数据范围有效期和包含子部门设置", true, 274),
            new(SaasPermissionCodes.RoleDataScope.Status, "角色数据范围状态", "更新角色数据范围绑定状态", true, 275),
            new(SaasPermissionCodes.RoleDataScope.Revoke, "角色数据范围撤销", "撤销角色数据范围绑定", true, 276),
        ]),
        new(SaasPermissionCodes.RolePermission.Group, "角色权限",
        [
            new(SaasPermissionCodes.RolePermission.Read, "角色权限查看", "查看角色权限绑定列表和详情", false, 280),
            new(SaasPermissionCodes.RolePermission.Grant, "角色权限授权", "授予或拒绝角色权限", true, 290),
            new(SaasPermissionCodes.RolePermission.Update, "角色权限更新", "更新角色权限操作和有效期", true, 300),
            new(SaasPermissionCodes.RolePermission.Status, "角色权限状态", "更新角色权限绑定状态", true, 310),
            new(SaasPermissionCodes.RolePermission.Revoke, "角色权限撤销", "撤销角色权限绑定", true, 320),
        ]),
        new(SaasPermissionCodes.UserRole.Group, "用户角色",
        [
            new(SaasPermissionCodes.UserRole.Read, "用户角色查看", "查看当前租户用户角色绑定列表和详情", false, 330),
            new(SaasPermissionCodes.UserRole.Grant, "用户角色授权", "授予当前租户成员角色", true, 340),
            new(SaasPermissionCodes.UserRole.Update, "用户角色更新", "更新用户角色授权原因和有效期", true, 350),
            new(SaasPermissionCodes.UserRole.Status, "用户角色状态", "更新用户角色绑定状态", true, 360),
            new(SaasPermissionCodes.UserRole.Revoke, "用户角色撤销", "撤销用户角色绑定", true, 370),
        ]),
        new(SaasPermissionCodes.UserPermission.Group, "用户直授权限",
        [
            new(SaasPermissionCodes.UserPermission.Read, "用户直授权限查看", "查看当前租户用户直授权限列表和详情", false, 380),
            new(SaasPermissionCodes.UserPermission.Grant, "用户直授权限授权", "授予或拒绝当前租户成员直授权限", true, 390),
            new(SaasPermissionCodes.UserPermission.Update, "用户直授权限更新", "更新用户直授权限操作、授权原因和有效期", true, 400),
            new(SaasPermissionCodes.UserPermission.Status, "用户直授权限状态", "更新用户直授权限绑定状态", true, 410),
            new(SaasPermissionCodes.UserPermission.Revoke, "用户直授权限撤销", "撤销用户直授权限绑定", true, 420),
        ]),
        new(SaasPermissionCodes.UserDataScope.Group, "用户数据范围",
        [
            new(SaasPermissionCodes.UserDataScope.Read, "用户数据范围查看", "查看当前租户用户数据范围覆盖列表和详情", false, 430),
            new(SaasPermissionCodes.UserDataScope.Grant, "用户数据范围授权", "授予当前租户成员数据范围覆盖", true, 440),
            new(SaasPermissionCodes.UserDataScope.Update, "用户数据范围更新", "更新用户数据范围覆盖模式和部门设置", true, 450),
            new(SaasPermissionCodes.UserDataScope.Status, "用户数据范围状态", "更新用户数据范围绑定状态", true, 460),
            new(SaasPermissionCodes.UserDataScope.Revoke, "用户数据范围撤销", "撤销用户数据范围绑定", true, 470),
        ]),
        new(SaasPermissionCodes.FieldLevelSecurity.Group, "字段级安全",
        [
            new(SaasPermissionCodes.FieldLevelSecurity.Read, "字段级安全查看", "查看字段级安全策略列表和详情", false, 480),
            new(SaasPermissionCodes.FieldLevelSecurity.Create, "字段级安全创建", "创建字段级安全策略", true, 490),
            new(SaasPermissionCodes.FieldLevelSecurity.Update, "字段级安全更新", "更新字段级安全策略", true, 500),
            new(SaasPermissionCodes.FieldLevelSecurity.Status, "字段级安全状态", "更新字段级安全策略状态", true, 510),
            new(SaasPermissionCodes.FieldLevelSecurity.Delete, "字段级安全删除", "删除字段级安全策略", true, 520),
            new(SaasPermissionCodes.FieldLevelSecurity.Export, "字段级安全导出", "导出当前数据范围内的字段级安全策略列表数据", false, 2570),
        ]),
        new(SaasPermissionCodes.Department.Group, "部门",
        [
            new(SaasPermissionCodes.Department.Read, "部门查看", "查看部门列表、详情和组织树", false, 500),
            new(SaasPermissionCodes.Department.Create, "部门创建", "创建当前租户部门", true, 501),
            new(SaasPermissionCodes.Department.Update, "部门更新", "更新当前租户部门基础资料和组织位置", true, 502),
            new(SaasPermissionCodes.Department.Status, "部门状态", "更新当前租户部门状态", true, 503),
            new(SaasPermissionCodes.Department.Delete, "部门删除", "删除当前租户部门", true, 504),
            new(SaasPermissionCodes.Department.Export, "部门导出", "导出当前数据范围内的部门列表数据", false, 2550),
        ]),
        new(SaasPermissionCodes.UserDepartment.Group, "用户部门",
        [
            new(SaasPermissionCodes.UserDepartment.Read, "用户部门归属查看", "查看用户部门归属和部门成员归属", false, 510),
            new(SaasPermissionCodes.UserDepartment.Grant, "用户部门归属分配", "为当前租户成员分配部门归属", true, 511),
            new(SaasPermissionCodes.UserDepartment.Update, "用户部门归属更新", "更新当前租户成员部门归属资料", true, 512),
            new(SaasPermissionCodes.UserDepartment.Status, "用户部门归属状态", "更新当前租户成员部门归属状态", true, 513),
            new(SaasPermissionCodes.UserDepartment.Revoke, "用户部门归属撤销", "撤销当前租户成员部门归属", true, 514),
        ]),
        new(SaasPermissionCodes.User.Group, "用户",
        [
            new(SaasPermissionCodes.User.Read, "用户查看", "查看用户列表、详情和已启用用户选择项", false, 520),
            new(SaasPermissionCodes.User.Create, "用户创建", "创建当前租户用户并初始化安全资料和成员身份", true, 521),
            new(SaasPermissionCodes.User.Update, "用户更新", "更新当前租户用户基础资料", true, 522),
            new(SaasPermissionCodes.User.Status, "用户状态", "更新当前租户用户启停状态", true, 523),
            new(SaasPermissionCodes.User.Delete, "用户删除", "删除当前租户用户并撤销当前租户成员身份", true, 524),
            new(SaasPermissionCodes.User.Export, "用户导出", "导出当前数据范围内的用户列表数据", false, 2460),
        ]),
        new(SaasPermissionCodes.UserSecurity.Group, "用户安全",
        [
            new(SaasPermissionCodes.UserSecurity.Read, "用户安全查看", "查看当前租户用户安全状态", true, 525),
            new(SaasPermissionCodes.UserSecurity.ResetPassword, "用户密码重置", "重置当前租户用户密码", true, 526),
            new(SaasPermissionCodes.UserSecurity.Lock, "用户锁定", "更新当前租户用户锁定状态", true, 527),
            new(SaasPermissionCodes.UserSecurity.LoginPolicy, "用户登录策略", "更新当前租户用户多端登录策略", true, 528),
            new(SaasPermissionCodes.UserSecurity.ResetTwoFactor, "用户双因素重置", "重置当前租户用户双因素认证（OTP），清除绑定后用户可重新设置", true, 2450),
        ]),
        new(SaasPermissionCodes.UserSession.Group, "用户会话",
        [
            new(SaasPermissionCodes.UserSession.Read, "用户会话查看", "查看当前租户用户会话摘要", true, 529),
            new(SaasPermissionCodes.UserSession.Revoke, "用户会话撤销", "撤销当前租户用户会话", true, 530),
            new(SaasPermissionCodes.UserSession.Export, "会话导出", "导出当前数据范围内的用户会话列表数据", false, 2690),
        ]),
        new(SaasPermissionCodes.PermissionDelegation.Group, "权限委托",
        [
            new(SaasPermissionCodes.PermissionDelegation.Read, "权限委托查看", "查看当前租户权限委托列表和详情", false, 530),
            new(SaasPermissionCodes.PermissionDelegation.Create, "权限委托创建", "创建当前租户权限委托", true, 540),
            new(SaasPermissionCodes.PermissionDelegation.Update, "权限委托更新", "更新当前租户权限委托", true, 550),
            new(SaasPermissionCodes.PermissionDelegation.Status, "权限委托状态", "更新当前租户权限委托状态", true, 560),
            new(SaasPermissionCodes.PermissionDelegation.Revoke, "权限委托撤销", "撤销当前租户权限委托", true, 570),
        ]),
        new(SaasPermissionCodes.UserStatistics.Group, "用户统计",
        [
            new(SaasPermissionCodes.UserStatistics.Read, "用户统计查看", "查看当前租户用户行为统计", true, 531),
        ]),
        new(SaasPermissionCodes.PasswordHistory.Group, "密码历史",
        [
            new(SaasPermissionCodes.PasswordHistory.Read, "密码历史查看", "查看当前租户用户密码修改历史", true, 532),
        ]),
        new(SaasPermissionCodes.ExternalLogin.Group, "第三方登录",
        [
            new(SaasPermissionCodes.ExternalLogin.Read, "第三方登录绑定查看", "查看当前租户用户第三方登录绑定摘要", true, 533),
        ]),
        new(SaasPermissionCodes.SessionRole.Group, "会话角色",
        [
            new(SaasPermissionCodes.SessionRole.Read, "会话角色查看", "查看当前租户用户会话激活角色", true, 534),
        ]),
        new(SaasPermissionCodes.OAuthApp.Group, "OAuth 应用",
        [
            new(SaasPermissionCodes.OAuthApp.Read, "OAuth应用查看", "查看当前租户 OAuth 应用注册信息", true, 535),
            new(SaasPermissionCodes.OAuthApp.Create, "OAuth应用创建", "创建当前租户 OAuth 应用", true, 2001),
            new(SaasPermissionCodes.OAuthApp.Update, "OAuth应用更新", "更新当前租户 OAuth 应用资料", true, 2002),
            new(SaasPermissionCodes.OAuthApp.Status, "OAuth应用状态", "更新当前租户 OAuth 应用启停状态", true, 2003),
            new(SaasPermissionCodes.OAuthApp.Delete, "OAuth应用删除", "删除当前租户 OAuth 应用", true, 2004),
            new(SaasPermissionCodes.OAuthApp.Secret, "OAuth应用密钥", "重置当前租户 OAuth 应用密钥", true, 2005),
            new(SaasPermissionCodes.OAuthApp.Export, "OAuth应用导出", "导出当前数据范围内的 OAuth 应用列表数据", false, 2630),
        ]),
        new(SaasPermissionCodes.OAuthCode.Group, "OAuth 授权码",
        [
            new(SaasPermissionCodes.OAuthCode.Read, "OAuth授权码查看", "查看当前租户 OAuth 授权码审计状态", true, 536),
        ]),
        new(SaasPermissionCodes.OAuthToken.Group, "OAuth Token",
        [
            new(SaasPermissionCodes.OAuthToken.Read, "OAuth Token查看", "查看当前租户 OAuth Token 生命周期状态", true, 537),
        ]),
        new(SaasPermissionCodes.AccessLog.Group, "访问日志",
        [
            new(SaasPermissionCodes.AccessLog.Read, "访问日志查看", "查看当前租户访问日志摘要", true, 538),
            new(SaasPermissionCodes.AccessLog.Export, "访问日志导出", "导出访问日志数据", false, 2480),
        ]),
        new(SaasPermissionCodes.ApiLog.Group, "API 日志",
        [
            new(SaasPermissionCodes.ApiLog.Read, "API日志查看", "查看当前租户 API 调用日志摘要", true, 539),
            new(SaasPermissionCodes.ApiLog.Export, "API日志导出", "导出开放接口日志数据", false, 2490),
        ]),
        new(SaasPermissionCodes.PermissionRequest.Group, "权限申请",
        [
            new(SaasPermissionCodes.PermissionRequest.Read, "权限申请查看", "查看当前租户权限申请列表和详情", false, 580),
            new(SaasPermissionCodes.PermissionRequest.Create, "权限申请创建", "提交当前租户权限申请", true, 590),
            new(SaasPermissionCodes.PermissionRequest.Update, "权限申请更新", "更新当前租户权限申请", true, 600),
            new(SaasPermissionCodes.PermissionRequest.Status, "权限申请状态", "更新当前租户权限申请状态", true, 610),
            new(SaasPermissionCodes.PermissionRequest.Withdraw, "权限申请撤回", "撤回当前租户权限申请", true, 620),
            new(SaasPermissionCodes.PermissionRequest.Export, "权限申请导出", "导出当前数据范围内的权限申请列表数据", false, 2580),
        ]),
        new(SaasPermissionCodes.ConstraintRule.Group, "约束规则",
        [
            new(SaasPermissionCodes.ConstraintRule.Read, "约束规则查看", "查看当前租户约束规则列表和详情", false, 630),
            new(SaasPermissionCodes.ConstraintRule.Create, "约束规则创建", "创建当前租户约束规则", true, 640),
            new(SaasPermissionCodes.ConstraintRule.Update, "约束规则更新", "更新当前租户约束规则", true, 650),
            new(SaasPermissionCodes.ConstraintRule.Status, "约束规则状态", "更新当前租户约束规则状态", true, 660),
            new(SaasPermissionCodes.ConstraintRule.Delete, "约束规则删除", "删除当前租户约束规则", true, 670),
            new(SaasPermissionCodes.ConstraintRule.Export, "约束规则导出", "导出当前数据范围内的约束规则列表数据", false, 2740),
        ]),
        new(SaasPermissionCodes.PermissionCondition.Group, "权限条件",
        [
            new(SaasPermissionCodes.PermissionCondition.Read, "权限ABAC条件查看", "查看角色或用户授权绑定的ABAC条件", false, 680),
            new(SaasPermissionCodes.PermissionCondition.Create, "权限ABAC条件创建", "创建角色或用户授权绑定的ABAC条件", true, 690),
            new(SaasPermissionCodes.PermissionCondition.Update, "权限ABAC条件更新", "更新角色或用户授权绑定的ABAC条件", true, 700),
            new(SaasPermissionCodes.PermissionCondition.Status, "权限ABAC条件状态", "更新权限ABAC条件状态", true, 710),
            new(SaasPermissionCodes.PermissionCondition.Delete, "权限ABAC条件删除", "删除权限ABAC条件", true, 720),
        ]),
        new(SaasPermissionCodes.DiffLog.Group, "数据变更日志",
        [
            new(SaasPermissionCodes.DiffLog.Read, "差异日志查看", "查看当前租户数据变更审计摘要", true, 800),
            new(SaasPermissionCodes.DiffLog.Export, "数据变更日志导出", "导出数据变更（差异）日志数据", false, 2520),
        ]),
        new(SaasPermissionCodes.ExceptionLog.Group, "异常日志",
        [
            new(SaasPermissionCodes.ExceptionLog.Read, "异常日志查看", "查看当前租户异常日志摘要", true, 810),
            new(SaasPermissionCodes.ExceptionLog.Export, "异常日志导出", "导出异常日志数据", false, 2510),
        ]),
        new(SaasPermissionCodes.LoginLog.Group, "登录日志",
        [
            new(SaasPermissionCodes.LoginLog.Read, "登录日志查看", "查看当前租户登录日志摘要", true, 820),
            new(SaasPermissionCodes.LoginLog.Export, "登录日志导出", "导出登录日志数据", false, 2500),
        ]),
        new(SaasPermissionCodes.OperationLog.Group, "操作日志",
        [
            new(SaasPermissionCodes.OperationLog.Read, "操作日志查看", "查看当前租户操作日志摘要", true, 830),
            new(SaasPermissionCodes.OperationLog.Export, "操作日志导出", "导出操作日志数据", false, 2470),
        ]),
        new(SaasPermissionCodes.PermissionChangeLog.Group, "权限变更日志",
        [
            new(SaasPermissionCodes.PermissionChangeLog.Read, "权限变更日志查看", "查看当前租户权限变更审计摘要", true, 840),
        ]),
        new(SaasPermissionCodes.Task.Group, "系统任务",
        [
            new(SaasPermissionCodes.Task.Read, "系统任务查看", "查看当前租户系统任务配置摘要", true, 845),
            new(SaasPermissionCodes.Task.Create, "系统任务创建", "创建当前租户系统任务配置", true, 2010),
            new(SaasPermissionCodes.Task.Update, "系统任务更新", "更新当前租户系统任务配置", true, 2011),
            new(SaasPermissionCodes.Task.Status, "系统任务状态", "更新当前租户系统任务启停状态", true, 2012),
            new(SaasPermissionCodes.Task.RunStatus, "系统任务运行状态", "更新当前租户系统任务运行状态", true, 2013),
            new(SaasPermissionCodes.Task.Delete, "系统任务删除", "删除当前租户系统任务配置", true, 2014),
            new(SaasPermissionCodes.Task.Export, "系统任务导出", "导出当前数据范围内的系统任务列表数据", false, 2670),
        ]),
        new(SaasPermissionCodes.TaskLog.Group, "任务日志",
        [
            new(SaasPermissionCodes.TaskLog.Read, "任务日志查看", "查看当前租户任务执行日志摘要", true, 850),
        ]),
        new(SaasPermissionCodes.Review.Group, "系统审查",
        [
            new(SaasPermissionCodes.Review.Read, "系统审查查看", "查看当前租户系统审查单摘要", true, 855),
            new(SaasPermissionCodes.Review.Create, "系统审查创建", "创建当前租户系统审查单", true, 2020),
            new(SaasPermissionCodes.Review.Update, "系统审查更新", "更新当前租户系统审查单", true, 2021),
            new(SaasPermissionCodes.Review.Status, "系统审查状态", "更新当前租户系统审查启停状态", true, 2022),
            new(SaasPermissionCodes.Review.Audit, "系统审查审核", "处理当前租户系统审查单审核结果", true, 2023),
            new(SaasPermissionCodes.Review.Withdraw, "系统审查撤回", "撤回当前租户系统审查单", true, 2024),
            new(SaasPermissionCodes.Review.Delete, "系统审查删除", "删除当前租户系统审查单", true, 2025),
            new(SaasPermissionCodes.Review.Export, "系统审查导出", "导出当前数据范围内的系统审查列表数据", false, 2610),
        ]),
        new(SaasPermissionCodes.ReviewLog.Group, "审查日志",
        [
            new(SaasPermissionCodes.ReviewLog.Read, "审查日志查看", "查看当前租户审查流转日志摘要", true, 860),
        ]),
        new(SaasPermissionCodes.Config.Group, "系统配置",
        [
            new(SaasPermissionCodes.Config.Read, "系统配置查看", "查看当前租户系统配置元数据", true, 870),
            new(SaasPermissionCodes.Config.Create, "系统配置创建", "创建当前租户系统配置", true, 2030),
            new(SaasPermissionCodes.Config.Update, "系统配置更新", "更新当前租户系统配置", true, 2031),
            new(SaasPermissionCodes.Config.Status, "系统配置状态", "更新当前租户系统配置状态", true, 2032),
            new(SaasPermissionCodes.Config.Delete, "系统配置删除", "删除当前租户系统配置", true, 2033),
            new(SaasPermissionCodes.Config.Import, "系统配置导入", "批量导入系统配置数据", true, 2530),
            new(SaasPermissionCodes.Config.Export, "系统配置导出", "导出当前数据范围内的系统配置列表数据", false, 2660),
        ]),
        new(SaasPermissionCodes.Dict.Group, "系统字典",
        [
            new(SaasPermissionCodes.Dict.Read, "系统字典查看", "查看当前租户系统字典和字典项", false, 880),
            new(SaasPermissionCodes.Dict.Create, "系统字典创建", "创建当前租户系统字典和字典项", true, 2040),
            new(SaasPermissionCodes.Dict.Update, "系统字典更新", "更新当前租户系统字典和字典项", true, 2041),
            new(SaasPermissionCodes.Dict.Status, "系统字典状态", "更新当前租户系统字典和字典项状态", true, 2042),
            new(SaasPermissionCodes.Dict.Delete, "系统字典删除", "删除当前租户系统字典和字典项", true, 2043),
            new(SaasPermissionCodes.Dict.Export, "系统字典导出", "导出当前数据范围内的系统字典列表数据", false, 2650),
        ]),
        new(SaasPermissionCodes.Version.Group, "系统版本",
        [
            new(SaasPermissionCodes.Version.Read, "系统版本查看", "查看当前租户系统版本和迁移历史", true, 890),
            new(SaasPermissionCodes.Version.Create, "系统版本创建", "创建当前租户系统版本记录", true, 2050),
            new(SaasPermissionCodes.Version.Update, "系统版本更新", "更新当前租户系统版本记录", true, 2051),
            new(SaasPermissionCodes.Version.Upgrade, "系统版本升级", "更新当前租户系统升级状态", true, 2052),
            new(SaasPermissionCodes.Version.Delete, "系统版本删除", "删除当前租户系统版本记录", true, 2053),
            new(SaasPermissionCodes.Version.Export, "版本导出", "导出当前数据范围内的系统版本列表数据", false, 2680),
        ]),
        new(SaasPermissionCodes.File.Group, "系统文件",
        [
            new(SaasPermissionCodes.File.Read, "系统文件查看", "查看当前租户文件元数据和存储副本摘要", true, 900),
            new(SaasPermissionCodes.File.Create, "系统文件创建", "创建当前租户文件元数据和存储副本", true, 2060),
            new(SaasPermissionCodes.File.Update, "系统文件更新", "更新当前租户文件元数据和存储副本", true, 2061),
            new(SaasPermissionCodes.File.Status, "系统文件状态", "更新当前租户文件和存储副本状态", true, 2062),
            new(SaasPermissionCodes.File.Delete, "系统文件删除", "删除当前租户文件元数据和存储副本", true, 2063),
            new(SaasPermissionCodes.File.Export, "系统文件导出", "导出当前数据范围内的系统文件列表数据", false, 2620),
        ]),
        new(SaasPermissionCodes.Message.Group, "系统消息",
        [
            new(SaasPermissionCodes.Message.Read, "系统消息查看", "查看当前租户邮件和短信发送摘要", true, 910),
            new(SaasPermissionCodes.Message.Create, "系统消息创建", "创建当前租户通知、邮件或短信", true, 2070),
            new(SaasPermissionCodes.Message.Update, "系统消息更新", "更新当前租户通知、邮件或短信", true, 2071),
            new(SaasPermissionCodes.Message.Status, "系统消息状态", "更新当前租户邮件或短信发送状态", true, 2072),
            new(SaasPermissionCodes.Message.Publish, "系统通知发布", "发布当前租户系统通知并生成用户站内信", true, 2073),
            new(SaasPermissionCodes.Message.Delete, "系统消息删除", "删除当前租户通知、邮件或短信", true, 2074),
            new(SaasPermissionCodes.Message.Export, "消息导出", "导出当前数据范围内的邮件短信列表数据", false, 2700),
        ]),
        new(SaasPermissionCodes.Notification.Group, "系统通知",
        [
            new(SaasPermissionCodes.Notification.Read, "系统通知查看", "查看当前租户系统通知/公告列表与详情", false, 2100),
            new(SaasPermissionCodes.Notification.Create, "系统通知创建", "创建当前租户系统通知/公告草稿", true, 2110),
            new(SaasPermissionCodes.Notification.Update, "系统通知更新", "更新当前租户系统通知/公告内容", true, 2120),
            new(SaasPermissionCodes.Notification.Publish, "系统通知发布", "发布当前租户系统通知/公告并下发用户站内信", true, 2130),
            new(SaasPermissionCodes.Notification.Delete, "系统通知删除", "删除当前租户系统通知/公告", true, 2140),
            new(SaasPermissionCodes.Notification.Export, "通知导出", "导出当前数据范围内的系统通知列表数据", false, 2710),
        ]),
        new(SaasPermissionCodes.StorageConfig.Group, "存储配置",
        [
            new(SaasPermissionCodes.StorageConfig.Read, "存储配置查看", "查看当前租户存储后端配置列表与详情", false, 2200),
            new(SaasPermissionCodes.StorageConfig.Create, "存储配置创建", "创建当前租户存储后端配置", true, 2210),
            new(SaasPermissionCodes.StorageConfig.Update, "存储配置更新", "更新当前租户存储后端配置", true, 2220),
            new(SaasPermissionCodes.StorageConfig.Status, "存储配置状态", "启用或停用当前租户存储后端配置", true, 2230),
            new(SaasPermissionCodes.StorageConfig.Delete, "存储配置删除", "删除当前租户存储后端配置", true, 2240),
            new(SaasPermissionCodes.StorageConfig.Export, "存储配置导出", "导出当前数据范围内的存储配置列表数据", false, 2730),
        ]),
        new(SaasPermissionCodes.Cache.Group, "缓存管理",
        [
            new(SaasPermissionCodes.Cache.Read, "缓存查看", "查看分布式缓存键值与统计（平台运维，可读取鉴权快照等敏感缓存明文，须审计）", true, 2300),
            new(SaasPermissionCodes.Cache.Clear, "缓存清理", "按键或模式清理分布式缓存（平台运维）", true, 2310),
        ]),
        new(SaasPermissionCodes.Server.Group, "服务监控",
        [
            new(SaasPermissionCodes.Server.Read, "服务监控查看", "查看服务器与运行时监控指标（平台运维）", false, 2320),
        ]),
        new(SaasPermissionCodes.MessageTemplate.Group, "消息模板",
        [
            new(SaasPermissionCodes.MessageTemplate.Read, "消息模板查看", "查看当前租户消息模板列表与详情", false, 2400),
            new(SaasPermissionCodes.MessageTemplate.Create, "消息模板创建", "创建当前租户消息模板（可覆盖全局默认）", true, 2410),
            new(SaasPermissionCodes.MessageTemplate.Update, "消息模板更新", "更新当前租户消息模板内容", true, 2420),
            new(SaasPermissionCodes.MessageTemplate.Status, "消息模板状态", "启用或停用当前租户消息模板", true, 2430),
            new(SaasPermissionCodes.MessageTemplate.Delete, "消息模板删除", "删除当前租户消息模板", true, 2440),
            new(SaasPermissionCodes.MessageTemplate.Export, "消息模板导出", "导出当前数据范围内的消息模板列表数据", false, 2600),
        ]),
    ];

    /// <summary>
    /// 全部权限定义（扁平，供种子使用；由 <see cref="Groups"/> 派生）
    /// </summary>
    public static IReadOnlyList<SaasPermissionDefinition> All { get; } = Groups
        .SelectMany(group => group.Permissions.Select(item => new SaasPermissionDefinition(
            SaasPermissionCodes.Module,
            item.PermissionCode,
            item.PermissionName,
            item.PermissionDescription,
            BuildTags(group.GroupCode, item.PermissionCode),
            item.IsRequireAudit,
            item.Sort,
            item.Sort)))
        .ToList();

    /// <summary>
    /// 组码 → 中文组名（由 <see cref="Groups"/> 派生）
    /// </summary>
    public static IReadOnlyDictionary<string, string> GroupNames { get; } =
        Groups.ToDictionary(group => group.GroupCode, group => group.GroupName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 取权限码的组码（资源段）：saas:{resource}:{action} → resource；无法解析时回退模块段
    /// </summary>
    public static string ResolveGroupCode(string? permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return "other";
        }

        var parts = permissionCode.Split(':', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 ? parts[1] : (parts.Length > 0 ? parts[0] : "other");
    }

    /// <summary>
    /// 取权限码的组显示名（未登记回退组码）
    /// </summary>
    public static string ResolveGroupName(string? permissionCode)
    {
        var groupCode = ResolveGroupCode(permissionCode);
        return GroupNames.TryGetValue(groupCode, out var name) ? name : groupCode;
    }

    /// <summary>
    /// 生成权限标签：[module, 组码]，导出/导入动作追加动作段（与历史落库值一致）
    /// </summary>
    private static string BuildTags(string groupCode, string permissionCode)
    {
        var parts = permissionCode.Split(':', StringSplitOptions.RemoveEmptyEntries);
        var action = parts.Length >= 3 ? parts[2] : string.Empty;
        return action is "export" or "import"
            ? $"[\"{SaasPermissionCodes.Module}\",\"{groupCode}\",\"{action}\"]"
            : $"[\"{SaasPermissionCodes.Module}\",\"{groupCode}\"]";
    }
}
