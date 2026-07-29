// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 创建业务编号规则命令。
/// </summary>
/// <param name="RuleCode">作用域内唯一的规则编码；领域服务会去除首尾空白并统一转为大写，创建后不可修改。</param>
/// <param name="RuleName">面向管理端展示的规则名称，不能为空且最长 100 个字符。</param>
/// <param name="Prefix">可选编号前缀；空白值会归一为 <see langword="null"/>。</param>
/// <param name="Separator">所有非空格式段之间的分隔符，最长 10 个字符。</param>
/// <param name="DateFormat">编号日期段格式，必须与重置周期形成不会跨周期重复的安全组合。</param>
/// <param name="SerialLength">流水段固定位数，范围为 1 至 18。</param>
/// <param name="ResetCycle">按规则时区计算的流水重置周期。</param>
/// <param name="TimeZoneId">规则时区；接受可移植 IANA 标识以及当前运行平台能够映射的历史 Windows 标识。</param>
/// <param name="AllowTenantUse">全局规则是否允许租户共享调用；租户私有规则会被领域层强制设为 <see langword="false"/>。</param>
/// <param name="Status">规则创建后的启停状态。</param>
/// <param name="Sort">管理端显示排序值，不能为负数。</param>
/// <param name="Remark">可选维护备注，最长 500 个字符。</param>
public sealed record NumberingRuleCreateCommand(
    string RuleCode,
    string RuleName,
    string? Prefix,
    string Separator,
    NumberingDateFormat DateFormat,
    int SerialLength,
    NumberingResetCycle ResetCycle,
    string TimeZoneId,
    bool AllowTenantUse,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 更新业务编号规则命令；规则编码保持不可变。
/// </summary>
/// <param name="BasicId">待更新规则主键，必须为正数并属于当前领域作用域。</param>
/// <param name="RuleName">新的规则名称。</param>
/// <param name="Prefix">新的可选前缀；规则首次发号后该格式字段被冻结。</param>
/// <param name="Separator">新的格式分隔符；规则首次发号后不可修改。</param>
/// <param name="DateFormat">新的日期段格式；规则首次发号后不可修改。</param>
/// <param name="SerialLength">新的流水段位数，范围为 1 至 18；规则首次发号后不可修改。</param>
/// <param name="ResetCycle">新的流水重置周期；规则首次发号后不可修改。</param>
/// <param name="TimeZoneId">新的规则时区；规则首次发号后不可修改。</param>
/// <param name="AllowTenantUse">全局规则的租户开放开关；租户私有规则始终为 <see langword="false"/>。</param>
/// <param name="Sort">新的管理端显示排序值，不能为负数。</param>
/// <param name="Remark">新的可选维护备注。</param>
public sealed record NumberingRuleUpdateCommand(
    long BasicId,
    string RuleName,
    string? Prefix,
    string Separator,
    NumberingDateFormat DateFormat,
    int SerialLength,
    NumberingResetCycle ResetCycle,
    string TimeZoneId,
    bool AllowTenantUse,
    int Sort,
    string? Remark);

/// <summary>
/// 更新业务编号规则启停状态命令。
/// </summary>
/// <param name="BasicId">待更新规则主键，必须为正数并属于当前领域作用域。</param>
/// <param name="Status">目标启停状态，必须是有效的 <see cref="EnableStatus"/> 枚举值。</param>
/// <param name="Remark">可选状态变更备注；非空时同时更新规则备注。</param>
public sealed record NumberingRuleStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 安全重置业务编号规则命令。
/// </summary>
/// <param name="BasicId">待重置规则主键，必须为正数并属于当前领域作用域。</param>
/// <param name="NextValue">重置后下一次发号应使用的流水值，必须为正数且不得进入历史已分配区间。</param>
/// <param name="Reason">必填重置原因，用于敏感操作结构化审计。</param>
/// <param name="ConfirmRuleCode">全局规则必填的规则编码二次确认；租户私有规则可为空。</param>
public sealed record NumberingRuleResetCommand(long BasicId, long NextValue, string Reason, string? ConfirmRuleCode);

/// <summary>
/// 业务编号规则命令结果。
/// </summary>
/// <param name="Rule">完成领域校验和持久化后的规则实体。</param>
public sealed record NumberingRuleCommandResult(SysNumberingRule Rule);
