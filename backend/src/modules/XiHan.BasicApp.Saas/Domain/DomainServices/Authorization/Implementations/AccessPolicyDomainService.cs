// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 访问策略领域服务实现
/// </summary>
/// <remarks>
/// 平台上下文只读写平台策略，租户上下文只读写本租户的策略。条件随策略整体替换，不允许空条件集。
/// </remarks>
public sealed class AccessPolicyDomainService : IAccessPolicyDomainService
{
    /// <summary>
    /// 一条策略最多的条件组数
    /// </summary>
    public const int MaxGroups = 20;

    /// <summary>
    /// 一条策略最多的条件数
    /// </summary>
    public const int MaxConditions = 100;

    private readonly IAccessPolicyRepository _policyRepository;
    private readonly IAccessPolicyConditionRepository _conditionRepository;
    private readonly IAccessPolicyRuleChecker _ruleChecker;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyDomainService(
        IAccessPolicyRepository policyRepository,
        IAccessPolicyConditionRepository conditionRepository,
        IAccessPolicyRuleChecker ruleChecker)
    {
        _policyRepository = policyRepository;
        _conditionRepository = conditionRepository;
        _ruleChecker = ruleChecker;
    }

    /// <summary>
    /// 创建访问策略
    /// </summary>
    public async Task<AccessPolicyCommandResult> CreateAccessPolicyAsync(AccessPolicyCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var policyCode = NormalizeCode(command.PolicyCode);
        ValidateEnum(command.Status, nameof(command.Status));
        ValidateCommon(command.PolicyName, command.Description, command.Priority, command.EffectiveTime, command.ExpirationTime, command.Remark);
        ValidateOptionalLength(command.TemplateCode, 100, nameof(command.TemplateCode), "来源模板编码不能超过 100 个字符。");
        ValidateConditions(command.Conditions);
        if (await _policyRepository.AnyAsync(policy => policy.PolicyCode == policyCode, cancellationToken))
        {
            throw new InvalidOperationException($"访问策略编码 {policyCode} 已存在。");
        }

        var policy = await _policyRepository.AddAsync(new SysAccessPolicy
        {
            PolicyCode = policyCode,
            PolicyName = command.PolicyName.Trim(),
            Description = NormalizeNullable(command.Description),
            Action = AccessPolicyAction.LogOnly,
            Priority = command.Priority,
            Status = command.Status,
            EffectiveTime = command.EffectiveTime,
            ExpirationTime = command.ExpirationTime,
            TemplateCode = NormalizeNullable(command.TemplateCode),
            Remark = NormalizeNullable(command.Remark)
        }, cancellationToken);
        await ReplaceConditionsAsync(policy.BasicId, command.Conditions, cancellationToken);
        return new AccessPolicyCommandResult(policy.BasicId);
    }

    /// <summary>
    /// 更新访问策略
    /// </summary>
    public async Task<AccessPolicyCommandResult> UpdateAccessPolicyAsync(AccessPolicyUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCommon(command.PolicyName, command.Description, command.Priority, command.EffectiveTime, command.ExpirationTime, command.Remark);
        ValidateConditions(command.Conditions);
        var policy = await GetPolicyOrThrowAsync(command.BasicId, cancellationToken);
        policy.PolicyName = command.PolicyName.Trim();
        policy.Description = NormalizeNullable(command.Description);
        policy.Priority = command.Priority;
        policy.EffectiveTime = command.EffectiveTime;
        policy.ExpirationTime = command.ExpirationTime;
        policy.Remark = NormalizeNullable(command.Remark);
        _ = await _policyRepository.UpdateAsync(policy, cancellationToken);
        await ReplaceConditionsAsync(policy.BasicId, command.Conditions, cancellationToken);
        return new AccessPolicyCommandResult(policy.BasicId);
    }

    /// <summary>
    /// 修改访问策略的动作
    /// </summary>
    public async Task<AccessPolicyCommandResult> UpdateAccessPolicyActionAsync(AccessPolicyActionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateEnum(command.Action, nameof(command.Action));
        var policy = await GetPolicyOrThrowAsync(command.BasicId, cancellationToken);
        policy.Action = command.Action;
        _ = await _policyRepository.UpdateAsync(policy, cancellationToken);
        return new AccessPolicyCommandResult(policy.BasicId);
    }

    /// <summary>
    /// 启用或停用访问策略
    /// </summary>
    public async Task<AccessPolicyCommandResult> UpdateAccessPolicyStatusAsync(AccessPolicyStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateEnum(command.Status, nameof(command.Status));
        var policy = await GetPolicyOrThrowAsync(command.BasicId, cancellationToken);
        if (command.Status == EnableStatus.Enabled)
        {
            var conditions = await _conditionRepository.GetByPolicyIdAsync(policy.BasicId, cancellationToken);
            ValidateConditions([.. conditions.Select(ToCommand)]);
        }

        policy.Status = command.Status;
        _ = await _policyRepository.UpdateAsync(policy, cancellationToken);
        return new AccessPolicyCommandResult(policy.BasicId);
    }

    /// <summary>
    /// 删除访问策略及其条件
    /// </summary>
    public async Task DeleteAccessPolicyAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var policy = await GetPolicyOrThrowAsync(id, cancellationToken);
        _ = await _conditionRepository.DeleteAsync(condition => condition.PolicyId == policy.BasicId, cancellationToken);
        if (!await _policyRepository.DeleteAsync(policy, cancellationToken))
        {
            throw new InvalidOperationException("访问策略删除失败。");
        }
    }

    private static AccessPolicyConditionCommand ToCommand(SysAccessPolicyCondition condition)
    {
        return new AccessPolicyConditionCommand(
            condition.ConditionGroup,
            condition.AttributeName,
            condition.Operator,
            condition.IsNegated,
            condition.ValueType,
            condition.ConditionValue,
            condition.Description);
    }

    private static string NormalizeCode(string policyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyCode);
        var normalized = policyCode.Trim();
        ValidateLength(normalized, 100, nameof(policyCode), "策略编码不能超过 100 个字符。");
        if (normalized.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("策略编码不能包含空白字符。");
        }

        return normalized;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateCommon(
        string policyName,
        string? description,
        int priority,
        DateTimeOffset? effectiveTime,
        DateTimeOffset? expirationTime,
        string? remark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ValidateLength(policyName, 200, nameof(policyName), "策略名称不能超过 200 个字符。");
        ValidateOptionalLength(description, 1000, nameof(description), "说明不能超过 1000 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        if (priority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority), "优先级不能小于 0。");
        }

        if (effectiveTime.HasValue && expirationTime.HasValue && effectiveTime.Value >= expirationTime.Value)
        {
            throw new InvalidOperationException("生效时间必须早于失效时间。");
        }
    }

    private void ValidateConditions(IReadOnlyList<AccessPolicyConditionCommand> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        if (conditions.Count == 0)
        {
            throw new InvalidOperationException("访问策略至少要有一条条件，空条件会匹配所有请求。");
        }

        if (conditions.Count > MaxConditions)
        {
            throw new InvalidOperationException($"一条访问策略最多 {MaxConditions} 条条件。");
        }

        if (conditions.Select(condition => condition.ConditionGroup).Distinct().Count() > MaxGroups)
        {
            throw new InvalidOperationException($"一条访问策略最多 {MaxGroups} 个条件组。");
        }

        foreach (var condition in conditions)
        {
            ArgumentNullException.ThrowIfNull(condition);
            ArgumentException.ThrowIfNullOrWhiteSpace(condition.AttributeName);
            ValidateEnum(condition.Operator, nameof(condition.Operator));
            ValidateEnum(condition.ValueType, nameof(condition.ValueType));
            ValidateLength(condition.AttributeName, 200, nameof(condition.AttributeName), "属性名称不能超过 200 个字符。");
            ValidateOptionalLength(condition.Description, 500, nameof(condition.Description), "条件说明不能超过 500 个字符。");
            if (condition.ConditionGroup < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(conditions), "条件分组不能小于 0。");
            }
        }

        _ruleChecker.Validate(conditions);
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

    private async Task<SysAccessPolicy> GetPolicyOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "访问策略主键必须大于 0。");
        }

        return await _policyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("访问策略不存在。");
    }

    private async Task ReplaceConditionsAsync(long policyId, IReadOnlyList<AccessPolicyConditionCommand> conditions, CancellationToken cancellationToken)
    {
        _ = await _conditionRepository.DeleteAsync(condition => condition.PolicyId == policyId, cancellationToken);
        _ = await _conditionRepository.AddRangeAsync(
            [.. conditions.Select(condition => new SysAccessPolicyCondition
            {
                PolicyId = policyId,
                ConditionGroup = condition.ConditionGroup,
                AttributeName = condition.AttributeName.Trim(),
                Operator = condition.Operator,
                IsNegated = condition.IsNegated,
                ValueType = condition.ValueType,
                ConditionValue = condition.ConditionValue?.Trim() ?? string.Empty,
                Description = NormalizeNullable(condition.Description)
            })],
            cancellationToken);
    }
}
