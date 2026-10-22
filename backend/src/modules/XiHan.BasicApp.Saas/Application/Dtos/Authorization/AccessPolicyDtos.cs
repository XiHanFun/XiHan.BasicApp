// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Authorization.Conditions;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 访问策略的一条条件
/// </summary>
public sealed class AccessPolicyConditionDto
{
    /// <summary>
    /// 条件分组（组内且、组间或）
    /// </summary>
    public int ConditionGroup { get; set; }

    /// <summary>
    /// 属性名称
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// 操作符
    /// </summary>
    public ConditionOperator Operator { get; set; } = ConditionOperator.Equals;

    /// <summary>
    /// 是否取反
    /// </summary>
    public bool IsNegated { get; set; }

    /// <summary>
    /// 条件值类型
    /// </summary>
    public ConfigDataType ValueType { get; set; } = ConfigDataType.String;

    /// <summary>
    /// 条件值
    /// </summary>
    public string ConditionValue { get; set; } = string.Empty;

    /// <summary>
    /// 条件说明
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 访问策略列表项
/// </summary>
public class AccessPolicyListItemDto : BasicAppDto
{
    /// <summary>
    /// 所属层，0 为平台
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 策略编码
    /// </summary>
    public string PolicyCode { get; set; } = string.Empty;

    /// <summary>
    /// 策略名称
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// 说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 动作
    /// </summary>
    public AccessPolicyAction Action { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 此刻是否生效（启用且在生效期内）
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 来源模板编码
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 求值时机：只引用请求本身的属性时在认证前，否则在认证后
    /// </summary>
    public AccessPolicyPhase Phase { get; set; }

    /// <summary>
    /// 条件数
    /// </summary>
    public int ConditionCount { get; set; }

    /// <summary>
    /// 近 24 小时命中次数
    /// </summary>
    public long RecentHitCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 访问策略详情
/// </summary>
public sealed class AccessPolicyDetailDto : AccessPolicyListItemDto
{
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 条件
    /// </summary>
    public List<AccessPolicyConditionDto> Conditions { get; set; } = [];
}

/// <summary>
/// 访问策略分页查询
/// </summary>
public sealed class AccessPolicyPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（编码、名称）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 动作
    /// </summary>
    public AccessPolicyAction? Action { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}

/// <summary>
/// 访问策略创建：动作一律是只记录，启用拦截另走修改动作
/// </summary>
public sealed class AccessPolicyCreateDto
{
    /// <summary>
    /// 策略编码
    /// </summary>
    public string PolicyCode { get; set; } = string.Empty;

    /// <summary>
    /// 策略名称
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// 说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 优先级，越小越先评估
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 来源模板编码
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 条件
    /// </summary>
    public List<AccessPolicyConditionDto> Conditions { get; set; } = [];
}

/// <summary>
/// 访问策略更新（不改动作与状态）
/// </summary>
public sealed class AccessPolicyUpdateDto : BasicAppDto
{
    /// <summary>
    /// 策略名称
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// 说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 条件
    /// </summary>
    public List<AccessPolicyConditionDto> Conditions { get; set; } = [];
}

/// <summary>
/// 访问策略动作修改
/// </summary>
public sealed class AccessPolicyActionUpdateDto : BasicAppDto
{
    /// <summary>
    /// 动作
    /// </summary>
    public AccessPolicyAction Action { get; set; }
}

/// <summary>
/// 访问策略状态修改
/// </summary>
public sealed class AccessPolicyStatusUpdateDto : BasicAppDto
{
    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }
}

/// <summary>
/// 访问策略条件属性（条件编辑器用）
/// </summary>
public sealed class AccessPolicyAttributeDto
{
    /// <summary>
    /// 登记名
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
    /// 是否一族属性（名称以 .* 结尾）
    /// </summary>
    public bool IsFamily { get; set; }

    /// <summary>
    /// 是否旧写法
    /// </summary>
    public bool IsLegacy { get; set; }

    /// <summary>
    /// 是否认证前即可取值
    /// </summary>
    public bool IsBeforeAuthentication { get; set; }

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
/// 访问策略试运行的模拟上下文；不填的项取当前请求
/// </summary>
public sealed class AccessPolicyTrialContextDto
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
    /// 请求方法
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 用户主键；不填取当前用户
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 端点权限码
    /// </summary>
    public List<string>? PermissionCodes { get; set; }
}

/// <summary>
/// 访问策略试运行：给已保存的策略主键，或给一份条件草稿
/// </summary>
public sealed class AccessPolicyTrialInputDto
{
    /// <summary>
    /// 已保存的策略主键
    /// </summary>
    public long? PolicyId { get; set; }

    /// <summary>
    /// 动作；草稿缺省为只记录，已保存的策略缺省取它自己的
    /// </summary>
    public AccessPolicyAction? Action { get; set; }

    /// <summary>
    /// 条件草稿（不给 <see cref="PolicyId"/> 时用）
    /// </summary>
    public List<AccessPolicyConditionDto>? Conditions { get; set; }

    /// <summary>
    /// 模拟上下文
    /// </summary>
    public AccessPolicyTrialContextDto Context { get; set; } = new();
}

/// <summary>
/// 访问策略试运行结果
/// </summary>
public sealed class AccessPolicyTrialResultDto
{
    /// <summary>
    /// 条件集结果；条件有错时为空
    /// </summary>
    public ConditionOutcome? Outcome { get; set; }

    /// <summary>
    /// 动作
    /// </summary>
    public AccessPolicyAction Action { get; set; }

    /// <summary>
    /// 是否会拦下这个请求（命中且动作为拒绝）
    /// </summary>
    public bool WouldBlock { get; set; }

    /// <summary>
    /// 求值时机
    /// </summary>
    public AccessPolicyPhase Phase { get; set; }

    /// <summary>
    /// 条件错误
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// 用到的客户端 IP
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// IP 归属地
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 用到的时刻
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// 时间类条件按的时区
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// 用到的 User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 用到的用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 各组、各子句的取值与结果
    /// </summary>
    public List<PermissionConditionTrialGroupDto> Groups { get; set; } = [];
}

/// <summary>
/// 命中记录分页查询
/// </summary>
public sealed class AccessPolicyHitPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 命中时间起
    /// </summary>
    public DateTimeOffset? HitTimeStart { get; set; }

    /// <summary>
    /// 命中时间止
    /// </summary>
    public DateTimeOffset? HitTimeEnd { get; set; }

    /// <summary>
    /// 来源
    /// </summary>
    public AccessPolicyHitSource? Source { get; set; }

    /// <summary>
    /// 访问策略主键
    /// </summary>
    public long? PolicyId { get; set; }

    /// <summary>
    /// 策略编码或权限码（模糊）
    /// </summary>
    public string? PolicyCode { get; set; }

    /// <summary>
    /// 动作
    /// </summary>
    public AccessPolicyAction? Action { get; set; }

    /// <summary>
    /// 求值时机
    /// </summary>
    public AccessPolicyPhase? Phase { get; set; }

    /// <summary>
    /// 是否拦下
    /// </summary>
    public bool? IsBlocked { get; set; }

    /// <summary>
    /// 客户端 IP（模糊）
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// 用户名（模糊）
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 请求路径（模糊）
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 链路追踪 ID
    /// </summary>
    public string? TraceId { get; set; }
}

/// <summary>
/// 命中记录列表项
/// </summary>
public sealed class AccessPolicyHitListItemDto : BasicAppDto
{
    /// <summary>
    /// 来源
    /// </summary>
    public AccessPolicyHitSource Source { get; set; }

    /// <summary>
    /// 访问策略主键
    /// </summary>
    public long? PolicyId { get; set; }

    /// <summary>
    /// 策略编码或权限码
    /// </summary>
    public string PolicyCode { get; set; } = string.Empty;

    /// <summary>
    /// 策略名称
    /// </summary>
    public string? PolicyName { get; set; }

    /// <summary>
    /// 动作
    /// </summary>
    public AccessPolicyAction Action { get; set; }

    /// <summary>
    /// 求值时机
    /// </summary>
    public AccessPolicyPhase Phase { get; set; }

    /// <summary>
    /// 是否拦下
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// 链路追踪 ID
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 客户端 IP
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// IP 归属地
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 端点权限码
    /// </summary>
    public string? PermissionCodes { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 客户端类型
    /// </summary>
    public string? ClientKind { get; set; }

    /// <summary>
    /// 这一分钟里第一次命中的时间
    /// </summary>
    public DateTimeOffset HitTime { get; set; }

    /// <summary>
    /// 这一分钟里的命中次数
    /// </summary>
    public int HitCount { get; set; }
}

/// <summary>
/// 一条自动封禁
/// </summary>
public sealed class LoginBanDto
{
    /// <summary>
    /// 客户端 IP
    /// </summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// 解封时间
    /// </summary>
    public DateTimeOffset Until { get; set; }

    /// <summary>
    /// 剩余秒数
    /// </summary>
    public long RemainingSeconds { get; set; }
}

/// <summary>
/// 手动解封
/// </summary>
public sealed class LoginUnbanDto
{
    /// <summary>
    /// 客户端 IP
    /// </summary>
    public string Ip { get; set; } = string.Empty;
}
