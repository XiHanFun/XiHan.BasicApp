// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 约束规则应用层映射器
/// </summary>
public static class ConstraintRuleApplicationMapper
{
    /// <summary>
    /// 映射约束规则创建命令
    /// </summary>
    public static ConstraintRuleCreateCommand ToCreateCommand(ConstraintRuleCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ConstraintRuleCreateCommand(
            input.RuleCode,
            input.RuleName,
            input.ConstraintType,
            input.TargetType,
            input.Parameters,
            input.Status,
            input.ViolationAction,
            input.Description,
            input.Priority,
            input.EffectiveTime,
            input.ExpirationTime,
            input.Remark,
            ToItemCommands(input.Items));
    }

    /// <summary>
    /// 映射约束规则更新命令
    /// </summary>
    public static ConstraintRuleUpdateCommand ToUpdateCommand(ConstraintRuleUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ConstraintRuleUpdateCommand(
            input.BasicId,
            input.RuleName,
            input.ConstraintType,
            input.TargetType,
            input.Parameters,
            input.ViolationAction,
            input.Description,
            input.Priority,
            input.EffectiveTime,
            input.ExpirationTime,
            input.Remark,
            ToItemCommands(input.Items));
    }

    /// <summary>
    /// 映射约束规则状态命令
    /// </summary>
    public static ConstraintRuleStatusCommand ToStatusCommand(ConstraintRuleStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ConstraintRuleStatusCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射约束规则列表项
    /// </summary>
    public static ConstraintRuleListItemDto ToListItemDto(SysConstraintRule rule, int itemCount, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new ConstraintRuleListItemDto
        {
            BasicId = rule.BasicId,
            RuleCode = rule.RuleCode,
            RuleName = rule.RuleName,
            ConstraintType = rule.ConstraintType,
            TargetType = rule.TargetType,
            IsGlobal = rule.IsGlobal,
            Status = rule.Status,
            IsActive = IsActive(rule, now),
            ViolationAction = rule.ViolationAction,
            Priority = rule.Priority,
            EffectiveTime = rule.EffectiveTime,
            ExpirationTime = rule.ExpirationTime,
            ItemCount = itemCount,
            Description = rule.Description,
            CreatedTime = rule.CreatedTime,
            ModifiedTime = rule.ModifiedTime
        };
    }

    /// <summary>
    /// 映射约束规则详情
    /// </summary>
    public static ConstraintRuleDetailDto ToDetailDto(SysConstraintRule rule, IReadOnlyList<ConstraintRuleItemDto> items, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new ConstraintRuleDetailDto
        {
            BasicId = rule.BasicId,
            RuleCode = rule.RuleCode,
            RuleName = rule.RuleName,
            ConstraintType = rule.ConstraintType,
            TargetType = rule.TargetType,
            Parameters = rule.Parameters,
            IsGlobal = rule.IsGlobal,
            Status = rule.Status,
            IsActive = IsActive(rule, now),
            ViolationAction = rule.ViolationAction,
            Description = rule.Description,
            Priority = rule.Priority,
            EffectiveTime = rule.EffectiveTime,
            ExpirationTime = rule.ExpirationTime,
            Remark = rule.Remark,
            Items = items,
            CreatedTime = rule.CreatedTime,
            CreatedId = rule.CreatedId,
            CreatedBy = rule.CreatedBy,
            ModifiedTime = rule.ModifiedTime,
            ModifiedId = rule.ModifiedId,
            ModifiedBy = rule.ModifiedBy
        };
    }

    /// <summary>
    /// 映射约束规则项
    /// </summary>
    public static ConstraintRuleItemDto ToItemDto(SysConstraintRuleItem item, string? targetCode, string? targetName)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new ConstraintRuleItemDto
        {
            BasicId = item.BasicId,
            TargetType = item.TargetType,
            TargetId = item.TargetId,
            TargetCode = targetCode,
            TargetName = targetName,
            ConstraintGroup = item.ConstraintGroup,
            Remark = item.Remark,
            CreatedTime = item.CreatedTime
        };
    }

    /// <summary>
    /// 判断规则当前是否生效
    /// </summary>
    private static bool IsActive(SysConstraintRule rule, DateTimeOffset now)
    {
        return rule.Status == EnableStatus.Enabled
            && (!rule.EffectiveTime.HasValue || rule.EffectiveTime.Value <= now)
            && (!rule.ExpirationTime.HasValue || rule.ExpirationTime.Value > now);
    }

    private static IReadOnlyList<ConstraintRuleItemCommand> ToItemCommands(IReadOnlyList<ConstraintRuleItemInputDto> items)
    {
        return items
            .Select(item => new ConstraintRuleItemCommand(item.TargetType, item.TargetId, item.ConstraintGroup, item.Remark))
            .ToArray();
    }
}
