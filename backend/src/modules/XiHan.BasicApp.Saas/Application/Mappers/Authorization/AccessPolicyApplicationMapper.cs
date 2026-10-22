// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 访问策略应用层映射器
/// </summary>
public static class AccessPolicyApplicationMapper
{
    /// <summary>
    /// 条件命令
    /// </summary>
    public static IReadOnlyList<AccessPolicyConditionCommand> ToConditionCommands(IEnumerable<AccessPolicyConditionDto>? conditions)
    {
        return conditions is null
            ? []
            : [.. conditions.Select(condition => new AccessPolicyConditionCommand(
                condition.ConditionGroup,
                condition.AttributeName ?? string.Empty,
                condition.Operator,
                condition.IsNegated,
                condition.ValueType,
                condition.ConditionValue ?? string.Empty,
                condition.Description))];
    }

    /// <summary>
    /// 创建命令
    /// </summary>
    public static AccessPolicyCreateCommand ToCreateCommand(AccessPolicyCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new AccessPolicyCreateCommand(
            input.PolicyCode,
            input.PolicyName,
            input.Description,
            input.Priority,
            input.Status,
            input.EffectiveTime,
            input.ExpirationTime,
            input.TemplateCode,
            input.Remark,
            ToConditionCommands(input.Conditions));
    }

    /// <summary>
    /// 更新命令
    /// </summary>
    public static AccessPolicyUpdateCommand ToUpdateCommand(AccessPolicyUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new AccessPolicyUpdateCommand(
            input.BasicId,
            input.PolicyName,
            input.Description,
            input.Priority,
            input.EffectiveTime,
            input.ExpirationTime,
            input.Remark,
            ToConditionCommands(input.Conditions));
    }

    /// <summary>
    /// 条件
    /// </summary>
    public static AccessPolicyConditionDto ToConditionDto(SysAccessPolicyCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return new AccessPolicyConditionDto
        {
            ConditionGroup = condition.ConditionGroup,
            AttributeName = condition.AttributeName,
            Operator = condition.Operator,
            IsNegated = condition.IsNegated,
            ValueType = condition.ValueType,
            ConditionValue = condition.ConditionValue,
            Description = condition.Description
        };
    }

    /// <summary>
    /// 列表项
    /// </summary>
    public static AccessPolicyListItemDto ToListItemDto(SysAccessPolicy policy, IReadOnlyList<SysAccessPolicyCondition> conditions, long recentHitCount, DateTimeOffset now)
    {
        return Fill(new AccessPolicyListItemDto(), policy, conditions, recentHitCount, now);
    }

    /// <summary>
    /// 详情
    /// </summary>
    public static AccessPolicyDetailDto ToDetailDto(SysAccessPolicy policy, IReadOnlyList<SysAccessPolicyCondition> conditions, long recentHitCount, DateTimeOffset now)
    {
        var dto = Fill(new AccessPolicyDetailDto(), policy, conditions, recentHitCount, now);
        dto.Remark = policy.Remark;
        dto.Conditions = [.. conditions.Select(condition => ToConditionDto(condition))];
        return dto;
    }

    /// <summary>
    /// 命中记录列表项
    /// </summary>
    public static AccessPolicyHitListItemDto ToHitListItemDto(SysAccessPolicyHit hit)
    {
        ArgumentNullException.ThrowIfNull(hit);
        return new AccessPolicyHitListItemDto
        {
            BasicId = hit.BasicId,
            Source = hit.Source,
            PolicyId = hit.PolicyId,
            PolicyCode = hit.PolicyCode,
            PolicyName = hit.PolicyName,
            Action = hit.Action,
            Phase = hit.Phase,
            IsBlocked = hit.IsBlocked,
            TraceId = hit.TraceId,
            Ip = hit.Ip,
            Location = hit.Location,
            UserId = hit.UserId,
            UserName = hit.UserName,
            Method = hit.Method,
            Path = hit.Path,
            PermissionCodes = hit.PermissionCodes,
            UserAgent = hit.UserAgent,
            ClientKind = hit.ClientKind,
            HitTime = hit.HitTime,
            HitCount = hit.HitCount
        };
    }

    private static TDto Fill<TDto>(TDto dto, SysAccessPolicy policy, IReadOnlyList<SysAccessPolicyCondition> conditions, long recentHitCount, DateTimeOffset now)
        where TDto : AccessPolicyListItemDto
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(conditions);
        dto.BasicId = policy.BasicId;
        dto.TenantId = policy.TenantId;
        dto.PolicyCode = policy.PolicyCode;
        dto.PolicyName = policy.PolicyName;
        dto.Description = policy.Description;
        dto.Action = policy.Action;
        dto.Priority = policy.Priority;
        dto.Status = policy.Status;
        dto.IsActive = policy.IsEffectiveAt(now);
        dto.EffectiveTime = policy.EffectiveTime;
        dto.ExpirationTime = policy.ExpirationTime;
        dto.TemplateCode = policy.TemplateCode;
        dto.Phase = conditions.All(condition => AccessPolicyPhases.IsAvailableBeforeAuthentication(condition.AttributeName))
            ? AccessPolicyPhase.BeforeAuthentication
            : AccessPolicyPhase.AfterAuthentication;
        dto.ConditionCount = conditions.Count;
        dto.RecentHitCount = recentHitCount;
        dto.CreatedTime = policy.CreatedTime;
        dto.ModifiedTime = policy.ModifiedTime;
        return dto;
    }
}
