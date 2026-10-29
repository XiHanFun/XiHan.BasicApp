// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包的消息键；各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（<c>-</c> 换成 <c>_</c>）。
/// </summary>
public static class AccessMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.access";

    /// <summary>
    /// 操作 <c>roles.create</c> 的说明。
    /// </summary>
    public const string CreateRoleDescription = "roles.create.description";

    /// <summary>
    /// 操作 <c>roles.get</c> 的说明。
    /// </summary>
    public const string GetRoleDescription = "roles.get.description";

    /// <summary>
    /// 操作 <c>roles.list</c> 的说明。
    /// </summary>
    public const string ListRolesDescription = "roles.list.description";

    /// <summary>
    /// 操作 <c>roles.update</c> 的说明。
    /// </summary>
    public const string UpdateRoleDescription = "roles.update.description";

    /// <summary>
    /// 操作 <c>roles.delete</c> 的说明。
    /// </summary>
    public const string DeleteRolesDescription = "roles.delete.description";

    /// <summary>
    /// 操作 <c>roles.grant</c> 的说明。
    /// </summary>
    public const string GrantRoleDescription = "roles.grant.description";

    /// <summary>
    /// 操作 <c>roles.revoke</c> 的说明。
    /// </summary>
    public const string RevokeRoleDescription = "roles.revoke.description";

    /// <summary>
    /// 操作 <c>roles.assign</c> 的说明。
    /// </summary>
    public const string AssignRoleDescription = "roles.assign.description";

    /// <summary>
    /// 操作 <c>roles.unassign</c> 的说明。
    /// </summary>
    public const string UnassignRoleDescription = "roles.unassign.description";

    /// <summary>
    /// 操作 <c>roles.members</c> 的说明。
    /// </summary>
    public const string RoleMembersDescription = "roles.members.description";

    /// <summary>
    /// 操作 <c>permissions.list</c> 的说明。
    /// </summary>
    public const string ListPermissionsDescription = "permissions.list.description";

    /// <summary>
    /// 操作 <c>auth.permissions</c> 的说明。
    /// </summary>
    public const string PermissionsDescription = "auth.permissions.description";

    /// <summary>
    /// 角色不存在；参数 <c>id</c>。
    /// </summary>
    public const string RoleNotFound = "access.role.not_found";

    /// <summary>
    /// 角色码已被占用；参数 <c>code</c>。
    /// </summary>
    public const string RoleCodeTaken = "access.role.code_taken";

    /// <summary>
    /// 内建角色不能删除；参数 <c>code</c>。
    /// </summary>
    public const string BuiltInRole = "access.role.built_in";

    /// <summary>
    /// 列表筛选不受支持；参数 <c>field</c>、<c>operator</c>。
    /// </summary>
    public const string FilterUnsupported = "access.role.filter_unsupported";

    /// <summary>
    /// 列表排序字段不受支持；参数 <c>field</c>。
    /// </summary>
    public const string SortUnsupported = "access.role.sort_unsupported";

    /// <summary>
    /// 权限码不在目录里；参数 <c>code</c>。
    /// </summary>
    public const string PermissionUnknown = "access.permission.unknown";

    /// <summary>
    /// 权限作用侧不符；参数 <c>code</c>、<c>side</c>。
    /// </summary>
    public const string PermissionScopeMismatch = "access.permission.scope_mismatch";

    /// <summary>
    /// 用户不存在或不在角色所属租户；参数 <c>id</c>。
    /// </summary>
    public const string MemberNotFound = "access.member.not_found";

    /// <summary>
    /// 操作 <c>roles.by-user</c> 的说明。
    /// </summary>
    public const string RolesByUserDescription = "roles.by_user.description";

    /// <summary>
    /// 平台角色的数据范围只能是全部；参数 <c>code</c>。
    /// </summary>
    public const string DataScopeNotApplicable = "access.role.data_scope_not_applicable";

    /// <summary>
    /// 自定义数据范围至少一个部门；参数 <c>code</c>。
    /// </summary>
    public const string CustomScopeEmpty = "access.role.custom_scope_empty";

    /// <summary>
    /// 部门不在当前租户；参数 <c>id</c>。
    /// </summary>
    public const string DepartmentNotFound = "access.department.not_found";

    /// <summary>
    /// 租户主体只能看租户侧的权限目录。
    /// </summary>
    public const string PlatformSideDenied = "access.permission.side_denied";

    /// <summary>
    /// 操作 <c>menus.list</c> 的说明。
    /// </summary>
    public const string ListMenusDescription = "menus.list.description";

    /// <summary>
    /// 操作 <c>menus.get</c> 的说明。
    /// </summary>
    public const string GetMenuDescription = "menus.get.description";

    /// <summary>
    /// 操作 <c>menus.create</c> 的说明。
    /// </summary>
    public const string CreateMenuDescription = "menus.create.description";

    /// <summary>
    /// 操作 <c>menus.update</c> 的说明。
    /// </summary>
    public const string UpdateMenuDescription = "menus.update.description";

    /// <summary>
    /// 操作 <c>menus.reset</c> 的说明。
    /// </summary>
    public const string ResetMenuDescription = "menus.reset.description";

    /// <summary>
    /// 操作 <c>menus.delete</c> 的说明。
    /// </summary>
    public const string DeleteMenusDescription = "menus.delete.description";

    /// <summary>
    /// 菜单节点不存在；参数 <c>id</c>。
    /// </summary>
    public const string MenuNotFound = "access.menu.not_found";

    /// <summary>
    /// 菜单编码已被占用；参数 <c>code</c>。
    /// </summary>
    public const string MenuCodeTaken = "access.menu.code_taken";

    /// <summary>
    /// 页面菜单不能删除；参数 <c>code</c>。
    /// </summary>
    public const string MenuBuiltIn = "access.menu.built_in";

    /// <summary>
    /// 目录下还有节点；参数 <c>code</c>。
    /// </summary>
    public const string MenuHasChildren = "access.menu.has_children";

    /// <summary>
    /// 上级节点不合规。
    /// </summary>
    public const string MenuParentInvalid = "access.menu.parent_invalid";

    /// <summary>
    /// 外链地址不合规。
    /// </summary>
    public const string MenuUrlInvalid = "access.menu.url_invalid";

    /// <summary>
    /// 页面菜单的来源字段归页面登记。
    /// </summary>
    public const string MenuPageManaged = "access.menu.page_managed";

    /// <summary>
    /// 目录与外链必须有标题。
    /// </summary>
    public const string MenuTitleRequired = "access.menu.title_required";

    /// <summary>
    /// 不是页面菜单，没有默认值可恢复；参数 <c>code</c>。
    /// </summary>
    public const string MenuNotPage = "access.menu.not_page";

    /// <summary>
    /// 菜单只能在平台侧管理。
    /// </summary>
    public const string MenuPlatformOnly = "access.menu.platform_only";

    /// <summary>
    /// 操作 <c>roles.deny</c> 的说明。
    /// </summary>
    public const string DenyRoleDescription = "roles.deny.description";

    /// <summary>
    /// 操作 <c>roles.update-member</c> 的说明。
    /// </summary>
    public const string UpdateRoleMemberDescription = "roles.update_member.description";

    /// <summary>
    /// 操作 <c>user-grants.list</c> 的说明。
    /// </summary>
    public const string ListUserGrantsDescription = "user_grants.list.description";

    /// <summary>
    /// 操作 <c>user-grants.grant</c> 的说明。
    /// </summary>
    public const string GrantUserDescription = "user_grants.grant.description";

    /// <summary>
    /// 操作 <c>user-grants.deny</c> 的说明。
    /// </summary>
    public const string DenyUserDescription = "user_grants.deny.description";

    /// <summary>
    /// 操作 <c>user-grants.revoke</c> 的说明。
    /// </summary>
    public const string RevokeUserDescription = "user_grants.revoke.description";

    /// <summary>
    /// 操作 <c>user-data-scopes.get</c> 的说明。
    /// </summary>
    public const string GetUserDataScopeDescription = "user_data_scopes.get.description";

    /// <summary>
    /// 操作 <c>user-data-scopes.set</c> 的说明。
    /// </summary>
    public const string SetUserDataScopeDescription = "user_data_scopes.set.description";

    /// <summary>
    /// 有效期不合法；参数 <c>effectiveAt</c>、<c>expiresAt</c>。
    /// </summary>
    public const string WindowInvalid = "access.grant.window_invalid";

    /// <summary>
    /// 用户级数据范围只在租户侧。
    /// </summary>
    public const string UserDataScopeTenantOnly = "access.user_data_scope.tenant_only";

    /// <summary>
    /// 自定义范围与部门不匹配。
    /// </summary>
    public const string UserDataScopeDepartments = "access.user_data_scope.departments";

    /// <summary>
    /// 操作 <c>field-security.entities</c> 的说明。
    /// </summary>
    public const string ListFieldSecurityEntitiesDescription = "field_security.entities.description";

    /// <summary>
    /// 操作 <c>field-security.list</c> 的说明。
    /// </summary>
    public const string ListFieldSecurityRulesDescription = "field_security.list.description";

    /// <summary>
    /// 操作 <c>field-security.get</c> 的说明。
    /// </summary>
    public const string GetFieldSecurityRuleDescription = "field_security.get.description";

    /// <summary>
    /// 操作 <c>field-security.create</c> 的说明。
    /// </summary>
    public const string CreateFieldSecurityRuleDescription = "field_security.create.description";

    /// <summary>
    /// 操作 <c>field-security.update</c> 的说明。
    /// </summary>
    public const string UpdateFieldSecurityRuleDescription = "field_security.update.description";

    /// <summary>
    /// 操作 <c>field-security.delete</c> 的说明。
    /// </summary>
    public const string DeleteFieldSecurityRulesDescription = "field_security.delete.description";

    /// <summary>
    /// 字段安全规则不存在；参数 <c>id</c>。
    /// </summary>
    public const string FieldRuleNotFound = "access.field_security.not_found";

    /// <summary>
    /// 目标在字段上已有规则；参数 <c>entity</c>、<c>field</c>。
    /// </summary>
    public const string FieldRuleTaken = "access.field_security.taken";

    /// <summary>
    /// 实体没有登记字段安全；参数 <c>entity</c>。
    /// </summary>
    public const string FieldEntityUnknown = "access.field_security.entity_unknown";

    /// <summary>
    /// 实体没有这个字段；参数 <c>entity</c>、<c>field</c>。
    /// </summary>
    public const string FieldUnknown = "access.field_security.field_unknown";

    /// <summary>
    /// 读取方式不适用；参数 <c>field</c>、<c>strategy</c>。
    /// </summary>
    public const string FieldStrategyNotApplicable = "access.field_security.strategy_not_applicable";

    /// <summary>
    /// 明文且可改写的规则没有限制。
    /// </summary>
    public const string FieldRuleNoEffect = "access.field_security.no_effect";

    /// <summary>
    /// 部分脱敏保留位数不合规；参数 <c>max</c>。
    /// </summary>
    public const string FieldKeepInvalid = "access.field_security.keep_invalid";

    /// <summary>
    /// 固定文本缺失或过长；参数 <c>max</c>。
    /// </summary>
    public const string FieldReplacementInvalid = "access.field_security.replacement_invalid";

    /// <summary>
    /// 部门目标只在租户侧。
    /// </summary>
    public const string FieldTargetTenantOnly = "access.field_security.target_tenant_only";

    /// <summary>
    /// 目标已停用；参数 <c>kind</c>、<c>id</c>。
    /// </summary>
    public const string FieldTargetDisabled = "access.field_security.target_disabled";

    /// <summary>
    /// 操作 <c>roles.hierarchy</c> 的说明。
    /// </summary>
    public const string RoleHierarchyDescription = "roles.hierarchy.description";

    /// <summary>
    /// 操作 <c>roles.set-parents</c> 的说明。
    /// </summary>
    public const string SetRoleParentsDescription = "roles.set_parents.description";

    /// <summary>
    /// 操作 <c>delegations.list</c> 的说明。
    /// </summary>
    public const string ListDelegationsDescription = "delegations.list.description";

    /// <summary>
    /// 操作 <c>delegations.create</c> 的说明。
    /// </summary>
    public const string CreateDelegationDescription = "delegations.create.description";

    /// <summary>
    /// 操作 <c>delegations.revoke</c> 的说明。
    /// </summary>
    public const string RevokeDelegationDescription = "delegations.revoke.description";

    /// <summary>
    /// 操作 <c>delegations.delete</c> 的说明。
    /// </summary>
    public const string DeleteDelegationsDescription = "delegations.delete.description";

    /// <summary>
    /// 角色不能继承自己。
    /// </summary>
    public const string RoleSelfParent = "access.role.self_parent";

    /// <summary>
    /// 内置角色不参与继承；参数 <c>name</c>。
    /// </summary>
    public const string InheritanceBuiltIn = "access.role.inheritance_built_in";

    /// <summary>
    /// 停用的角色不能设为上级；参数 <c>name</c>。
    /// </summary>
    public const string ParentDisabled = "access.role.parent_disabled";

    /// <summary>
    /// 设为上级会形成环路；参数 <c>name</c>。
    /// </summary>
    public const string InheritanceCycle = "access.role.inheritance_cycle";

    /// <summary>
    /// 已经间接继承；参数 <c>name</c>。
    /// </summary>
    public const string InheritanceRedundant = "access.role.inheritance_redundant";

    /// <summary>
    /// 角色仍被其他角色继承；参数 <c>name</c>。
    /// </summary>
    public const string RoleHasChildren = "access.role.has_children";

    /// <summary>
    /// 委托人与被委托人是同一人。
    /// </summary>
    public const string DelegationSelf = "access.delegation.self";

    /// <summary>
    /// 委托目标不是恰好一个权限码或角色。
    /// </summary>
    public const string DelegationTargetInvalid = "access.delegation.target_invalid";

    /// <summary>
    /// 委托的有效期不合规；参数 <c>effectiveAt</c>、<c>expiresAt</c>。
    /// </summary>
    public const string DelegationWindowInvalid = "access.delegation.window_invalid";

    /// <summary>
    /// 委托人此刻没有所委托的权限或角色；参数 <c>target</c>。
    /// </summary>
    public const string DelegatorNotHolding = "access.delegation.delegator_not_holding";

    /// <summary>
    /// 委托的角色停用或是内置角色；参数 <c>name</c>。
    /// </summary>
    public const string DelegationRoleInvalid = "access.delegation.role_invalid";

    /// <summary>
    /// 已有一条相同且未到期的委托。
    /// </summary>
    public const string DelegationTaken = "access.delegation.taken";

    /// <summary>
    /// 委托已撤销或已到期；参数 <c>id</c>。
    /// </summary>
    public const string DelegationClosed = "access.delegation.closed";

    /// <summary>
    /// 委托不存在；参数 <c>id</c>。
    /// </summary>
    public const string DelegationNotFound = "access.delegation.not_found";

    /// <summary>
    /// 操作 <c>permission-conditions.attributes</c> 的说明。
    /// </summary>
    public const string ListConditionAttributesDescription = "permission_conditions.attributes.description";

    /// <summary>
    /// 操作 <c>permission-conditions.list</c> 的说明。
    /// </summary>
    public const string ListPermissionConditionsDescription = "permission_conditions.list.description";

    /// <summary>
    /// 操作 <c>permission-conditions.create</c> 的说明。
    /// </summary>
    public const string CreatePermissionConditionDescription = "permission_conditions.create.description";

    /// <summary>
    /// 操作 <c>permission-conditions.update</c> 的说明。
    /// </summary>
    public const string UpdatePermissionConditionDescription = "permission_conditions.update.description";

    /// <summary>
    /// 操作 <c>permission-conditions.delete</c> 的说明。
    /// </summary>
    public const string DeletePermissionConditionsDescription = "permission_conditions.delete.description";

    /// <summary>
    /// 主体在权限码上没有这条授予或拒绝；参数 <c>permission</c>、<c>action</c>。
    /// </summary>
    public const string ConditionBindingMissing = "access.condition.binding_missing";

    /// <summary>
    /// 角色停用或是内置角色；参数 <c>name</c>。
    /// </summary>
    public const string ConditionSubjectInvalid = "access.condition.subject_invalid";

    /// <summary>
    /// 条件组超出上限；参数 <c>max</c>。
    /// </summary>
    public const string ConditionGroupsExceeded = "access.condition.groups_exceeded";

    /// <summary>
    /// 子句超出上限；参数 <c>max</c>。
    /// </summary>
    public const string ConditionClausesExceeded = "access.condition.clauses_exceeded";

    /// <summary>
    /// 权限条件不存在；参数 <c>id</c>。
    /// </summary>
    public const string ConditionNotFound = "access.condition.not_found";

    /// <summary>
    /// 当前条件下不允许此操作；参数 <c>permission</c>。
    /// </summary>
    public const string ConditionNotMet = "access.condition.not_met";

    /// <summary>
    /// 试算的客户端地址写错；参数 <c>ip</c>。
    /// </summary>
    public const string TrialIpInvalid = "access.condition.trial_ip_invalid";

    /// <summary>
    /// 操作 <c>permission-conditions.trial</c> 的说明。
    /// </summary>
    public const string TrialPermissionConditionDescription = "permission_conditions.trial.description";

    /// <summary>
    /// 操作 <c>permission-conditions.batch-trial</c> 的说明。
    /// </summary>
    public const string BatchTrialPermissionConditionDescription = "permission_conditions.batch_trial.description";

    /// <summary>
    /// 操作 <c>permission-conditions.inventory</c> 的说明。
    /// </summary>
    public const string PermissionConditionInventoryDescription = "permission_conditions.inventory.description";

    /// <summary>
    /// 访问策略编码含空白；参数 <c>code</c>。
    /// </summary>
    public const string PolicyCodeInvalid = "access.policy.code_invalid";

    /// <summary>
    /// 访问策略没有条件。
    /// </summary>
    public const string PolicyConditionsRequired = "access.policy.conditions_required";

    /// <summary>
    /// 访问策略的条件组或条件超过上限；参数 <c>maxGroups</c>、<c>maxConditions</c>。
    /// </summary>
    public const string PolicyConditionsExceeded = "access.policy.conditions_exceeded";

    /// <summary>
    /// 访问策略的失效时间不晚于生效时间。
    /// </summary>
    public const string PolicyWindowInvalid = "access.policy.window_invalid";

    /// <summary>
    /// 启用中的拒绝策略会拦下当前这次请求；参数 <c>code</c>。
    /// </summary>
    public const string PolicySelfLockout = "access.policy.self_lockout";

    /// <summary>
    /// 访问策略的模板不存在；参数 <c>code</c>。
    /// </summary>
    public const string PolicyTemplateUnknown = "access.policy.template_unknown";

    /// <summary>
    /// 访问策略编码在这一侧已存在；参数 <c>code</c>。
    /// </summary>
    public const string PolicyCodeTaken = "access.policy.code_taken";

    /// <summary>
    /// 访问策略不存在；参数 <c>id</c>。
    /// </summary>
    public const string PolicyNotFound = "access.policy.not_found";

    /// <summary>
    /// 调用被访问策略拒绝。
    /// </summary>
    public const string PolicyDenied = "access.policy.denied";

    /// <summary>
    /// 登录被登录时机的访问策略拒绝。
    /// </summary>
    public const string PolicyLoginDenied = "access.policy.login_denied";

    /// <summary>
    /// 登录失败自动封禁的参数写错；参数 <c>key</c>。
    /// </summary>
    public const string LoginAutoBanInvalid = "access.login_ban.settings_invalid";

    /// <summary>
    /// 要解封的地址没有被封禁；参数 <c>ip</c>。
    /// </summary>
    public const string LoginBanNotFound = "access.login_ban.not_found";

    /// <summary>
    /// 来源地址因登录失败次数过多被暂时封禁；参数 <c>retryAfter</c>（秒）。
    /// </summary>
    public const string LoginBanned = "access.login_ban.banned";

    /// <summary>
    /// 操作 <c>login-bans.list</c> 的说明。
    /// </summary>
    public const string ListLoginBansDescription = "login_bans.list.description";

    /// <summary>
    /// 操作 <c>login-bans.lift</c> 的说明。
    /// </summary>
    public const string LiftLoginBanDescription = "login_bans.lift.description";

    /// <summary>
    /// 约束规则编码含空白；参数 <c>code</c>。
    /// </summary>
    public const string ConstraintCodeInvalid = "access.constraint.code_invalid";

    /// <summary>
    /// 约束类型用不了这种目标类型；参数 <c>type</c>、<c>targetType</c>。
    /// </summary>
    public const string ConstraintTargetMismatch = "access.constraint.target_mismatch";

    /// <summary>
    /// 规则项太少；参数 <c>min</c>。
    /// </summary>
    public const string ConstraintItemsTooFew = "access.constraint.items_too_few";

    /// <summary>
    /// 规则项目标重复；参数 <c>target</c>。
    /// </summary>
    public const string ConstraintItemDuplicate = "access.constraint.item_duplicate";

    /// <summary>
    /// 规则项目标不在这一侧或不可用；参数 <c>target</c>。
    /// </summary>
    public const string ConstraintTargetInvalid = "access.constraint.target_invalid";

    /// <summary>
    /// 条件类约束没有条件，或条件超过上限；参数 <c>maxGroups</c>、<c>maxConditions</c>。
    /// </summary>
    public const string ConstraintConditionsRequired = "access.constraint.conditions_required";

    /// <summary>
    /// 要激活的角色此刻不持有；参数 <c>id</c>。
    /// </summary>
    public const string ActivationRoleNotHeld = "access.activation.not_held";

    /// <summary>
    /// 约束规则编码在这一侧已存在；参数 <c>code</c>。
    /// </summary>
    public const string ConstraintCodeTaken = "access.constraint.code_taken";

    /// <summary>
    /// 违反了拦截类的约束规则，这次变更没做；参数 <c>code</c>、<c>name</c>。
    /// </summary>
    public const string ConstraintViolated = "access.constraint.violated";

    /// <summary>
    /// 约束规则不存在；参数 <c>id</c>。
    /// </summary>
    public const string ConstraintRuleNotFound = "access.constraint.not_found";

    /// <summary>
    /// 会话角色记录不存在；参数 <c>id</c>。
    /// </summary>
    public const string SessionRoleNotFound = "access.session_role.not_found";

    /// <summary>
    /// 操作 <c>constraint-rules.list</c> 的说明。
    /// </summary>
    public const string ListConstraintRulesDescription = "constraint_rules.list.description";

    /// <summary>
    /// 操作 <c>constraint-rules.get</c> 的说明。
    /// </summary>
    public const string GetConstraintRuleDescription = "constraint_rules.get.description";

    /// <summary>
    /// 操作 <c>constraint-rules.create</c> 的说明。
    /// </summary>
    public const string CreateConstraintRuleDescription = "constraint_rules.create.description";

    /// <summary>
    /// 操作 <c>constraint-rules.update</c> 的说明。
    /// </summary>
    public const string UpdateConstraintRuleDescription = "constraint_rules.update.description";

    /// <summary>
    /// 操作 <c>constraint-rules.set-status</c> 的说明。
    /// </summary>
    public const string SetConstraintRuleStatusDescription = "constraint_rules.set_status.description";

    /// <summary>
    /// 操作 <c>constraint-rules.delete</c> 的说明。
    /// </summary>
    public const string DeleteConstraintRulesDescription = "constraint_rules.delete.description";

    /// <summary>
    /// 操作 <c>session-roles.list</c> 的说明。
    /// </summary>
    public const string ListSessionRolesDescription = "session_roles.list.description";

    /// <summary>
    /// 操作 <c>session-roles.get</c> 的说明。
    /// </summary>
    public const string GetSessionRoleDescription = "session_roles.get.description";

    /// <summary>
    /// 操作 <c>auth.session-roles</c> 的说明。
    /// </summary>
    public const string CurrentSessionRolesDescription = "auth.session_roles.description";

    /// <summary>
    /// 操作 <c>policies.list</c> 的说明。
    /// </summary>
    public const string ListAccessPoliciesDescription = "policies.list.description";

    /// <summary>
    /// 操作 <c>policies.get</c> 的说明。
    /// </summary>
    public const string GetAccessPolicyDescription = "policies.get.description";

    /// <summary>
    /// 操作 <c>policies.create</c> 的说明。
    /// </summary>
    public const string CreateAccessPolicyDescription = "policies.create.description";

    /// <summary>
    /// 操作 <c>policies.update</c> 的说明。
    /// </summary>
    public const string UpdateAccessPolicyDescription = "policies.update.description";

    /// <summary>
    /// 操作 <c>policies.set-action</c> 的说明。
    /// </summary>
    public const string SetAccessPolicyActionDescription = "policies.set_action.description";

    /// <summary>
    /// 操作 <c>policies.set-status</c> 的说明。
    /// </summary>
    public const string SetAccessPolicyStatusDescription = "policies.set_status.description";

    /// <summary>
    /// 操作 <c>policies.delete</c> 的说明。
    /// </summary>
    public const string DeleteAccessPoliciesDescription = "policies.delete.description";

    /// <summary>
    /// 操作 <c>policies.attributes</c> 的说明。
    /// </summary>
    public const string ListAccessPolicyAttributesDescription = "policies.attributes.description";

    /// <summary>
    /// 操作 <c>policies.templates</c> 的说明。
    /// </summary>
    public const string ListAccessPolicyTemplatesDescription = "policies.templates.description";

    /// <summary>
    /// 操作 <c>policies.trial</c> 的说明。
    /// </summary>
    public const string TrialAccessPolicyDescription = "policies.trial.description";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(AccessMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
