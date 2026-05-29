#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleDomainService
// Guid:2c60e3db-69dd-463e-991d-7909879a42fb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 约束规则领域服务实现
/// </summary>
public sealed class ConstraintRuleDomainService
    : IConstraintRuleDomainService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ConstraintRuleDomainService(
        IConstraintRuleRepository constraintRuleRepository,
        IConstraintRuleItemRepository constraintRuleItemRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ITenantUserRepository tenantUserRepository)
    {
        _constraintRuleRepository = constraintRuleRepository;
        _constraintRuleItemRepository = constraintRuleItemRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _tenantUserRepository = tenantUserRepository;
    }

    private readonly IConstraintRuleItemRepository _constraintRuleItemRepository;
    private readonly IConstraintRuleRepository _constraintRuleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantUserRepository _tenantUserRepository;

    /// <inheritdoc />
    public async Task<ConstraintRuleCommandResult> CreateConstraintRuleAsync(ConstraintRuleCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateCreateInput(command, now);

        var ruleCode = command.RuleCode.Trim();
        await EnsureRuleCodeNotExistsAsync(ruleCode, null, cancellationToken);
        await ValidateRuleItemsAsync(command.ConstraintType, command.TargetType, command.Items, now, cancellationToken);

        var rule = new SysConstraintRule
        {
            RuleCode = ruleCode,
            RuleName = command.RuleName.Trim(),
            ConstraintType = command.ConstraintType,
            TargetType = command.TargetType,
            Parameters = NormalizeParameters(command.Parameters),
            Status = command.Status,
            ViolationAction = command.ViolationAction,
            Description = NormalizeNullable(command.Description),
            Priority = command.Priority,
            EffectiveTime = command.EffectiveTime,
            ExpirationTime = command.ExpirationTime,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedRule = await _constraintRuleRepository.AddAsync(rule, cancellationToken);
        _ = await ReplaceRuleItemsAsync(savedRule.BasicId, command.Items, cancellationToken);
        return new ConstraintRuleCommandResult(savedRule.BasicId);
    }

    /// <inheritdoc />
    public async Task DeleteConstraintRuleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rule = await GetEditableRuleOrThrowAsync(id, cancellationToken);
        _ = await _constraintRuleItemRepository.DeleteAsync(item => item.ConstraintRuleId == rule.BasicId, cancellationToken);
        if (!await _constraintRuleRepository.DeleteAsync(rule, cancellationToken))
        {
            throw new InvalidOperationException("约束规则删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<ConstraintRuleCommandResult> UpdateConstraintRuleAsync(ConstraintRuleUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateUpdateInput(command, now);

        var rule = await GetEditableRuleOrThrowAsync(command.BasicId, cancellationToken);
        await ValidateRuleItemsAsync(command.ConstraintType, command.TargetType, command.Items, now, cancellationToken);

        rule.RuleName = command.RuleName.Trim();
        rule.ConstraintType = command.ConstraintType;
        rule.TargetType = command.TargetType;
        rule.Parameters = NormalizeParameters(command.Parameters);
        rule.ViolationAction = command.ViolationAction;
        rule.Description = NormalizeNullable(command.Description);
        rule.Priority = command.Priority;
        rule.EffectiveTime = command.EffectiveTime;
        rule.ExpirationTime = command.ExpirationTime;
        rule.Remark = NormalizeNullable(command.Remark);

        var savedRule = await _constraintRuleRepository.UpdateAsync(rule, cancellationToken);
        _ = await ReplaceRuleItemsAsync(savedRule.BasicId, command.Items, cancellationToken);
        return new ConstraintRuleCommandResult(savedRule.BasicId);
    }

    /// <inheritdoc />
    public async Task<ConstraintRuleCommandResult> UpdateConstraintRuleStatusAsync(ConstraintRuleStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "约束规则主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var now = DateTimeOffset.UtcNow;
        var rule = await GetEditableRuleOrThrowAsync(command.BasicId, cancellationToken);
        var items = await GetRuleItemsAsync(rule.BasicId, cancellationToken);
        if (command.Status == EnableStatus.Enabled)
        {
            ValidateRuleItems(rule.ConstraintType, rule.TargetType, ToCommandItems(items));
            await EnsureTargetsUsableAsync(items, now, cancellationToken);
            rule.Enable();
        }
        else
        {
            rule.Disable();
        }

        rule.Remark = NormalizeNullable(command.Remark) ?? rule.Remark;
        var savedRule = await _constraintRuleRepository.UpdateAsync(rule, cancellationToken);
        return new ConstraintRuleCommandResult(savedRule.BasicId);
    }

    private static void EnsureRuleCanBeMaintained(SysConstraintRule rule)
    {
        if (rule.IsGlobal)
        {
            throw new InvalidOperationException("平台级全局约束规则必须通过平台运维流程维护。");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeParameters(string? parameters)
    {
        var normalized = NormalizeNullable(parameters);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("约束参数必须是合法 JSON。", exception);
        }

        return normalized;
    }

    private static IReadOnlyList<ConstraintRuleItemCommand> ToCommandItems(IReadOnlyList<SysConstraintRuleItem> items)
    {
        return items
            .Select(item => new ConstraintRuleItemCommand(item.TargetType, item.TargetId, item.ConstraintGroup, item.Remark))
            .ToArray();
    }

    private static void ValidateCommonInput(
        string ruleName,
        ConstraintType constraintType,
        ConstraintTargetType targetType,
        string? parameters,
        ViolationAction violationAction,
        string? description,
        int priority,
        DateTimeOffset? effectiveTime,
        DateTimeOffset? expirationTime,
        string? remark,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);
        ValidateEnum(constraintType, nameof(constraintType));
        ValidateEnum(targetType, nameof(targetType));
        ValidateEnum(violationAction, nameof(violationAction));
        ValidateLength(ruleName, 200, nameof(ruleName), "规则名称不能超过 200 个字符。");
        ValidateOptionalLength(description, 1000, nameof(description), "规则描述不能超过 1000 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        _ = NormalizeParameters(parameters);

        if (priority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority), "优先级不能小于 0。");
        }

        if (expirationTime.HasValue && expirationTime.Value <= now)
        {
            throw new InvalidOperationException("失效时间必须晚于当前时间。");
        }

        if (effectiveTime.HasValue && expirationTime.HasValue && effectiveTime.Value >= expirationTime.Value)
        {
            throw new InvalidOperationException("生效时间必须早于失效时间。");
        }
    }

    private static void ValidateCreateInput(ConstraintRuleCreateCommand command, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RuleCode);
        ValidateRuleCode(command.RuleCode);
        ValidateCommonInput(
            command.RuleName,
            command.ConstraintType,
            command.TargetType,
            command.Parameters,
            command.ViolationAction,
            command.Description,
            command.Priority,
            command.EffectiveTime,
            command.ExpirationTime,
            command.Remark,
            now);
        ValidateEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void ValidateLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidateRuleCode(string ruleCode)
    {
        var normalizedRuleCode = ruleCode.Trim();
        ValidateLength(normalizedRuleCode, 100, nameof(ruleCode), "规则编码不能超过 100 个字符。");
        if (normalizedRuleCode.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("规则编码不能包含空白字符。");
        }
    }

    private static void ValidateRuleItems(
        ConstraintType constraintType,
        ConstraintTargetType targetType,
        IReadOnlyList<ConstraintRuleItemCommand> items)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("约束规则至少需要一个规则项。");
        }

        var duplicated = items
            .GroupBy(item => new { item.TargetType, item.TargetId })
            .Any(group => group.Count() > 1);
        if (duplicated)
        {
            throw new InvalidOperationException("约束规则项目标不能重复。");
        }

        foreach (var item in items)
        {
            ValidateEnum(item.TargetType, nameof(item.TargetType));
            if (item.TargetId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(item.TargetId), "规则项目标主键必须大于 0。");
            }

            if (item.ConstraintGroup < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(item.ConstraintGroup), "约束分组不能小于 0。");
            }

            if (constraintType != ConstraintType.Prerequisite && item.TargetType != targetType)
            {
                throw new InvalidOperationException("非先决条件约束的规则项目标类型必须与规则目标类型一致。");
            }
        }

        if (constraintType is ConstraintType.SSD or ConstraintType.DSD or ConstraintType.MutualExclusion && items.Count < 2)
        {
            throw new InvalidOperationException("职责分离或互斥约束至少需要两个目标项。");
        }

        if (constraintType == ConstraintType.Prerequisite)
        {
            if (!items.Any(item => item.ConstraintGroup == 0) || !items.Any(item => item.ConstraintGroup == 1))
            {
                throw new InvalidOperationException("先决条件约束必须同时包含必备项分组 0 和目标项分组 1。");
            }
        }
    }

    private static void ValidateUpdateInput(ConstraintRuleUpdateCommand command, DateTimeOffset now)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "约束规则主键必须大于 0。");
        }

        ValidateCommonInput(
            command.RuleName,
            command.ConstraintType,
            command.TargetType,
            command.Parameters,
            command.ViolationAction,
            command.Description,
            command.Priority,
            command.EffectiveTime,
            command.ExpirationTime,
            command.Remark,
            now);
    }

    private async Task EnsureRuleCodeNotExistsAsync(string ruleCode, long? excludeId, CancellationToken cancellationToken)
    {
        var exists = excludeId.HasValue
            ? await _constraintRuleRepository.AnyAsync(
                rule => rule.RuleCode == ruleCode && rule.BasicId != excludeId.Value,
                cancellationToken)
            : await _constraintRuleRepository.AnyAsync(rule => rule.RuleCode == ruleCode, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("约束规则编码已存在。");
        }
    }

    private async Task EnsureTargetsUsableAsync(IReadOnlyList<SysConstraintRuleItem> items, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            _ = await GetAvailableTargetSummaryOrThrowAsync(item.TargetType, item.TargetId, now, cancellationToken);
        }
    }

    private async Task<(string? Code, string? Name)> GetAvailablePermissionTargetSummaryOrThrowAsync(long permissionId, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能配置约束规则。");
        }

        return (permission.PermissionCode, permission.PermissionName);
    }

    private async Task<(string? Code, string? Name)> GetAvailableRoleTargetSummaryOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能配置约束规则。");
        }

        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色约束规则必须通过平台运维流程维护。");
        }

        return (role.RoleCode, role.RoleName);
    }

    private async Task<(string? Code, string? Name)> GetAvailableTargetSummaryOrThrowAsync(
        ConstraintTargetType targetType,
        long targetId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return targetType switch
        {
            ConstraintTargetType.Role => await GetAvailableRoleTargetSummaryOrThrowAsync(targetId, cancellationToken),
            ConstraintTargetType.Permission => await GetAvailablePermissionTargetSummaryOrThrowAsync(targetId, cancellationToken),
            ConstraintTargetType.User => await GetAvailableTenantMemberTargetSummaryOrThrowAsync(targetId, now, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(targetType), "约束规则目标类型无效。")
        };
    }

    private async Task<(string? Code, string? Name)> GetAvailableTenantMemberTargetSummaryOrThrowAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("未接受邀请的租户成员不能配置约束规则。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能配置约束规则。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员约束规则必须通过平台运维流程维护。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能配置约束规则。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能配置约束规则。");
        }

        return (null, tenantMember.DisplayName);
    }

    private async Task<SysConstraintRule> GetEditableRuleOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "约束规则主键必须大于 0。");
        }

        var rule = await _constraintRuleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("约束规则不存在。");

        EnsureRuleCanBeMaintained(rule);
        return rule;
    }

    private async Task<IReadOnlyList<SysConstraintRuleItem>> GetRuleItemsAsync(long ruleId, CancellationToken cancellationToken)
    {
        return await _constraintRuleItemRepository.GetListAsync(
            item => item.ConstraintRuleId == ruleId,
            item => item.ConstraintGroup,
            cancellationToken);
    }

    private async Task<IReadOnlyList<SysConstraintRuleItem>> ReplaceRuleItemsAsync(
        long ruleId,
        IReadOnlyList<ConstraintRuleItemCommand> inputs,
        CancellationToken cancellationToken)
    {
        _ = await _constraintRuleItemRepository.DeleteAsync(item => item.ConstraintRuleId == ruleId, cancellationToken);

        var items = inputs
            .Select(input => new SysConstraintRuleItem
            {
                ConstraintRuleId = ruleId,
                TargetType = input.TargetType,
                TargetId = input.TargetId,
                ConstraintGroup = input.ConstraintGroup,
                Remark = NormalizeNullable(input.Remark)
            })
            .ToArray();

        return await _constraintRuleItemRepository.AddRangeAsync(items, cancellationToken);
    }

    private async Task ValidateRuleItemsAsync(
        ConstraintType constraintType,
        ConstraintTargetType targetType,
        IReadOnlyList<ConstraintRuleItemCommand> items,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ValidateRuleItems(constraintType, targetType, items);

        foreach (var item in items)
        {
            _ = await GetAvailableTargetSummaryOrThrowAsync(item.TargetType, item.TargetId, now, cancellationToken);
        }
    }
}
