// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Authorization.Conditions;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 条件属性（条件编辑器用）
/// </summary>
public sealed class PermissionConditionAttributeDto
{
    /// <summary>
    /// 属性名；一族属性以 <c>.*</c> 结尾
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 值种类
    /// </summary>
    public ConditionValueKind Kind { get; set; }

    /// <summary>
    /// 是否多值
    /// </summary>
    public bool IsMultiValued { get; set; }

    /// <summary>
    /// 是否一族属性
    /// </summary>
    public bool IsFamily { get; set; }

    /// <summary>
    /// 是否旧写法（保存时提示改用新名）
    /// </summary>
    public bool IsLegacy { get; set; }

    /// <summary>
    /// 是否数据行属性（所绑权限码登记的实体列）
    /// </summary>
    public bool IsRowAttribute { get; set; }

    /// <summary>
    /// 旧名别名
    /// </summary>
    public List<string> Aliases { get; set; } = [];

    /// <summary>
    /// 能用的运算符
    /// </summary>
    public List<ConditionOperator> Operators { get; set; } = [];
}

/// <summary>
/// 条件盘点的诊断
/// </summary>
public enum PermissionConditionDiagnostic
{
    /// <summary>
    /// 属性未登记（含数据行属性没登记）
    /// </summary>
    UnregisteredAttribute = 0,

    /// <summary>
    /// 用的是旧名
    /// </summary>
    LegacyName = 1,

    /// <summary>
    /// 运算符不适用或条件值解析失败
    /// </summary>
    InvalidCondition = 2,

    /// <summary>
    /// 值类型与属性的值种类不符
    /// </summary>
    ValueTypeMismatch = 3,

    /// <summary>
    /// 所在授权已失效（停用、过期、未生效、已删除，角色或权限停用）
    /// </summary>
    BindingInactive = 4,

    /// <summary>
    /// 同时（或都没）挂在角色权限与用户直授上
    /// </summary>
    AmbiguousBinding = 5
}

/// <summary>
/// 条件组在生效后的结论
/// </summary>
public enum PermissionConditionGroupConclusion
{
    /// <summary>
    /// 可正常求值
    /// </summary>
    Evaluable = 0,

    /// <summary>
    /// 有子句恒不命中，这组恒不满足
    /// </summary>
    NeverMatches = 1,

    /// <summary>
    /// 授权已失效，不影响任何人
    /// </summary>
    BindingInactive = 2,

    /// <summary>
    /// 组内子句全部停用，不参与
    /// </summary>
    Disabled = 3
}

/// <summary>
/// 条件所在授权的类别
/// </summary>
public enum PermissionConditionBindingKind
{
    /// <summary>
    /// 角色权限
    /// </summary>
    Role = 0,

    /// <summary>
    /// 用户直授
    /// </summary>
    User = 1,

    /// <summary>
    /// 二选一不成立
    /// </summary>
    Ambiguous = 2
}

/// <summary>
/// 条件盘点
/// </summary>
public sealed class PermissionConditionInventoryDto
{
    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTimeOffset GeneratedTime { get; set; }

    /// <summary>
    /// 条件条数
    /// </summary>
    public int ConditionCount { get; set; }

    /// <summary>
    /// 涉及的授权条数
    /// </summary>
    public int BindingCount { get; set; }

    /// <summary>
    /// 各结论的条件组数
    /// </summary>
    public Dictionary<PermissionConditionGroupConclusion, int> GroupConclusions { get; set; } = [];

    /// <summary>
    /// 各诊断的条件条数
    /// </summary>
    public Dictionary<PermissionConditionDiagnostic, int> Diagnostics { get; set; } = [];

    /// <summary>
    /// 明细（每条条件一行）
    /// </summary>
    public List<PermissionConditionInventoryItemDto> Items { get; set; } = [];
}

/// <summary>
/// 条件盘点明细
/// </summary>
public sealed class PermissionConditionInventoryItemDto
{
    /// <summary>
    /// 条件主键
    /// </summary>
    public long ConditionId { get; set; }

    /// <summary>
    /// 租户主键，0 为平台
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 租户编码
    /// </summary>
    public string? TenantCode { get; set; }

    /// <summary>
    /// 授权类别
    /// </summary>
    public PermissionConditionBindingKind BindingKind { get; set; }

    /// <summary>
    /// 角色权限主键
    /// </summary>
    public long? RolePermissionId { get; set; }

    /// <summary>
    /// 用户直授主键
    /// </summary>
    public long? UserPermissionId { get; set; }

    /// <summary>
    /// 角色编码
    /// </summary>
    public string? RoleCode { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 权限码
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 权限名称
    /// </summary>
    public string? PermissionName { get; set; }

    /// <summary>
    /// 授权动作：授予上是「满足才授予」，禁止上是「满足才禁止」
    /// </summary>
    public PermissionAction? PermissionAction { get; set; }

    /// <summary>
    /// 条件分组
    /// </summary>
    public int ConditionGroup { get; set; }

    /// <summary>
    /// 属性名
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// 运算符
    /// </summary>
    public ConditionOperator Operator { get; set; }

    /// <summary>
    /// 是否取反
    /// </summary>
    public bool IsNegated { get; set; }

    /// <summary>
    /// 值类型
    /// </summary>
    public ConfigDataType ValueType { get; set; }

    /// <summary>
    /// 条件值
    /// </summary>
    public string ConditionValue { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public ValidityStatus Status { get; set; }

    /// <summary>
    /// 诊断
    /// </summary>
    public List<PermissionConditionDiagnostic> Diagnostics { get; set; } = [];

    /// <summary>
    /// 条件无法求值时的原因
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 所在组的结论
    /// </summary>
    public PermissionConditionGroupConclusion GroupConclusion { get; set; }
}

/// <summary>
/// 试算的模拟请求上下文；不填的项取当前请求（IP、User-Agent）或当前时刻
/// </summary>
public class PermissionConditionTrialContextDto
{
    /// <summary>
    /// 客户端 IP
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// 时刻
    /// </summary>
    public DateTimeOffset? Time { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// HTTP 方法
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 路由值
    /// </summary>
    public Dictionary<string, string>? RouteValues { get; set; }

    /// <summary>
    /// 查询串
    /// </summary>
    public Dictionary<string, string>? QueryValues { get; set; }

    /// <summary>
    /// 是否模拟模仿登录
    /// </summary>
    public bool IsImpersonating { get; set; }
}

/// <summary>
/// 单人试算输入
/// </summary>
public sealed class PermissionConditionTrialInputDto : PermissionConditionTrialContextDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 权限码
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户主键（只在平台上下文可选，0 为平台；租户上下文固定为当前租户）
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 数据行主键（权限码登记了条件资源时用于取数据行属性）
    /// </summary>
    public long? ResourceId { get; set; }
}

/// <summary>
/// 批量试算输入：按一条授权找出受影响的人，各算一遍
/// </summary>
public sealed class PermissionConditionBatchTrialInputDto : PermissionConditionTrialContextDto
{
    /// <summary>
    /// 角色权限主键（与用户直授二选一）
    /// </summary>
    public long? RolePermissionId { get; set; }

    /// <summary>
    /// 用户直授主键
    /// </summary>
    public long? UserPermissionId { get; set; }
}

/// <summary>
/// 试算结论
/// </summary>
public enum PermissionConditionTrialVerdict
{
    /// <summary>
    /// 快照里没有授予
    /// </summary>
    NotGranted = 0,

    /// <summary>
    /// 无条件授予（含超管、租户所有者）
    /// </summary>
    Unconditional = 1,

    /// <summary>
    /// 条件满足，授予
    /// </summary>
    Granted = 2,

    /// <summary>
    /// 条件不满足，或有条件的禁止命中
    /// </summary>
    Denied = 3,

    /// <summary>
    /// 还差数据行属性
    /// </summary>
    Pending = 4
}

/// <summary>
/// 单人试算结果
/// </summary>
public sealed class PermissionConditionTrialResultDto
{
    /// <summary>
    /// 结论
    /// </summary>
    public PermissionConditionTrialVerdict Verdict { get; set; }

    /// <summary>
    /// 租户主键
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 权限码
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 试算用的 IP
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// 试算用的时刻
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// 时间类属性所用的时区
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// 授予路径
    /// </summary>
    public List<PermissionConditionTrialPathDto> Paths { get; set; } = [];

    /// <summary>
    /// 用户直授上有条件的禁止
    /// </summary>
    public List<PermissionConditionTrialSetDto> Denies { get; set; } = [];
}

/// <summary>
/// 一条授予路径的试算
/// </summary>
public sealed class PermissionConditionTrialPathDto
{
    /// <summary>
    /// 来源：<c>role:{角色主键}</c>、<c>user:{用户直授主键}</c>、<c>delegation-role:{角色主键}</c>
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 来源名称（角色名、直授）
    /// </summary>
    public string? SourceLabel { get; set; }

    /// <summary>
    /// 链上有无条件的授予行
    /// </summary>
    public bool HasUnconditionalGrant { get; set; }

    /// <summary>
    /// 授予条件集
    /// </summary>
    public List<PermissionConditionTrialSetDto> GrantSets { get; set; } = [];

    /// <summary>
    /// 链上有条件的禁止
    /// </summary>
    public List<PermissionConditionTrialSetDto> DenySets { get; set; } = [];

    /// <summary>
    /// 这条路径的结果：命中为成立，不命中为不成立，待定为还差数据行
    /// </summary>
    public ConditionOutcome Outcome { get; set; }
}

/// <summary>
/// 一个条件集的试算
/// </summary>
public sealed class PermissionConditionTrialSetDto
{
    /// <summary>
    /// 条件集键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 结果；快照里找不到这个条件集时为空
    /// </summary>
    public ConditionOutcome? Outcome { get; set; }

    /// <summary>
    /// 各组
    /// </summary>
    public List<PermissionConditionTrialGroupDto> Groups { get; set; } = [];
}

/// <summary>
/// 一个条件组的试算
/// </summary>
public sealed class PermissionConditionTrialGroupDto
{
    /// <summary>
    /// 条件分组
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// 结果
    /// </summary>
    public ConditionOutcome Outcome { get; set; }

    /// <summary>
    /// 子句
    /// </summary>
    public List<PermissionConditionTrialClauseDto> Clauses { get; set; } = [];
}

/// <summary>
/// 一条子句的试算
/// </summary>
public sealed class PermissionConditionTrialClauseDto
{
    /// <summary>
    /// 条件主键
    /// </summary>
    public string? ConditionId { get; set; }

    /// <summary>
    /// 属性名
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// 运算符
    /// </summary>
    public ConditionOperator Operator { get; set; }

    /// <summary>
    /// 是否取反
    /// </summary>
    public bool IsNegated { get; set; }

    /// <summary>
    /// 条件值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 结果
    /// </summary>
    public ConditionOutcome Outcome { get; set; }

    /// <summary>
    /// 取到的属性值
    /// </summary>
    public List<string> AttributeValues { get; set; } = [];

    /// <summary>
    /// 无法判定或待定的原因
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// 批量试算结果
/// </summary>
public sealed class PermissionConditionBatchTrialResultDto
{
    /// <summary>
    /// 权限码
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 受影响的人数
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// 是否只算了前一部分人
    /// </summary>
    public bool Truncated { get; set; }

    /// <summary>
    /// 每人的结论
    /// </summary>
    public List<PermissionConditionBatchTrialItemDto> Items { get; set; } = [];
}

/// <summary>
/// 批量试算中一个人的结论
/// </summary>
public sealed class PermissionConditionBatchTrialItemDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 结论
    /// </summary>
    public PermissionConditionTrialVerdict Verdict { get; set; }
}
