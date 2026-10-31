// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包的错误码：<c>ACCESS_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </summary>
public static class AccessErrors
{
    /// <summary>
    /// 角色不存在（或不在当前主体的作用侧）。
    /// </summary>
    public const string RoleNotFound = "ACCESS_NOTFOUND_001";

    /// <summary>
    /// 用户不存在或不在角色所属的租户。
    /// </summary>
    public const string MemberNotFound = "ACCESS_NOTFOUND_002";

    /// <summary>
    /// 角色码在该作用侧已被占用。
    /// </summary>
    public const string RoleCodeTaken = "ACCESS_CONFLICT_001";

    /// <summary>
    /// 内建角色不能删除。
    /// </summary>
    public const string BuiltInRole = "ACCESS_CONFLICT_002";

    /// <summary>
    /// 权限码不在权限目录里。
    /// </summary>
    public const string PermissionUnknown = "ACCESS_VALIDATION_001";

    /// <summary>
    /// 权限的作用侧与角色所在侧不符。
    /// </summary>
    /// <remarks>
    /// 平台角色只能授平台 / 两侧权限，租户角色只能授租户 / 两侧权限。
    /// </remarks>
    public const string PermissionScopeMismatch = "ACCESS_VALIDATION_002";

    /// <summary>
    /// 列表的筛选或排序字段不受支持。
    /// </summary>
    public const string QueryUnsupported = "ACCESS_VALIDATION_003";

    /// <summary>
    /// 平台角色的数据范围只能是全部、不能带自定义部门。
    /// </summary>
    public const string DataScopeNotApplicable = "ACCESS_VALIDATION_004";

    /// <summary>
    /// 自定义数据范围至少要有一个部门。
    /// </summary>
    public const string CustomScopeEmpty = "ACCESS_VALIDATION_005";

    /// <summary>
    /// 自定义数据范围里的部门不在当前租户。
    /// </summary>
    public const string DepartmentNotFound = "ACCESS_NOTFOUND_003";

    /// <summary>
    /// 租户主体要看平台侧的权限目录。
    /// </summary>
    public const string PlatformSideDenied = "ACCESS_DENIED_001";

    /// <summary>
    /// 菜单只能在平台侧管理。
    /// </summary>
    public const string MenuPlatformOnly = "ACCESS_DENIED_002";

    /// <summary>
    /// 菜单节点不存在。
    /// </summary>
    public const string MenuNotFound = "ACCESS_NOTFOUND_004";

    /// <summary>
    /// 菜单编码已被占用。
    /// </summary>
    public const string MenuCodeTaken = "ACCESS_CONFLICT_003";

    /// <summary>
    /// 页面菜单由页面登记生成，不能删除。
    /// </summary>
    public const string MenuBuiltIn = "ACCESS_CONFLICT_004";

    /// <summary>
    /// 目录下还有节点，不能删除。
    /// </summary>
    public const string MenuHasChildren = "ACCESS_CONFLICT_005";

    /// <summary>
    /// 上级节点不合规。
    /// </summary>
    public const string MenuParentInvalid = "ACCESS_VALIDATION_006";

    /// <summary>
    /// 外链地址不合规（外链须是 http / https 绝对地址，目录不能有）。
    /// </summary>
    public const string MenuUrlInvalid = "ACCESS_VALIDATION_007";

    /// <summary>
    /// 页面菜单的来源字段（新建、路由、组件、权限、外链）归页面登记。
    /// </summary>
    public const string MenuPageManaged = "ACCESS_VALIDATION_008";

    /// <summary>
    /// 目录与外链必须有标题。
    /// </summary>
    public const string MenuTitleRequired = "ACCESS_VALIDATION_009";

    /// <summary>
    /// 只有页面菜单能恢复默认。
    /// </summary>
    public const string MenuNotPage = "ACCESS_VALIDATION_010";

    /// <summary>
    /// 有效期不合法：到期须晚于生效。
    /// </summary>
    public const string WindowInvalid = "ACCESS_VALIDATION_011";

    /// <summary>
    /// 用户级数据范围只在租户侧设置。
    /// </summary>
    public const string UserDataScopeTenantOnly = "ACCESS_VALIDATION_012";

    /// <summary>
    /// 用户级自定义数据范围要列出部门，其他范围不能带部门。
    /// </summary>
    public const string UserDataScopeDepartments = "ACCESS_VALIDATION_013";

    /// <summary>
    /// 字段安全规则不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string FieldRuleNotFound = "ACCESS_NOTFOUND_005";

    /// <summary>
    /// 这一侧这个目标在这个字段上已有规则。
    /// </summary>
    /// <remarks>
    /// 参数 <c>entity</c>、<c>field</c>。
    /// </remarks>
    public const string FieldRuleTaken = "ACCESS_CONFLICT_006";

    /// <summary>
    /// 实体没有登记字段安全。
    /// </summary>
    /// <remarks>
    /// 参数 <c>entity</c>。
    /// </remarks>
    public const string FieldEntityUnknown = "ACCESS_VALIDATION_014";

    /// <summary>
    /// 实体没有这个可配置字段。
    /// </summary>
    /// <remarks>
    /// 参数 <c>entity</c>、<c>field</c>。
    /// </remarks>
    public const string FieldUnknown = "ACCESS_VALIDATION_015";

    /// <summary>
    /// 读取方式不适用于这个字段。
    /// </summary>
    /// <remarks>
    /// 非文本只能明文或隐藏，不可空的不能隐藏。
    /// 参数 <c>field</c>、<c>strategy</c>。
    /// </remarks>
    public const string FieldStrategyNotApplicable = "ACCESS_VALIDATION_016";

    /// <summary>
    /// 明文且可改写的规则什么都没限制。
    /// </summary>
    public const string FieldRuleNoEffect = "ACCESS_VALIDATION_017";

    /// <summary>
    /// 部分脱敏的保留位数不合规。
    /// </summary>
    /// <remarks>
    /// 参数 <c>max</c>。
    /// </remarks>
    public const string FieldKeepInvalid = "ACCESS_VALIDATION_018";

    /// <summary>
    /// 固定文本缺失或过长。
    /// </summary>
    /// <remarks>
    /// 参数 <c>max</c>。
    /// </remarks>
    public const string FieldReplacementInvalid = "ACCESS_VALIDATION_019";

    /// <summary>
    /// 部门目标只在租户侧。
    /// </summary>
    public const string FieldTargetTenantOnly = "ACCESS_VALIDATION_020";

    /// <summary>
    /// 目标（角色或部门）已停用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>kind</c>、<c>id</c>。
    /// </remarks>
    public const string FieldTargetDisabled = "ACCESS_VALIDATION_021";

    /// <summary>
    /// 角色不能继承自己。
    /// </summary>
    public const string RoleSelfParent = "ACCESS_VALIDATION_022";

    /// <summary>
    /// 内置角色不参与继承。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string InheritanceBuiltIn = "ACCESS_VALIDATION_023";

    /// <summary>
    /// 停用的角色不能设为上级。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string ParentDisabled = "ACCESS_VALIDATION_024";

    /// <summary>
    /// 设为上级会形成环路。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string InheritanceCycle = "ACCESS_VALIDATION_025";

    /// <summary>
    /// 已经间接继承，不必再直接继承。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string InheritanceRedundant = "ACCESS_VALIDATION_026";

    /// <summary>
    /// 委托人与被委托人是同一人。
    /// </summary>
    public const string DelegationSelf = "ACCESS_VALIDATION_027";

    /// <summary>
    /// 委托须且只能指定一个权限码或一个角色。
    /// </summary>
    public const string DelegationTargetInvalid = "ACCESS_VALIDATION_028";

    /// <summary>
    /// 委托的到期时间须晚于当前时间与生效时间。
    /// </summary>
    public const string DelegationWindowInvalid = "ACCESS_VALIDATION_029";

    /// <summary>
    /// 委托人此刻没有所委托的权限或角色。
    /// </summary>
    /// <remarks>
    /// 参数 <c>target</c>。
    /// </remarks>
    public const string DelegatorNotHolding = "ACCESS_VALIDATION_030";

    /// <summary>
    /// 委托的角色停用或是内置角色。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string DelegationRoleInvalid = "ACCESS_VALIDATION_031";

    /// <summary>
    /// 已有一条相同且未到期的委托。
    /// </summary>
    public const string DelegationTaken = "ACCESS_CONFLICT_007";

    /// <summary>
    /// 角色仍被其他角色继承，不能删除。
    /// </summary>
    public const string RoleHasChildren = "ACCESS_CONFLICT_008";

    /// <summary>
    /// 委托已撤销或已到期。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string DelegationClosed = "ACCESS_CONFLICT_009";

    /// <summary>
    /// 委托不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string DelegationNotFound = "ACCESS_NOTFOUND_006";

    /// <summary>
    /// 主体在权限码上没有这条授予或拒绝。
    /// </summary>
    /// <remarks>
    /// 参数 <c>permission</c>、<c>action</c>。
    /// </remarks>
    public const string ConditionBindingMissing = "ACCESS_VALIDATION_032";

    /// <summary>
    /// 角色停用或是内置角色，不能挂条件。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string ConditionSubjectInvalid = "ACCESS_VALIDATION_033";

    /// <summary>
    /// 一个绑定的条件组超出上限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>max</c>。
    /// </remarks>
    public const string ConditionGroupsExceeded = "ACCESS_VALIDATION_034";

    /// <summary>
    /// 一个条件组的子句超出上限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>max</c>。
    /// </remarks>
    public const string ConditionClausesExceeded = "ACCESS_VALIDATION_035";

    /// <summary>
    /// 权限条件不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string ConditionNotFound = "ACCESS_NOTFOUND_007";

    /// <summary>
    /// 当前条件下不允许此操作。
    /// </summary>
    /// <remarks>
    /// 参数 <c>permission</c>。
    /// </remarks>
    public const string ConditionNotMet = "ACCESS_DENIED_003";

    /// <summary>
    /// 客户端地址写错（试算、解封）。
    /// </summary>
    /// <remarks>
    /// 参数 <c>ip</c>。
    /// </remarks>
    public const string TrialIpInvalid = "ACCESS_VALIDATION_036";

    /// <summary>
    /// 访问策略编码含空白。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string PolicyCodeInvalid = "ACCESS_VALIDATION_037";

    /// <summary>
    /// 访问策略没有条件。
    /// </summary>
    public const string PolicyConditionsRequired = "ACCESS_VALIDATION_038";

    /// <summary>
    /// 访问策略的条件组或条件超过上限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>maxGroups</c>、<c>maxConditions</c>。
    /// </remarks>
    public const string PolicyConditionsExceeded = "ACCESS_VALIDATION_039";

    /// <summary>
    /// 访问策略的失效时间不晚于生效时间。
    /// </summary>
    public const string PolicyWindowInvalid = "ACCESS_VALIDATION_040";

    /// <summary>
    /// 启用中的拒绝策略会拦下当前这次请求（防自锁）。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string PolicySelfLockout = "ACCESS_VALIDATION_041";

    /// <summary>
    /// 访问策略的模板不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string PolicyTemplateUnknown = "ACCESS_VALIDATION_042";

    /// <summary>
    /// 访问策略编码在这一侧已存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string PolicyCodeTaken = "ACCESS_CONFLICT_010";

    /// <summary>
    /// 访问策略不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string PolicyNotFound = "ACCESS_NOTFOUND_008";

    /// <summary>
    /// 调用被访问策略拒绝。
    /// </summary>
    public const string PolicyDenied = "ACCESS_DENIED_004";

    /// <summary>
    /// 登录被登录时机的访问策略拒绝。
    /// </summary>
    public const string PolicyLoginDenied = "ACCESS_DENIED_005";

    /// <summary>
    /// 登录失败自动封禁的参数写错。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>。
    /// </remarks>
    public const string LoginAutoBanInvalid = "ACCESS_VALIDATION_043";

    /// <summary>
    /// 要解封的地址没有被封禁。
    /// </summary>
    /// <remarks>
    /// 参数 <c>ip</c>。
    /// </remarks>
    public const string LoginBanNotFound = "ACCESS_NOTFOUND_009";

    /// <summary>
    /// 来源地址因登录失败次数过多被暂时封禁。
    /// </summary>
    /// <remarks>
    /// 参数 <c>retryAfter</c>（秒）。
    /// </remarks>
    public const string LoginBanned = "ACCESS_UNAVAILABLE_001";

    /// <summary>
    /// 约束规则编码含空白。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string ConstraintCodeInvalid = "ACCESS_VALIDATION_044";

    /// <summary>
    /// 约束类型用不了这种目标类型。
    /// </summary>
    /// <remarks>
    /// 参数 <c>type</c>、<c>targetType</c>。
    /// </remarks>
    public const string ConstraintTargetMismatch = "ACCESS_VALIDATION_045";

    /// <summary>
    /// 规则项太少。
    /// </summary>
    /// <remarks>
    /// 参数 <c>min</c>。
    /// </remarks>
    public const string ConstraintItemsTooFew = "ACCESS_VALIDATION_046";

    /// <summary>
    /// 规则项目标重复。
    /// </summary>
    /// <remarks>
    /// 参数 <c>target</c>。
    /// </remarks>
    public const string ConstraintItemDuplicate = "ACCESS_VALIDATION_047";

    /// <summary>
    /// 规则项目标不在这一侧或不可用。
    /// </summary>
    /// <remarks>
    /// 参数 <c>target</c>。
    /// </remarks>
    public const string ConstraintTargetInvalid = "ACCESS_VALIDATION_048";

    /// <summary>
    /// 条件类约束没有条件，或条件超过上限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>maxGroups</c>、<c>maxConditions</c>。
    /// </remarks>
    public const string ConstraintConditionsRequired = "ACCESS_VALIDATION_049";

    /// <summary>
    /// 要激活的角色此刻不持有。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string ActivationRoleNotHeld = "ACCESS_VALIDATION_050";

    /// <summary>
    /// 约束规则编码在这一侧已存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string ConstraintCodeTaken = "ACCESS_CONFLICT_011";

    /// <summary>
    /// 违反了拦截类的约束规则，这次变更没做。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>、<c>name</c>。
    /// </remarks>
    public const string ConstraintViolated = "ACCESS_CONFLICT_012";

    /// <summary>
    /// 约束规则不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string ConstraintRuleNotFound = "ACCESS_NOTFOUND_010";

    /// <summary>
    /// 会话角色记录不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string SessionRoleNotFound = "ACCESS_NOTFOUND_011";

    /// <summary>
    /// 字段受字段安全保护，不能筛选、排序或关键字搜索。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string FieldProtected = "ACCESS_DENIED_006";

    /// <summary>
    /// 字段对调用方只读，不能填写或修改。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string FieldReadOnly = "ACCESS_DENIED_007";
}
